﻿using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class ASTCodeConverter
    {
        public static CodeBody Convert(Infrastructure.Session session, Grammar.ASTNode astNode, List<Variable> localVariables, Type returnType)
        {
            var analyzer = new ASTCodeConverter(session, localVariables, returnType);
            var body = new CodeBody();
            body.code = analyzer.ParseBlock(astNode);
            body.localVariables = localVariables;
            return body;
        }


        private Infrastructure.Session session;
        private List<Variable> localVariables;
        private Type returnType;
        private Stack<bool> inAssignmentLhs = new Stack<bool>();


        private ASTCodeConverter(Infrastructure.Session session, List<Variable> localVariables, Type returnType)
        {
            this.session = session;
            this.localVariables = localVariables;
            this.returnType = returnType;
            this.inAssignmentLhs.Push(false);
        }


        private CodeNode ParseBlock(Grammar.ASTNode astNode)
        {
            var code = new CodeNodeSequence();
            code.span = astNode.Span();
            code.outputType = new TypeVoid();

            foreach (var exprNode in astNode.EnumerateChildren())
            {
                try
                {
                    code.children.Add(this.ParseExpression(exprNode));
                }
                catch (CheckException) { }
            }

            return code;
        }


        private CodeNode ParseExpression(Grammar.ASTNode astNode)
        {
            if (astNode.kind == Grammar.ASTNodeKind.Block)
                return this.ParseBlock(astNode);
            else if (astNode.kind == Grammar.ASTNodeKind.ControlLet)
                return this.ParseControlLet(astNode);
            else if (astNode.kind == Grammar.ASTNodeKind.Call)
                return this.ParseCall(astNode);
            else if (astNode.kind == Grammar.ASTNodeKind.BinaryOp)
                return this.ParseBinaryOp(astNode);
            else if (astNode.kind == Grammar.ASTNodeKind.StructLiteral)
                return this.ParseStructLiteral(astNode);
            else if (astNode.kind == Grammar.ASTNodeKind.Name)
                return this.ParseName(astNode);
            else
                throw new InternalException("not implemented");
        }


        private CodeNode ParseControlLet(Grammar.ASTNode astNode)
        {
            var code = new CodeNodeControlLet();
            code.span = astNode.Span();
            code.outputType = new TypeVoid();

            var local = new Variable();
            local.declSpan = astNode.Child(0).Span();

            local.pathASTNode = astNode.Child(0).Child(0);
            local.template = ASTTemplateUtil.ResolveTemplateFromName(this.session, astNode.Child(0));
            local.template.unconstrained = false;

            local.type = new TypeUnconstrained();
            if (astNode.ChildIs(1, Grammar.ASTNodeKind.Type))
            {
                local.type = ASTTypeUtil.Resolve(this.session, astNode.Child(1));
            }

            this.localVariables.Add(local);

            return code;
        }


        private CodeNode ParseCall(Grammar.ASTNode astNode)
        {
            var code = new CodeNodeCall();
            code.span = astNode.Span();
            code.outputType = new TypeUnconstrained();

            foreach (var child in astNode.children)
                code.children.Add(this.ParseExpression(child));

            return code;
        }


        private CodeNode ParseBinaryOp(Grammar.ASTNode astNode)
        {
            var op = astNode.Child(0).GetExcerpt();

            if (op == "=")
            {
                var code = new CodeNodeAssign();
                code.span = astNode.Span();

                inAssignmentLhs.Push(true);
                code.children.Add(this.ParseExpression(astNode.Child(1)));
                inAssignmentLhs.Pop();

                code.children.Add(this.ParseExpression(astNode.Child(2)));
                code.outputType = new TypeVoid();

                return code;
            }
            else
                throw new InternalException("not implemented");
        }


        private CodeNode ParseStructLiteral(Grammar.ASTNode astNode)
        {
            var code = new CodeNodeStructLiteral();
            code.span = astNode.Span();
            code.outputType = ASTTypeUtil.Resolve(session, astNode.Child(0));

            return code;
        }


        private CodeNode ParseName(Grammar.ASTNode astNode)
        {
            var templ = ASTTemplateUtil.ResolveTemplateFromName(this.session, astNode);

            var localIndex = this.localVariables.FindLastIndex(
                loc => (
                ASTPathUtil.Compare(loc.pathASTNode, astNode.Child(0)) &&
                loc.template.IsMatch(templ)));

            if (localIndex >= 0)
            {
                if (this.inAssignmentLhs.Peek())
                {
                    var code = new CodeNodeLocalAddress();
                    code.span = astNode.Span();
                    code.localIndex = localIndex;
                    code.outputType = new TypeUnconstrained();

                    return code;
                }
                else
                {
                    var code = new CodeNodeLocalValue();
                    code.span = astNode.Span();
                    code.localIndex = localIndex;
                    code.outputType = new TypeUnconstrained();

                    return code;
                }
            }

            var isFunct = ASTTopDeclFinder.IsFunct(this.session, astNode);
            if (isFunct)
            {
                var code = new CodeNodeFunct();
                code.span = astNode.Span();
                code.nameInference.pathASTNode = astNode.Child(0);
                code.nameInference.template = ASTTemplateUtil.ResolveTemplateFromName(this.session, astNode);
                code.potentialFuncts = ASTTopDeclFinder.FindFunctsNamed(this.session, astNode).ConvertAll(d => (DefFunct)d.def);
                code.outputType = new TypeUnconstrained();

                return code;
            }

            session.diagn.Add(MessageKind.Error, MessageCode.UndeclaredIdentifier,
                "'" + ASTPathUtil.GetString(astNode.Child(0)) + "' is not declared",
                astNode.Span());
            throw new CheckException();
        }
    }
}
