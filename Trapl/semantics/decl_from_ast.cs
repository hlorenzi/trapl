
namespace Trapl.Semantics
{
    public static class DeclASTConverter
    {
        public static void AddToDecls(Infrastructure.Session session, Grammar.ASTNode declASTNode)
        {
            if (declASTNode.ChildIs(1, Grammar.ASTNodeKind.StructDecl))
            {
                var decl = new DeclStruct();
                decl.declASTNode = declASTNode;
                decl.nameASTNode = declASTNode.Child(0);
                decl.pathASTNode = declASTNode.Child(0).Child(0);
                decl.defASTNode = declASTNode.Child(1);
                decl.templateASTNode = TemplateASTUtil.GetTemplateNodeOrNull(declASTNode.Child(0));
                session.structDecls.Add(decl.pathASTNode, decl);
            }
            else if (declASTNode.ChildIs(1, Grammar.ASTNodeKind.FunctDecl))
            {
                var decl = new DeclFunct();
                decl.declASTNode = declASTNode;
                decl.nameASTNode = declASTNode.Child(0);
                decl.pathASTNode = declASTNode.Child(0).Child(0);
                decl.defASTNode = declASTNode.Child(1);
                decl.templateASTNode = TemplateASTUtil.GetTemplateNodeOrNull(declASTNode.Child(0));
                session.functDecls.Add(decl.pathASTNode, decl);
            }
            else
                throw new InternalException("unreachable");
        }
    }
}
