
namespace Trapl.Semantics
{
    public class Variable
    {
        public Grammar.ASTNode pathASTNode;
        public Template template;
        public Type type;
        public Diagnostics.Span declSpan;

        public Variable()
        {
        }

        public Variable(Grammar.ASTNode pathASTNode, Type type, Diagnostics.Span declSpan)
        {
            this.pathASTNode = pathASTNode;
            this.template = new Template();
            this.type = type;
            this.declSpan = declSpan;
        }

        public string GetString(Infrastructure.Session session)
        {
            return ASTPathUtil.GetString(this.pathASTNode) + this.template.GetString(session);
        }
    }
}