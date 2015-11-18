﻿
namespace Trapl.Infrastructure
{
    public class Decl
    {
        public Grammar.ASTNode declASTNode;

        public Grammar.ASTNode nameASTNode;
        public Grammar.ASTNode templateASTNode;
        public Name name = new Name();

        public Grammar.ASTNode defASTNode;

        public bool resolved;
        public bool bodyResolved;
        public bool primitive;


        public void ResolveTemplate(Infrastructure.Session session)
        {
            this.name.template = Semantics.TemplateUtil.ResolveFromTemplateAST(session, this.templateASTNode, true);
        }


        public virtual void Resolve(Infrastructure.Session session) { }


        public virtual void ResolveBody(Infrastructure.Session session) { }


        public string GetString(Infrastructure.Session session)
        {
            return Semantics.PathUtil.GetDisplayString(this.name.pathASTNode) +
                (this.name.template == null ? "<?>" : this.name.template.GetString(session));
        }

        public virtual void PrintToConsole(Infrastructure.Session session, int indentLevel)
        {

        }
    }
}