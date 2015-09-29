using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class CodeAnalyzer
    {
        public static CodeSegment Analyze(Interface.Session session, Grammar.ASTNode node, List<DefFunct.Variable> localVariables, Type returnType)
        {
            var analyzer = new CodeAnalyzer(session, localVariables, returnType);
            var segment = new CodeSegment();
            analyzer.ParseBlock(node, segment);
            localVariables = analyzer.localVariables;
            return segment;
        }


        private Interface.Session session;
        private List<DefFunct.Variable> localVariables;
        private Type returnType;
        private Stack<bool> inAssignmentLhs = new Stack<bool>();


        private CodeAnalyzer(Interface.Session session, List<DefFunct.Variable> localVariables, Type returnType)
        {
            this.session = session;
            this.localVariables = localVariables;
            this.returnType = returnType;
            this.inAssignmentLhs.Push(false);
        }


        private CodeSegment ParseBlock(Grammar.ASTNode node, CodeSegment segment)
        {
            var curLocalIndex = this.localVariables.Count;

            foreach (var exprNode in node.EnumerateChildren())
            {
                try
                {
                    Type type;
                    segment = this.ParseExpression(exprNode, segment, out type);
                    segment.nodes.Add(new CodeNodePop());
                }
                catch (CheckException) { }
            }

            for (int i = this.localVariables.Count - 1; i >= curLocalIndex; i--)
            {
                var v = this.localVariables[i];
                if (v.outOfScope)
                    continue;

                v.outOfScope = true;
                var codeNode = new CodeNodeLocalEnd();
                codeNode.localIndex = i;
                segment.nodes.Add(codeNode);
            }

            return segment;
        }


        private CodeSegment ParseExpression(Grammar.ASTNode node, CodeSegment segment, out Type type)
        {
            if (node.kind == Grammar.ASTNodeKind.Identifier)
                return this.ParseIdentifier(node, segment, out type);
            else if (node.kind == Grammar.ASTNodeKind.ControlLet)
                return this.ParseControlLet(node, segment, out type);
            else if (node.kind == Grammar.ASTNodeKind.ControlIf)
                return this.ParseControlIf(node, segment, out type);
            else if (node.kind == Grammar.ASTNodeKind.ControlWhile)
                return this.ParseControlWhile(node, segment, out type);
            else if (node.kind == Grammar.ASTNodeKind.ControlReturn)
                return this.ParseControlReturn(node, segment, out type);
            else if (node.kind == Grammar.ASTNodeKind.NumberLiteral)
                return this.ParseLiteral(node, segment, out type);
            else if (node.kind == Grammar.ASTNodeKind.BinaryOp)
                return this.ParseBinaryOp(node, segment, out type);
            else if (node.kind == Grammar.ASTNodeKind.UnaryOp)
                return this.ParseUnaryOp(node, segment, out type);
            else if (node.kind == Grammar.ASTNodeKind.Call)
                return this.ParseCall(node, segment, out type);
            else
                throw new InternalException("unimplemented");
        }


        private CodeSegment ParseControlLet(Grammar.ASTNode node, CodeSegment segment, out Type type)
        {
            var varName = node.Child(0).GetExcerpt();
            var varSpan = node.Span();

            var shadowedDecl = this.localVariables.FindLast(v => v.name == varName && !v.outOfScope);
            if (shadowedDecl != null)
            {
                this.session.diagn.Add(MessageKind.Warning, MessageCode.Shadowing,
                    "previous declaration hidden", varSpan, shadowedDecl.declSpan);
            }

            if (node.ChildNumber() == 1)
            {
                this.session.diagn.Add(MessageKind.Error, MessageCode.InferenceImpossible,
                    "type inference impossible without initializer", node.Span());
                throw new CheckException();
            }

            if (node.Child(1).kind == Grammar.ASTNodeKind.TypeName)
            {
                Type varType = ASTTypeUtil.Resolve(this.session, new PatternReplacementCollection(), node.Child(1), false);
                varType.addressable = true;

                if (node.ChildNumber() == 3)
                {
                    Type initializerType;
                    segment = this.ParseExpression(node.Child(2), segment, out initializerType);
                    if (!varType.IsSame(initializerType))
                    {
                        this.session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTypes,
                            "incompatible '" + initializerType.GetString(this.session) + "' initializer",
                            node.Child(2).Span(), node.Child(1).Span());
                    }
                }

                var newVariable = new DefFunct.Variable(varName, varType, varSpan);
                this.localVariables.Add(newVariable);

                var codeNode = new CodeNodeLocalBegin();
                codeNode.localIndex = this.localVariables.Count - 1;
                segment.nodes.Add(codeNode);

                type = new TypeVoid();
                return segment;
            }
            else
            {
                var newVariable = new DefFunct.Variable();
                newVariable.name = varName;
                newVariable.declSpan = varSpan;
                this.localVariables.Add(newVariable);

                var codeNode = new CodeNodeLocalBegin();
                codeNode.localIndex = this.localVariables.Count - 1;
                segment.nodes.Add(codeNode);

                var pushLocalNode = new CodeNodePushLocal();
                pushLocalNode.localIndex = this.localVariables.Count - 1;
                segment.nodes.Add(pushLocalNode);

                Type varType;
                CodeSegment initializerSegmentEnd;
                try
                {
                    initializerSegmentEnd = this.ParseExpression(node.Child(1), segment, out varType);
                }
                catch (CheckException)
                {
                    this.localVariables.Remove(newVariable);
                    throw;
                }

                if (varType is TypeVoid)
                {
                    this.session.diagn.Add(MessageKind.Error, MessageCode.ExplicitVoid,
                        "type inferred to be 'Void'", node.Child(1).Span(), node.Child(0).Span());
                    throw new CheckException();
                }

                newVariable.type = varType;

                var storeNode = new CodeNodeStore();
                initializerSegmentEnd.nodes.Add(storeNode);

                type = new TypeVoid();
                return initializerSegmentEnd;
            }
        }


        private CodeSegment ParseIdentifier(Grammar.ASTNode node, CodeSegment segment, out Type type)
        {
            var varName = node.Span().GetExcerpt();

            // Try to find a local variable with the specified name.
            var localDeclIndex = this.localVariables.FindLastIndex(v => v.name == varName && !v.outOfScope);
            if (localDeclIndex >= 0)
            {
                /*if (this.inAssignmentLhs.Peek())
                {
                    var codeNode = new CodeNodePushLocalReference();
                    codeNode.localIndex = localDeclIndex;
                    segment.nodes.Add(codeNode);
                }
                else*/
                {
                    var codeNode = new CodeNodePushLocal();
                    codeNode.localIndex = localDeclIndex;
                    segment.nodes.Add(codeNode);
                }

                type = this.localVariables[localDeclIndex].type;
                type.addressable = true;
                return segment;
            }


            // Or try to find a funct with the specified name and parameter pattern.
            var nameASTNode = node.Child(0);
            var patternASTNode = new Grammar.ASTNode(Grammar.ASTNodeKind.ParameterPattern, node.Span().JustAfter());
            if (node.ChildIs(1, Grammar.ASTNodeKind.ParameterPattern))
                patternASTNode = node.Child(1);

            var matchingTopDecl = ASTTopDeclFinder.Find(this.session, nameASTNode, patternASTNode);
            if (matchingTopDecl != null)
            {
                // Check that what the matching topdecl defines is a funct.
                var funct = matchingTopDecl.def as DefFunct;
                if (funct == null)
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.UnknownType,
                        "'" + nameASTNode.GetExcerpt() + "' is not a funct", nameASTNode.GetOriginalSpan());
                    throw new Semantics.CheckException();
                }

                matchingTopDecl.ResolveBody(this.session);

                var codeNode = new CodeNodePushFunct();
                codeNode.topDecl = matchingTopDecl;
                segment.nodes.Add(codeNode);

                var functType = new TypeFunct();
                functType.addressable = false;
                functType.returnType = funct.returnType;
                for (int j = 0; j < funct.arguments.Count; j++)
                    functType.argumentTypes.Add(funct.arguments[j].type);
                type = functType;
                return segment;
            }

            // Or couldn't find a match with anything.
            this.session.diagn.Add(MessageKind.Error, MessageCode.UnknownIdentifier,
                "unknown identifier", node.Span());

            var outOfScopeDecl = this.localVariables.FindLast(v => v.name == varName && v.outOfScope);
            if (outOfScopeDecl != null)
            { } // FIXME: Add info message about out-of-scope local.

            throw new CheckException();
        }


        private CodeSegment ParseLiteral(Grammar.ASTNode node, CodeSegment segment, out Type type)
        {
            var codeNode = new CodeNodePushLiteral();
            codeNode.literalExcerpt = node.Span().GetExcerpt();
            segment.nodes.Add(codeNode);

            type = new TypeStruct((DefStruct)this.session.topDecls.Find(d => d.qualifiedName == "Int32").def);
            return segment;
        }


        private CodeSegment ParseBinaryOp(Grammar.ASTNode node, CodeSegment segment, out Type type)
        {
            var op = node.Child(0).Span().GetExcerpt();

            if (op == "=")
            {
                Type lhsType, rhsType;

                this.inAssignmentLhs.Push(true);
                var segment2 = this.ParseExpression(node.Child(1), segment, out lhsType);
                this.inAssignmentLhs.Pop();

                var segment3 = this.ParseExpression(node.Child(2), segment2, out rhsType);

                if (!lhsType.addressable)
                    this.session.diagn.Add(MessageKind.Error, MessageCode.CannotAssign,
                        "expression is not assignable", node.Child(1).Span());

                else if (!lhsType.IsSame(rhsType))
                    this.session.diagn.Add(MessageKind.Error, MessageCode.CannotAssign,
                        "assignment type mismatch: '" + lhsType.GetString(this.session) + "' and " +
                        "'" + rhsType.GetString(this.session) + "'",
                        node.Child(1).Span(), node.Child(2).Span());

                segment3.nodes.Add(new CodeNodeStore());
                type = new TypeVoid();
                return segment3;
            }
            else if (op == ".")
            {
                Type accessedType;
                var segment2 = this.ParseExpression(node.Child(1), segment, out accessedType);

                var accessedTypeStruct = (accessedType as TypeStruct);
                if (accessedTypeStruct == null)
                {
                    this.session.diagn.Add(MessageKind.Error, MessageCode.CannotAssign,
                        "'" + accessedType.GetString(session) + "' expression accessed like a struct",
                        node.Child(1).Span(), node.Child(2).Span());
                    throw new CheckException();
                }

                if (node.Child(2).kind != Grammar.ASTNodeKind.Identifier)
                {
                    this.session.diagn.Add(MessageKind.Error, MessageCode.CannotAssign,
                        "expecting a member name",
                        node.Child(2).Span());
                    throw new CheckException();
                }

                var memberName = node.Child(2).GetExcerpt();
                var memberIndex = accessedTypeStruct.structDef.members.FindIndex(m => m.name == memberName);
                if (memberIndex < 0)
                {
                    this.session.diagn.Add(MessageKind.Error, MessageCode.CannotAssign,
                        "'" + accessedType.GetString(session) + "' struct has no member '" + memberName + "'",
                        node.Child(1).Span(), node.Child(2).Span());
                    throw new CheckException();
                }

                segment2.nodes.Add(new CodeNodeAccess(accessedTypeStruct.structDef, memberIndex));
                type = accessedTypeStruct.structDef.members[memberIndex].type;
                type.addressable = accessedType.addressable;
                return segment2;
            }
            else
            {
                Type lhsType, rhsType;
                var segment2 = this.ParseExpression(node.Child(1), segment, out lhsType);
                var segment3 = this.ParseExpression(node.Child(2), segment2, out rhsType);

                string opName;
                switch (op)
                {
                    case "+": opName = "add"; break;
                    case "-": opName = "sub"; break;
                    case "*": opName = "mul"; break;
                    case "/": opName = "div"; break;
                    case "%": opName = "rem"; break;
                    case "==": opName = "eq"; break;
                    case "!=": opName = "noteq"; break;
                    case "<": opName = "less"; break;
                    case "<=": opName = "lesseq"; break;
                    case ">": opName = "greater"; break;
                    case ">=": opName = "greatereq"; break;
                    case "&": opName = "and"; break;
                    case "|": opName = "or"; break;
                    case "^": opName = "xor"; break;
                    default: throw new InternalException("unimplemented");
                }

                var matchingTopDecl = ASTTopDeclFinder.FindBinaryOpPrimitive(this.session,
                    opName, node.Child(0).Span(),
                    lhsType, node.Child(1).Span(),
                    rhsType, node.Child(2).Span());

                var codeNode = new CodeNodePushFunct();
                codeNode.topDecl = matchingTopDecl;
                segment.nodes.Add(codeNode);
                segment.nodes.Add(new CodeNodeCall());

                type = ((DefFunct)matchingTopDecl.def).returnType;
                return segment;
            }
        }


        private CodeSegment ParseUnaryOp(Grammar.ASTNode node, CodeSegment segment, out Type type)
        {
            var op = node.Child(0).Span().GetExcerpt();

            if (op == "&")
            {
                Type operandType;
                var segment2 = this.ParseExpression(node.Child(1), segment, out operandType);

                if (!operandType.addressable)
                {
                    this.session.diagn.Add(MessageKind.Error, MessageCode.CannotAddress,
                        "expression is not addressable", node.Child(1).Span());
                    throw new CheckException();
                }

                segment2.nodes.Add(new CodeNodeAddress());
                type = new TypePointer(operandType);
                return segment2;
            }
            else if (op == "@")
            {
                Type operandType;
                var segment2 = this.ParseExpression(node.Child(1), segment, out operandType);

                if (!(operandType is TypePointer))
                {
                    this.session.diagn.Add(MessageKind.Error, MessageCode.CannotDereference,
                        "'" + operandType.GetString(this.session) + "' expression is not dereferenceable",
                        node.Child(1).Span());
                    throw new CheckException();
                }

                segment2.nodes.Add(new CodeNodeDereference());
                type = ((TypePointer)operandType).pointeeType;
                type.addressable = true;
                return segment2;
            }

            type = new TypeVoid();
            return segment;
        }


        private CodeSegment ParseCall(Grammar.ASTNode node, CodeSegment segment, out Type type)
        {
            var argTypes = new Type[node.ChildNumber() - 1];
            Type targetType;

            for (int i = node.ChildNumber() - 1; i >= 1; i--)
            {
                segment = this.ParseExpression(node.Child(i), segment, out argTypes[i - 1]);
            }

            segment = this.ParseExpression(node.Child(0), segment, out targetType);

            var targetFunctType = targetType as TypeFunct;
            if (targetFunctType == null)
            {
                this.session.diagn.Add(MessageKind.Error, MessageCode.CannotCall,
                    "'" + targetType.GetString(this.session) + "' expression is not callable",
                    node.Child(0).Span());
                throw new CheckException();
            }

            if (targetFunctType.argumentTypes.Count != argTypes.Length)
            {
                this.session.diagn.Add(MessageKind.Error, MessageCode.WrongArgumentNumber,
                    "wrong number of arguments to '" + targetType.GetString(this.session) + "' funct",
                    node.Span());
                throw new CheckException();
            }

            for (int i = 0; i < argTypes.Length; i++)
            {
                if (!argTypes[i].IsSame(targetFunctType.argumentTypes[i]))
                {
                    this.session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTypes,
                        "incompatible '" + argTypes[i].GetString(this.session) + "' expression " +
                        "to '" + targetFunctType.argumentTypes[i].GetString(this.session) + "' argument",
                        node.Child(i + 1).Span());
                }
            }

            var codeNode = new CodeNodeCall();
            segment.nodes.Add(codeNode);

            type = targetFunctType.returnType;
            return segment;
        }


        private CodeSegment ParseControlIf(Grammar.ASTNode node, CodeSegment segmentBefore, out Type type)
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

            Type conditionType;
            segmentBefore = this.ParseExpression(node.Child(0), segmentBefore, out conditionType);
            if (!(conditionType is TypeStruct) ||
                ((TypeStruct)conditionType).structDef.topDecl.qualifiedName != "Bool")
            {
                this.session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTypes,
                    "'" + conditionType.GetString(this.session) + "' expression used as condition",
                    node.Child(0).Span());
                throw new CheckException();
            }

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

            type = new TypeVoid();
            return segmentAfter;
        }


        private CodeSegment ParseControlWhile(Grammar.ASTNode node, CodeSegment segmentBefore, out Type type)
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

            Type conditionType;
            var segmentConditionEnd = this.ParseExpression(node.Child(0), segmentCondition, out conditionType);
            if (!(conditionType is TypeStruct) ||
                ((TypeStruct)conditionType).structDef.topDecl.qualifiedName != "Bool")
            {
                this.session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTypes,
                    "'" + conditionType.GetString(this.session) + "' expression used as condition",
                    node.Child(0).Span());
                throw new CheckException();
            }

            segmentConditionEnd.nodes.Add(new CodeNodeIf());
            segmentConditionEnd.GoesTo(segmentBody);

            var segmentBodyEnd = this.ParseBlock(node.Child(1), segmentBody);
            segmentBodyEnd.GoesTo(segmentCondition);

            segmentConditionEnd.GoesTo(segmentAfter);

            type = new TypeVoid();
            return segmentAfter;
        }



        private CodeSegment ParseControlReturn(Grammar.ASTNode node, CodeSegment segmentBefore, out Type type)
        {
            Type exprType;
            if (node.ChildNumber() == 1)
            {
                segmentBefore = this.ParseExpression(node.Child(0), segmentBefore, out exprType);

                if (!exprType.IsSame(this.returnType))
                {
                    this.session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTypes,
                        "'" + exprType.GetString(this.session) + "' expression incompatible with '" +
                        this.returnType.GetString(this.session) + "' return type",
                        node.Child(0).Span());
                    throw new CheckException();
                }
            }
            else
            {
                exprType = new TypeVoid();

                if (!exprType.IsSame(this.returnType))
                {
                    this.session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTypes,
                        "return expression incompatible with '" +
                        this.returnType.GetString(this.session) + "' return type",
                        node.Span());
                    throw new CheckException();
                }
            }


            var returnNode = new CodeNodeReturn();
            returnNode.exprType = exprType;
            segmentBefore.nodes.Add(returnNode);

            type = new TypeVoid();
            return segmentBefore;
        }
    }
}
