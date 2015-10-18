
namespace Trapl.Semantics
{
    public static class ASTTopDeclConverter
    {
        public static TopDecl Convert(Infrastructure.Session session, Grammar.ASTNode topDeclASTNode)
        {
            var topDecl = new TopDecl();

            try
            {
                topDecl.declASTNode = topDeclASTNode;
                topDecl.nameASTNode = topDeclASTNode.Child(0);
                topDecl.pathASTNode = topDeclASTNode.Child(0).Child(0);
                topDecl.defASTNode = topDeclASTNode.Child(1);
                topDecl.template = ASTTemplateUtil.ResolveTemplateFromName(session, topDecl.nameASTNode);
            }
            catch (CheckException) { }

            return topDecl;
        }
    }
}
