
namespace Trapl.Semantics
{
    public class Variable
    {
        public Name name;
        public Type type;
        public Diagnostics.Span declSpan;

        public Variable()
        {
        }

        public Variable(Grammar.ASTNode pathASTNode, Type type, Diagnostics.Span declSpan)
        {
            this.name = new Name(pathASTNode.Span(), pathASTNode, new Template());
            this.type = type;
            this.declSpan = declSpan;
        }

        public string GetString(Infrastructure.Session session)
        {
            return this.name.GetString(session);
        }
    }
}