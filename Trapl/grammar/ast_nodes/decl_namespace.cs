using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeDeclNamespace : ASTNode
    {
        public ASTNodePath path;
        public List<ASTNodeUse> useDirectives = new List<ASTNodeUse>();
        public List<ASTNode> innerDecls = new List<ASTNode>();


        public void SetPathNode(ASTNodePath path)
        {
            path.SetParent(this);
            this.AddSpan(path.GetSpan());
            this.path = path;
        }


        public void AddUseNode(ASTNodeUse use)
        {
            use.SetParent(this);
            this.AddSpan(use.GetSpan());
            this.useDirectives.Add(use);
        }


        public void AddInnerNode(ASTNode decl)
        {
            decl.SetParent(this);
            this.AddSpan(decl.GetSpan());
            this.innerDecls.Add(decl);
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.path;

            foreach (var use in this.useDirectives)
                yield return use;

            foreach (var decl in this.innerDecls)
                yield return decl;
        }
    }
}
