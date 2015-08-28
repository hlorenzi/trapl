using System;
using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class Analyzer 
    {
        public static Output Pass(Structure.Output syn, Diagnostics.MessageList diagn)
        {
            var analyzer = new Analyzer(syn, diagn);
            analyzer.ParseStructDecls();
            analyzer.TestForStructCycles();
            analyzer.ParseFunctDecls();
            analyzer.ParseFunctBodies();
            return analyzer.output;
        }


        private class ParserException : Exception { }


        private Output output;
        private Structure.Output syn;
        private Diagnostics.MessageList diagn;


        private Analyzer(Structure.Output syn, Diagnostics.MessageList diagn)
        {
            this.output = new Output();
            this.syn = syn;
            this.diagn = diagn;
        }


        private VariableType ResolveType(Syntax.Node node, Source source, bool voidAllowed = false)
        {
            if (node.kind != Syntax.NodeKind.TypeName)
                throw new ParserException();

            var name = source.Excerpt(node.Span());
            var structDefWithName = this.output.structDefs.Find(s => s.name == name);
            if (structDefWithName != null)
            {
                var type = new VariableTypeStruct();
                type.structDef = structDefWithName;

                if (!voidAllowed && type.IsSame(this.MakeVoidType()))
                {
                    this.diagn.AddError(MessageID.SemanticsVoidType(), source, node.Span());
                    throw new ParserException();
                }

                return type;
            }

            this.diagn.AddError(MessageID.SemanticsUnknownType(), source, node.Span());
            throw new ParserException();
        }


        private VariableType MakeVoidType()
        {
            var type = new VariableTypeStruct();
            type.structDef = this.output.structDefs[0];
            return type;
        }


        private void ParseStructDecls()
        {
            // First, add primitive types.
            this.output.structDefs.Add(new StructDef("Void"));
            this.output.structDefs.Add(new StructDef("Bool"));
            this.output.structDefs.Add(new StructDef("Int8"));
            this.output.structDefs.Add(new StructDef("Int16"));
            this.output.structDefs.Add(new StructDef("Int32"));
            this.output.structDefs.Add(new StructDef("Int64"));
            this.output.structDefs.Add(new StructDef("UInt8"));
            this.output.structDefs.Add(new StructDef("UInt16"));
            this.output.structDefs.Add(new StructDef("UInt32"));
            this.output.structDefs.Add(new StructDef("UInt64"));
            this.output.structDefs.Add(new StructDef("Float32"));
            this.output.structDefs.Add(new StructDef("Float64"));

            var userStructFirstIndex = this.output.structDefs.Count;

            // Then, add user structs without parsing their members.
            for (int i = 0; i < this.syn.structDecls.Count; i++)
                this.output.structDefs.Add(new StructDef(
                    this.syn.structDecls[i].name,
                    this.syn.structDecls[i].source,
                    this.syn.structDecls[i].syntaxNode.Span()));

            // And finally, parse struct members, resolving their types.
            for (int i = 0; i < this.syn.structDecls.Count; i++)
            {
                var src = this.syn.structDecls[i].source;
                foreach (var memberNode in this.syn.structDecls[i].syntaxNode.EnumerateChildren())
                {
                    if (memberNode.kind != Syntax.NodeKind.StructMemberDecl)
                        continue;

                    try
                    {
                        var memberDef = new StructDef.Member();
                        memberDef.name = src.Excerpt(memberNode.Child(0).Span());
                        memberDef.declSpan = memberNode.Span();
                        memberDef.type = this.ResolveType(memberNode.Child(1), src);
                        this.output.structDefs[i + userStructFirstIndex].members.Add(memberDef);
                    }
                    catch (ParserException) { }
                }
            }
        }


        private bool TestForStructCycles()
        {
            bool result = false;
            var alreadyChecked = new Stack<StructDef>();

            // Recursively check for struct cycles.
            foreach (var st in this.output.structDefs)
            {
                alreadyChecked.Push(st);
                if (TestForStructCyclesInner(alreadyChecked, st))
                {
                    this.diagn.AddError(MessageID.SemanticsStructCycleDetected(), st.source, st.declSpan);
                    result = true;
                }

                alreadyChecked.Pop();
            }

            return result;
        }


        private bool TestForStructCyclesInner(Stack<StructDef> alreadyChecked, StructDef structToCheck)
        {
            // Recurse into struct members to check for cycles.
            foreach (var member in structToCheck.members)
            {
                if (member.type is VariableTypeStruct)
                {
                    var memberTypeStruct = ((VariableTypeStruct)member.type).structDef;
                    if (alreadyChecked.Contains(memberTypeStruct))
                        return true;

                    alreadyChecked.Push(memberTypeStruct);
                    if (TestForStructCyclesInner(alreadyChecked, memberTypeStruct))
                        return true;

                    alreadyChecked.Pop();
                }
            }
            return false;
        }


        private void ParseFunctDecls()
        {
            foreach (var decl in this.syn.functDecls)
            {
                try
                {
                    var doubleDef = this.output.functDefs.Find(f => f.name == decl.name);
                    if (doubleDef != null)
                    {
                        this.diagn.AddError(MessageID.SemanticsDoubleDef(),
                            MessageCaret.Primary(decl.source, decl.syntaxNode.Span()),
                            MessageCaret.Primary(doubleDef.source, doubleDef.declSpan));
                        continue;
                    }

                    var funct = new FunctDef(decl.name, decl.source, decl.syntaxNode.Span());

                    // Parse arguments.
                    foreach (var argNode in decl.syntaxNode.EnumerateChildren())
                    {
                        if (argNode.kind != Syntax.NodeKind.FunctArgDecl)
                            continue;

                        var argName = decl.source.Excerpt(argNode.Child(0).Span());
                        var argType = this.ResolveType(argNode.Child(1), decl.source);
                        funct.arguments.Add(new FunctDef.Variable(argName, argType, argNode.Span()));
                        funct.localVariables.Add(new FunctDef.Variable(argName, argType, argNode.Span()));
                    }

                    // Parse return type.
                    funct.returnType = this.MakeVoidType();
                    foreach (var argNode in decl.syntaxNode.EnumerateChildren())
                    {
                        if (argNode.kind != Syntax.NodeKind.FunctReturnDecl)
                            continue;

                        funct.returnType = this.ResolveType(argNode.Child(0), decl.source);
                    }

                    this.output.functDefs.Add(funct);
                }
                catch (ParserException) { }
            }
        }


        private void ParseFunctBodies()
        {
            foreach (var decl in this.syn.functDecls)
            {
            }
        }
    }
}
