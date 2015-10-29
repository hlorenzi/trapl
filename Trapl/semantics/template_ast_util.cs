using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public static class TemplateASTUtil
    {
        public static Template ResolveTemplate(Infrastructure.Session session, Grammar.ASTNode templateASTNode)
        {
            if (templateASTNode == null)
            {
                var noTempl = new Template();
                noTempl.unconstrained = true;
                return noTempl;
            }

            if (templateASTNode.kind != Grammar.ASTNodeKind.TemplateList)
                throw new InternalException("node is not a TemplateList");

            var templ = new Template();

            foreach (var param in templateASTNode.EnumerateChildren())
            {
                templ.parameters.Add(ResolveParameter(session, param));
            }

            return templ;
        }


        public static Template ResolveTemplateFromName(Infrastructure.Session session, Grammar.ASTNode nameASTNode)
        {
            if (nameASTNode.kind != Grammar.ASTNodeKind.Name)
                throw new InternalException("node is not a Name");

            return ResolveTemplate(session, GetTemplateNodeOrNull(nameASTNode));
        }


        public static Grammar.ASTNode GetTemplateNodeOrNull(Grammar.ASTNode node)
        {
            if (node.kind != Grammar.ASTNodeKind.Name)
                throw new InternalException("node is not a Name");

            if (node.ChildNumber() >= 2 && node.ChildIs(1, Grammar.ASTNodeKind.TemplateList))
                return node.Child(1);
            else
                return null;
        }


        private static Template.Parameter ResolveParameter(Infrastructure.Session session, Grammar.ASTNode paramASTNode)
        {
            if (paramASTNode.kind != Grammar.ASTNodeKind.TemplateParameter)
                throw new InternalException("node is not a TemplateParameter");

            var contentsASTNode = paramASTNode.Child(0);
            if (contentsASTNode.kind == Grammar.ASTNodeKind.Type)
            {
                var param = new Template.ParameterType();
                param.type = TypeASTUtil.Resolve(session, contentsASTNode);
                return param;
            }
            else
            {
                throw new InternalException("unimplemented");
            }
        }
    }
}
