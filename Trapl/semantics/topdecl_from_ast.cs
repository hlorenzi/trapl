
namespace Trapl.Semantics
{
    public static class TopDeclASTConverter
    {
        public static TopDecl Convert(Infrastructure.Session session, Grammar.ASTNode topDeclASTNode)
        {
            var topDecl = new TopDecl();

            topDecl.declASTNode = topDeclASTNode;
            topDecl.nameASTNode = topDeclASTNode.Child(0);
            topDecl.pathASTNode = topDeclASTNode.Child(0).Child(0);
            topDecl.defASTNode = topDeclASTNode.Child(1);
            topDecl.templateASTNode = TemplateASTUtil.GetTemplateNodeOrNull(topDeclASTNode.Child(0));

            return topDecl;
        }
    }
}
