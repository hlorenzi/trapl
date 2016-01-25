using System.Collections.Generic;


namespace Trapl.Grammar
{
    public abstract class ASTNodeUse : ASTNode
    {

    }


    public class ASTNodeUseAll : ASTNodeUse
    {
        public ASTNodePath path;


        public void SetPathNode(ASTNodePath path)
        {
            path.SetParent(this);
            this.path = path;
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.path;
        }
    }
}
