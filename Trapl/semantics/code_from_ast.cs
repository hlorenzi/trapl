using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class CodeASTConverter
    {
        public static CodeBody Convert(Infrastructure.Session session, Grammar.ASTNode astNode, List<Variable> localVariables, Type returnType)
        {
            var analyzer = new CodeASTConverter(session, localVariables, returnType);
            var body = new CodeBody();
            body.code = analyzer.ParseBlock(astNode);
            body.localVariables = localVariables;
            return body;
        }


        private Infrastructure.Session session;
        private List<Variable> localVariables;
        private Type returnType;


        private CodeASTConverter(Infrastructure.Session session, List<Variable> localVariables, Type returnType)
        {
            this.session = session;
            this.localVariables = localVariables;
            this.returnType = returnType;
        }


        private CodeNode ParseBlock(Grammar.ASTNode astNode)
        {
            var code = new CodeNodeSequence();
            code.span = astNode.Span();
            code.outputType = new TypeTuple();

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
            else if (astNode.kind == Grammar.ASTNodeKind.UnaryOp)
                return this.ParseUnaryOp(astNode);
            else if (astNode.kind == Grammar.ASTNodeKind.StructLiteral)
                return this.ParseStructLiteral(astNode);
            else if (astNode.kind == Grammar.ASTNodeKind.Name)
                return this.ParseName(astNode);
            else
                throw new InternalException("unimplemented");
        }


        private CodeNode ParseControlLet(Grammar.ASTNode astNode)
        {
            var code = new CodeNodeControlLet();
            code.span = astNode.Span();
            code.outputType = new TypeTuple();

            var local = new Variable();
            local.declSpan = astNode.Child(0).Span();

            local.pathASTNode = astNode.Child(0).Child(0);
            local.template = TemplateASTUtil.ResolveTemplateFromName(this.session, astNode.Child(0), true);
            local.template.unconstrained = false;

            local.type = new TypePlaceholder();
            if (astNode.ChildNumber() >= 2 &&
                TypeASTUtil.IsTypeNode(astNode.Child(1).kind))
            {
                local.type = TypeASTUtil.Resolve(this.session, astNode.Child(1), false);
            }

            this.localVariables.Add(local);

            return code;
        }


        private CodeNode ParseCall(Grammar.ASTNode astNode)
        {
            var code = new CodeNodeCall();
            code.span = astNode.Span();
            code.outputType = new TypePlaceholder();

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

                code.children.Add(this.ParseExpression(astNode.Child(1)));
                code.children.Add(this.ParseExpression(astNode.Child(2)));
                code.outputType = new TypeTuple();

                return code;
            }
            else
                throw new InternalException("not implemented");
        }


        private CodeNode ParseUnaryOp(Grammar.ASTNode astNode)
        {
            var op = astNode.Child(0).GetExcerpt();

            if (op == "&")
            {
                var code = new CodeNodeAddress();
                code.span = astNode.Span();
                code.children.Add(this.ParseExpression(astNode.Child(1)));
                code.outputType = new TypeReference(new TypePlaceholder());

                return code;
            }
            else if (op == "@")
            {
                var code = new CodeNodeDereference();
                code.span = astNode.Span();
                code.children.Add(this.ParseExpression(astNode.Child(1)));
                code.outputType = new TypePlaceholder();

                return code;
            }
            else
                throw new InternalException("not implemented");
        }


        private CodeNode ParseStructLiteral(Grammar.ASTNode astNode)
        {
            var code = new CodeNodeStructLiteral();
            code.span = astNode.Span();
            code.outputType = TypeASTUtil.Resolve(session, astNode.Child(0), false);

            return code;
        }


        private CodeNode ParseName(Grammar.ASTNode astNode)
        {
            var templ = TemplateASTUtil.ResolveTemplateFromName(this.session, astNode, false);

            var localIndex = this.localVariables.FindLastIndex(
                loc => (
                PathASTUtil.Compare(loc.pathASTNode, astNode.Child(0)) &&
                loc.template.IsMatch(templ)));

            if (localIndex >= 0)
            {
                var code = new CodeNodeLocal();
                code.span = astNode.Span();
                code.localIndex = localIndex;
                code.outputType = new TypePlaceholder();

                return code;
            }

            var functList = session.functDecls.GetDeclsClone(astNode.Child(0));
            if (functList.Count > 0)
            {
                var code = new CodeNodeFunct();
                code.span = astNode.Span();
                code.nameInference.pathASTNode = astNode.Child(0);
                code.nameInference.template = templ;
                code.potentialFuncts = functList;
                code.outputType = new TypePlaceholder();

                return code;
            }

            session.diagn.Add(MessageKind.Error, MessageCode.UndeclaredIdentifier,
                "'" + PathASTUtil.GetString(astNode.Child(0)) + "' is not declared",
                astNode.Span());
            throw new CheckException();
        }
    }
}
