using System;
using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class Analyzer 
    {
        public static Output Pass(Structure.Output syn, Diagnostics.Collection diagn)
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
        private Diagnostics.Collection diagn;


        private Analyzer(Structure.Output syn, Diagnostics.Collection diagn)
        {
            this.output = new Output();
            this.syn = syn;
            this.diagn = diagn;
        }


        private VariableType ResolveType(Grammar.ASTNode node, SourceCode source, bool voidAllowed = false)
        {
            if (node.kind != Grammar.ASTNodeKind.TypeName)
                throw new ParserException();

            int pointerLevel = 0;
            int curChild = 0;
            while (node.ChildNumber() > curChild && node.Child(curChild).kind == Grammar.ASTNodeKind.Operator)
            {
                pointerLevel += 1;
                curChild += 1;
            }
            var name = source.GetExcerpt(node.Child(curChild).Span());

            var structDefWithName = this.output.structDefs.Find(s => s.name == name);
            if (structDefWithName != null)
            {
                var type = new VariableTypeStruct();
                type.structDef = structDefWithName;

                if (!voidAllowed && type.IsSame(this.MakeVoidType()))
                {
                    this.diagn.Add(MessageKind.Error, MessageCode.ExplicitVoid,
                        "'Void' type used explicitly", source, node.Span());
                    throw new ParserException();
                }

                VariableType finalType = type;
                while (pointerLevel > 0)
                {
                    finalType = new VariableTypePointer(finalType);
                    pointerLevel -= 1;
                }

                return finalType;
            }

            this.diagn.Add(MessageKind.Error, MessageCode.UnknownType,
                "unknown type", source, node.Span());
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
                    if (memberNode.kind != Grammar.ASTNodeKind.StructMemberDecl)
                        continue;

                    try
                    {
                        var memberDef = new StructDef.Member();
                        memberDef.name = src.GetExcerpt(memberNode.Child(0).Span());
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
                    this.diagn.Add(MessageKind.Error, MessageCode.StructRecursion,
                        "infinite struct member recursion", st.source, st.declSpan);
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


        private TemplateList ParseTemplateList(Grammar.ASTNode node, SourceCode src)
        {
            var templList = new TemplateList();
            if (node != null)
            {
                foreach (var paramNode in node.EnumerateChildren())
                {
                    if (paramNode.kind == Grammar.ASTNodeKind.TypeName)
                    {
                        var param = new TemplateList.Parameter();
                        param.kind = TemplateList.ParameterKind.Specific;
                        param.specificType = this.ResolveType(paramNode, src, true);
                        templList.parameters.Add(param);
                    }
                    else if (paramNode.kind == Grammar.ASTNodeKind.TemplateType)
                    {
                        var param = new TemplateList.Parameter();
                        param.kind = TemplateList.ParameterKind.Generic;
                        param.genericName = ""; // FIXME!
                        templList.parameters.Add(param);
                    }
                }
            }
            return templList;
        }


        private void ParseFunctDecls()
        {
            foreach (var decl in this.syn.functDecls)
            {
                try
                {
                    var templList = this.ParseTemplateList(decl.templateListNode, decl.source);

                    if (templList.IsGeneric())
                    {
                        this.output.templFunctDecls.Add(decl);
                        continue;
                    }

                    var functName = decl.name;
                    var funct = new FunctDef(functName, decl.source, decl.nameSpan, decl.syntaxNode.Span());
                    funct.templateList = templList;

                    // Parse arguments.
                    foreach (var argNode in decl.syntaxNode.EnumerateChildren())
                    {
                        if (argNode.kind != Grammar.ASTNodeKind.FunctArgDecl)
                            continue;

                        var argName = decl.source.GetExcerpt(argNode.Child(0).Span());
                        var argType = this.ResolveType(argNode.Child(1), decl.source);
                        funct.arguments.Add(new FunctDef.Variable(argName, argType, argNode.Span()));
                        funct.localVariables.Add(new FunctDef.Variable(argName, argType, argNode.Span()));
                    }

                    // Parse return type.
                    funct.returnType = this.MakeVoidType();
                    foreach (var argNode in decl.syntaxNode.EnumerateChildren())
                    {
                        if (argNode.kind != Grammar.ASTNodeKind.FunctReturnDecl)
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
                    decl.syntaxNode.ChildWithKind(Grammar.ASTNodeKind.Block),
                    funct);
            }
        }


        private class FunctBodyAnalyzer
        {
            public static CodeSegment Analyze(Semantics.Analyzer owner, Grammar.ASTNode node, FunctDef funct)
            {
                var analyzer = new FunctBodyAnalyzer(owner, funct);
                var segment = new CodeSegment();
                analyzer.ParseBlock(node, segment);
                return segment;
            }


            private Semantics.Analyzer owner;
            private FunctDef funct;
            private VariableType[] callContext;


            private FunctBodyAnalyzer(Semantics.Analyzer owner, FunctDef funct)
            {
                this.owner = owner;
                this.funct = funct;
                this.callContext = null;
            }


            private CodeSegment ParseBlock(Grammar.ASTNode node, CodeSegment segment)
            {
                var curLocalIndex = this.funct.localVariables.Count;

                foreach (var exprNode in node.EnumerateChildren())
                {
                    try
                    {
                        VariableType type;
                        segment = this.ParseExpression(exprNode, segment, out type);
                        if (!type.IsSame(this.owner.MakeVoidType()))
                            segment.nodes.Add(new CodeNodePop());
                    }
                    catch (ParserException) { }
                }

                for (int i = this.funct.localVariables.Count - 1; i >= curLocalIndex; i--)
                {
                    var v = this.funct.localVariables[i];
                    if (v.outOfScope)
                        continue;

                    v.outOfScope = true;
                    var codeNode = new CodeNodeLocalEnd();
                    codeNode.localIndex = i;
                    segment.nodes.Add(codeNode);
                }

                return segment;
            }


            private CodeSegment ParseExpression(Grammar.ASTNode node, CodeSegment segment, out VariableType type)
            {
                if (node.kind == Grammar.ASTNodeKind.Identifier)
                    return this.ParseIdentifier(node, segment, out type);
                else
                {
                    this.callContext = null;

                    if (node.kind == Grammar.ASTNodeKind.ControlLet)
                        return this.ParseControlLet(node, segment, out type);
                    else if (node.kind == Grammar.ASTNodeKind.ControlIf)
                        return this.ParseControlIf(node, segment, out type);
                    else if (node.kind == Grammar.ASTNodeKind.ControlWhile)
                        return this.ParseControlWhile(node, segment, out type);
                    else if (node.kind == Grammar.ASTNodeKind.NumberLiteral)
                        return this.ParseLiteral(node, segment, out type);
                    else if (node.kind == Grammar.ASTNodeKind.BinaryOp)
                        return this.ParseBinaryOp(node, segment, out type);
                    else if (node.kind == Grammar.ASTNodeKind.UnaryOp)
                        return this.ParseUnaryOp(node, segment, out type);
                    else if (node.kind == Grammar.ASTNodeKind.Call)
                        return this.ParseCall(node, segment, out type);
                    else
                        throw new ParserException();
                }
            }


            private CodeSegment ParseControlLet(Grammar.ASTNode node, CodeSegment segment, out VariableType type)
            {
                var varName = this.funct.source.GetExcerpt(node.Child(0).Span());
                var varSpan = node.Span();

                var shadowedDecl = this.funct.localVariables.FindLast(v => v.name == varName && !v.outOfScope);
                if (shadowedDecl != null)
                {
                    this.owner.diagn.Add(MessageKind.Warning, MessageCode.Shadowing,
                        "previous declaration hidden",
                        MessageCaret.Primary(this.funct.source, varSpan),
                        MessageCaret.Primary(this.funct.source, shadowedDecl.declSpan));
                }

                if (node.ChildNumber() == 1)
                {
                    this.owner.diagn.Add(MessageKind.Error, MessageCode.InferenceImpossible,
                        "type inference impossible without initializer", this.funct.source, node.Span());
                    throw new ParserException();
                }

                VariableType varType;
                if (node.Child(1).kind == Grammar.ASTNodeKind.TypeName)
                {
                    varType = this.owner.ResolveType(node.Child(1), this.funct.source, false);
                    varType.addressable = true;

                    if (node.ChildNumber() == 3)
                    {
                        VariableType initializerType;
                        segment = this.ParseExpression(node.Child(2), segment, out initializerType);
                        if (!varType.IsSame(initializerType))
                        {
                            this.owner.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTypes,
                                "incompatible '" + initializerType.Name() + "' initializer",
                                MessageCaret.Primary(this.funct.source, node.Child(2).Span()),
                                MessageCaret.Primary(this.funct.source, node.Child(1).Span()));
                        }
                    }

                    var newVariable = new FunctDef.Variable(varName, varType, varSpan);
                    this.funct.localVariables.Add(newVariable);

                    var codeNode = new CodeNodeLocalBegin();
                    codeNode.localIndex = this.funct.localVariables.Count - 1;
                    segment.nodes.Add(codeNode);

                    type = this.owner.MakeVoidType();
                    return segment;
                }
                else
                {
                    var initializerSegment = new CodeSegment();
                    var initializerSegmentEnd = this.ParseExpression(node.Child(1), initializerSegment, out varType);

                    if (varType.IsSame(this.owner.MakeVoidType()))
                    {
                        this.owner.diagn.Add(MessageKind.Error, MessageCode.ExplicitVoid,
                            "type inferred to be 'Void'",
                            MessageCaret.Primary(this.funct.source, node.Child(1).Span()),
                            MessageCaret.Primary(this.funct.source, node.Child(0).Span()));
                        throw new ParserException();
                    }

                    var newVariable = new FunctDef.Variable(varName, varType, varSpan);
                    this.funct.localVariables.Add(newVariable);

                    var codeNode = new CodeNodeLocalBegin();
                    codeNode.localIndex = this.funct.localVariables.Count - 1;
                    segment.nodes.Add(codeNode);

                    var pushLocalNode = new CodeNodePushLocal();
                    pushLocalNode.localIndex = this.funct.localVariables.Count - 1;
                    segment.nodes.Add(pushLocalNode);

                    segment.GoesTo(initializerSegment);

                    var storeNode = new CodeNodeStore();
                    initializerSegmentEnd.nodes.Add(storeNode);

                    type = this.owner.MakeVoidType();
                    return initializerSegmentEnd;
                }
            }


            private CodeSegment ParseIdentifier(Grammar.ASTNode node, CodeSegment segment, out VariableType type)
            {
                var varName = this.funct.source.GetExcerpt(node.Span());
                var callCtx = this.callContext;
                this.callContext = null;

                var localDeclIndex = this.funct.localVariables.FindLastIndex(v => v.name == varName && !v.outOfScope);
                if (localDeclIndex >= 0)
                {
                    var codeNode = new CodeNodePushLocal();
                    codeNode.localIndex = localDeclIndex;
                    segment.nodes.Add(codeNode);

                    type = this.funct.localVariables[localDeclIndex].type;
                    return segment;
                }
                
                for (int i = 0; i < this.owner.output.functDefs.Count; i++)
                {
                    var fn = this.owner.output.functDefs[i];
                    if (fn.name != varName)
                        continue;

                    if (fn.templateList.IsGeneric())
                        continue;

                    if (fn.templateList.parameters.Count > 0)
                    {
                        if (callCtx == null)
                            continue;

                        if (fn.templateList.parameters.Count != callCtx.Length)
                            continue;

                        bool match = true;
                        for (int j = 0; j < fn.templateList.parameters.Count; j++)
                        {
                            if (!fn.templateList.parameters[j].specificType.IsSame(callCtx[j]))
                                match = false;
                        }

                        if (!match)
                            continue;
                    }

                    var codeNode = new CodeNodePushFunct();
                    codeNode.functIndex = i;
                    segment.nodes.Add(codeNode);

                    var functType = new VariableTypeFunct();
                    functType.addressable = false;
                    functType.returnType = fn.returnType;
                    for (int j = 0; j < fn.arguments.Count; j++)
                        functType.argumentTypes.Add(fn.arguments[j].type);
                    type = functType;
                    return segment;
                }

                this.owner.diagn.Add(MessageKind.Error, MessageCode.UnknownIdentifier,
                    "unknown identifier", this.funct.source, node.Span());

                var outOfScopeDecl = this.funct.localVariables.FindLast(v => v.name == varName && v.outOfScope);
                if (outOfScopeDecl != null)
                    { } // FIXME: Add info message about out-of-scope local.

                throw new ParserException();
            }


            private CodeSegment ParseLiteral(Grammar.ASTNode node, CodeSegment segment, out VariableType type)
            {
                var codeNode = new CodeNodePushLiteral();
                codeNode.literalExcerpt = this.funct.source.GetExcerpt(node.Span());
                segment.nodes.Add(codeNode);

                var stType = new VariableTypeStruct();
                stType.structDef = this.owner.output.structDefs.Find(s => s.name == "Int32");
                type = stType;
                return segment;
            }


            private CodeSegment ParseBinaryOp(Grammar.ASTNode node, CodeSegment segment, out VariableType type)
            {
                var op = this.funct.source.GetExcerpt(node.Child(0).Span());

                if (op == "=")
                {
                    VariableType lhsType, rhsType;
                    var segment2 = this.ParseExpression(node.Child(1), segment, out lhsType);
                    var segment3 = this.ParseExpression(node.Child(2), segment2, out rhsType);

                    if (!lhsType.addressable)
                        this.owner.diagn.Add(MessageKind.Error, MessageCode.CannotAssign,
                            "expression is not assignable", this.funct.source, node.Child(1).Span());

                    else if (!lhsType.IsSame(rhsType))
                        this.owner.diagn.Add(MessageKind.Error, MessageCode.CannotAssign,
                            "assignment type mismatch: '" + lhsType.Name() + "' and " +
                            "'" + rhsType.Name() + "'",
                            MessageCaret.Primary(this.funct.source, node.Child(1).Span()),
                            MessageCaret.Primary(this.funct.source, node.Child(2).Span()));

                    segment3.nodes.Add(new CodeNodeStore());
                    type = this.owner.MakeVoidType();
                    return segment3;
                }

                type = this.owner.MakeVoidType();
                return segment;
            }


            private CodeSegment ParseUnaryOp(Grammar.ASTNode node, CodeSegment segment, out VariableType type)
            {
                var op = this.funct.source.GetExcerpt(node.Child(0).Span());

                if (op == "&")
                {
                    VariableType operandType;
                    var segment2 = this.ParseExpression(node.Child(1), segment, out operandType);

                    if (!operandType.addressable)
                    {
                        this.owner.diagn.Add(MessageKind.Error, MessageCode.CannotAddress,
                            "expression is not addressable",
                            this.funct.source, node.Child(1).Span());
                        throw new ParserException();
                    }

                    segment2.nodes.Add(new CodeNodeAddress());
                    type = new VariableTypePointer(operandType);
                    return segment2;
                }
                else if (op == "@")
                {
                    VariableType operandType;
                    var segment2 = this.ParseExpression(node.Child(1), segment, out operandType);

                    if (!(operandType is VariableTypePointer))
                    {
                        this.owner.diagn.Add(MessageKind.Error, MessageCode.CannotDereference,
                            "'" + operandType.Name() + "' expression is not dereferenceable",
                            this.funct.source, node.Child(1).Span());
                        throw new ParserException();
                    }

                    segment2.nodes.Add(new CodeNodeDereference());
                    type = ((VariableTypePointer)operandType).pointeeType;
                    type.addressable = true;
                    return segment2;
                }

                type = this.owner.MakeVoidType();
                return segment;
            }


            private CodeSegment ParseCall(Grammar.ASTNode node, CodeSegment segment, out VariableType type)
            {
                var argTypes = new VariableType[node.ChildNumber() - 1];
                VariableType targetType;

                for (int i = 1; i < node.ChildNumber(); i++)
                {
                    segment = this.ParseExpression(node.Child(i), segment, out argTypes[i - 1]);
                }

                this.callContext = argTypes;
                segment = this.ParseExpression(node.Child(0), segment, out targetType);

                var targetFunctType = targetType as VariableTypeFunct;
                if (targetFunctType == null)
                {
                    this.owner.diagn.Add(MessageKind.Error, MessageCode.CannotCall,
                        "'" + targetType.Name() + "' expression is not callable",
                        this.funct.source, node.Child(0).Span());
                    throw new ParserException();
                }

                if (targetFunctType.argumentTypes.Count != argTypes.Length)
                {
                    this.owner.diagn.Add(MessageKind.Error, MessageCode.WrongArgumentNumber,
                        "wrong number of arguments to '" + targetType.Name() + "' funct",
                        this.funct.source, node.Span());
                    throw new ParserException();
                }

                for (int i = 0; i < argTypes.Length; i++)
                {
                    if (!argTypes[i].IsSame(targetFunctType.argumentTypes[i]))
                    {
                        this.owner.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTypes,
                            "incompatible '" + argTypes[i].Name() + "' expression " +
                            "to '" + targetFunctType.argumentTypes[i].Name() + "' argument",
                            this.funct.source, node.Child(i + 1).Span());
                    }
                }

                var codeNode = new CodeNodeCall();
                segment.nodes.Add(codeNode);

                type = targetFunctType.returnType;
                return segment;
            }


            private CodeSegment ParseControlIf(Grammar.ASTNode node, CodeSegment segmentBefore, out VariableType type)
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
                         SEGMENT AFTER                          SEGMENT AFTER
                         
                */

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

                type = this.owner.MakeVoidType();
                return segmentAfter;
            }


            private CodeSegment ParseControlWhile(Grammar.ASTNode node, CodeSegment segmentBefore, out VariableType type)
            {
                /*    SEGMENT BEFORE
                        |
                        v
                  +-> SEGMENT CONDITION
                  |               |
                  |       +-------+-------+
                  |       |               |
                  |       v               v
                  +-- SEGMENT BODY    SEGMENT AFTER
                
                */

                var segmentCondition = new CodeSegment();
                var segmentBody = new CodeSegment();
                var segmentAfter = new CodeSegment();

                segmentBefore.GoesTo(segmentCondition);

                segmentCondition.nodes.Add(new CodeNodeIf());
                segmentCondition.GoesTo(segmentBody);

                var segmentBodyEnd = this.ParseBlock(node.Child(1), segmentBody);
                segmentBodyEnd.GoesTo(segmentCondition);

                segmentCondition.GoesTo(segmentAfter);

                type = this.owner.MakeVoidType();
                return segmentAfter;
            }
        }
    }
}
