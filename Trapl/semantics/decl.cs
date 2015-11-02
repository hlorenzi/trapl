using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class Decl
    {
        public Grammar.ASTNode declASTNode;

        public Grammar.ASTNode nameASTNode;
        public Grammar.ASTNode pathASTNode;
        public Grammar.ASTNode templateASTNode;
        public Template template;

        public Grammar.ASTNode defASTNode;

        public bool resolved;
        public bool bodyResolved;


        public void ResolveTemplate(Infrastructure.Session session)
        {
            this.template = TemplateASTUtil.ResolveTemplate(session, this.templateASTNode, true);
        }


        public virtual void Resolve(Infrastructure.Session session) { }


        public virtual void ResolveBody(Infrastructure.Session session) { }


        public string GetString(Infrastructure.Session session)
        {
            return PathASTUtil.GetString(this.pathASTNode) +
                (template == null ? "<?>" : template.GetString(session));
        }

        public virtual void PrintToConsole(Infrastructure.Session session, int indentLevel) { }
    }
}
