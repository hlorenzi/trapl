using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeDeclNamespace : ASTNode
    {
        public ASTNodePath path;
        public ASTNodeDeclGroup innerGroup;


        public void SetPathNode(ASTNodePath path)
        {
            path.SetParent(this);
            this.AddSpan(path.GetSpan());
            this.path = path;
        }


        public void SetInnerGroupNode(ASTNodeDeclGroup groupNode)
        {
            groupNode.SetParent(this);
            this.AddSpan(groupNode.GetSpan());
            this.innerGroup = groupNode;
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.path;
            yield return this.innerGroup;
        }
    }
}
