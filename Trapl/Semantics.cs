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


            for (int i = 0; i < this.output.functDefs.Count; i++)
            {
                var decl = this.syn.functDecls[i];
                var funct = this.output.functDefs[i];

                funct.body = FunctBodyAnalyzer.Analyze(
                    this,
                    decl.syntaxNode.ChildWithKind(Syntax.NodeKind.Block),
                    funct);
            }
        }


        private class FunctBodyAnalyzer
        {
            public static CodeSegment Analyze(Semantics.Analyzer owner, Syntax.Node node, FunctDef funct)
            {
                var analyzer = new FunctBodyAnalyzer(owner, funct);
                var segment = new CodeSegment();
                analyzer.ParseBlock(node, segment);
                return segment;
            }


            private Semantics.Analyzer owner;
            private FunctDef funct;


            private FunctBodyAnalyzer(Semantics.Analyzer owner, FunctDef funct)
            {
                this.owner = owner;
                this.funct = funct;
            }


            private CodeSegment ParseBlock(Syntax.Node node, CodeSegment segment)
            {
                var curLocalIndex = this.funct.localVariables.Count;

                foreach (var exprNode in node.EnumerateChildren())
                {
                    try
                    {
                        segment = this.ParseExpression(exprNode, segment);
                    }
                    catch (ParserException) { }
                }

                for (int i = this.funct.localVariables.Count - 1; i >= curLocalIndex; i--)
                {
                    var v = this.funct.localVariables[i];
                    if (v.outOfScope)
                        continue;

                    v.outOfScope = true;
                    var codeNode = new CodeNodeVariableEnd();
                    codeNode.localIndex = i;
                    segment.nodes.Add(codeNode);
                }

                return segment;
            }


            private CodeSegment ParseExpression(Syntax.Node node, CodeSegment segment)
            {
                if (node.kind == Syntax.NodeKind.ControlLet)
                    return this.ParseControlLet(node, segment);
                else if (node.kind == Syntax.NodeKind.ControlIf)
                    return this.ParseControlIf(node, segment);
                else if (node.kind == Syntax.NodeKind.ControlWhile)
                    return this.ParseControlWhile(node, segment);
                //else
                //    throw new ParserException();

                return segment;
            }


            private CodeSegment ParseControlLet(Syntax.Node node, CodeSegment segment)
            {
                if (node.ChildNumber() == 1)
                {
                    this.owner.diagn.AddError(MessageID.SemanticsCannotInferType(), this.funct.source, node.Span());
                    throw new ParserException();
                }

                var varName = this.funct.source.Excerpt(node.Child(0).Span());
                var varType = this.owner.ResolveType(node.Child(1), this.funct.source, false);
                var varSpan = node.Span();

                var previousDecl = this.funct.localVariables.FindLast(v => v.name == varName && !v.outOfScope);
                if (previousDecl != null)
                {
                    this.owner.diagn.AddWarning(MessageID.SemanticsShadowing(),
                        MessageCaret.Primary(this.funct.source, varSpan),
                        MessageCaret.Primary(this.funct.source, previousDecl.declSpan));
                }

                var newVariable = new FunctDef.Variable(varName, varType, varSpan);
                this.funct.localVariables.Add(newVariable);

                var codeNode = new CodeNodeVariableBegin();
                codeNode.localIndex = this.funct.localVariables.Count - 1;

                segment.nodes.Add(codeNode);
                return segment;
            }


            private CodeSegment ParseControlIf(Syntax.Node node, CodeSegment segmentBefore)
            {
                /*       SEGMENT BEFORE                         SEGMENT BEFORE
                               |                                      |
                       +-------+-------+                      +-------+-------+
                       |               |                      |               |
                       v               v                      |               v
                 SEGMENT FALSE    SEGMENT TRUE       or       |          SEGMENT TRUE
                       |               |                      |               |
                       +-------+-------+                      +-------+-------+
                               |                                      |
                               v                                      v
                         SEGMENT AFTER                          SEGMENT AFTER            */

                segmentBefore.nodes.Add(new CodeNodeIf());

                var segmentTrue = new CodeSegment();
                var segmentTrueEnd = this.ParseBlock(node.Child(1), segmentTrue);

                var segmentAfter = new CodeSegment();
                segmentBefore.GoesTo(segmentTrue);
                segmentTrueEnd.GoesTo(segmentAfter);

                if (node.ChildNumber() == 3)
                {
                    var segmentFalse = new CodeSegment();
                    var segmentFalseEnd = this.ParseBlock(node.Child(2), segmentFalse);
                    segmentBefore.GoesTo(segmentFalse);
                    segmentFalseEnd.GoesTo(segmentAfter);
                }
                else
                {
                    segmentBefore.GoesTo(segmentAfter);
                }

                return segmentAfter;
            }


            private CodeSegment ParseControlWhile(Syntax.Node node, CodeSegment segmentBefore)
            {
                /*    SEGMENT BEFORE
                        |
                        v
                  +-> SEGMENT CONDITION
                  |               |
                  |       +-------+-------+
                  |       |               |
                  |       v               v
                  +-- SEGMENT BODY    SEGMENT AFTER         */

                var segmentCondition = new CodeSegment();
                var segmentBody = new CodeSegment();
                var segmentAfter = new CodeSegment();

                segmentBefore.GoesTo(segmentCondition);

                segmentCondition.nodes.Add(new CodeNodeIf());
                segmentCondition.GoesTo(segmentBody);

                var segmentBodyEnd = this.ParseBlock(node.Child(1), segmentBody);
                segmentBodyEnd.GoesTo(segmentCondition);

                segmentCondition.GoesTo(segmentAfter);

                return segmentAfter;
            }
        }
    }
}
