using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trapl.Infrastructure
{
    public class Name
    {
        public Diagnostics.Span span;
        public Grammar.ASTNode pathASTNode;
        public Template template;


        public Name()
        {

        }


        public Name(Diagnostics.Span span, Grammar.ASTNode pathASTNode, Template template)
        {
            if (pathASTNode.kind != Grammar.ASTNodeKind.Path)
                throw new InternalException("node is not a Path");

            this.pathASTNode = pathASTNode;
            this.template = template;
        }


        public bool Compare(Name other)
        {
            return
                Semantics.PathUtil.Compare(this.pathASTNode, other.pathASTNode) &&
                this.template.IsMatch(other.template);
        }


        public bool Compare(Grammar.ASTNode pathASTNode, Template template)
        {
            return
                Semantics.PathUtil.Compare(this.pathASTNode, pathASTNode) &&
                this.template.IsMatch(template);
        }


        public string GetString(Infrastructure.Session session)
        {
            return Semantics.PathUtil.GetDisplayString(this.pathASTNode) + this.template.GetString(session);
        }
    }
}
