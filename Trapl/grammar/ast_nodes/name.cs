using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeName : ASTNode
    {
        public ASTNodePath path;
        public ASTNodeLifetimeParams lifetimeParams;
        public ASTNodeDeclParams declParams;


        public void SetPathNode(ASTNodePath path)
        {
            path.SetParent(this);
            this.AddSpan(path.GetSpan());
            this.path = path;
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.path;

            if (this.lifetimeParams != null)
                yield return this.lifetimeParams;

            if (this.declParams != null)
                yield return this.declParams;
        }
    }
}
