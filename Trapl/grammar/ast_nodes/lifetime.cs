using System.Collections.Generic;

namespace Trapl.Grammar
{
    public abstract class ASTNodeLifetime : ASTNode
    {

    }


    public class ASTNodeConcreteLifetime : ASTNodeLifetime
    {
        public ASTNodeIdentifier identifier;


        public void SetIdentifierNode(ASTNodeIdentifier identifier)
        {
            identifier.SetParent(this);
            this.AddSpan(identifier.GetSpan());
            this.identifier = identifier;
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.identifier;
        }
    }


    public class ASTNodePlaceholderLifetime : ASTNodeLifetime
    {

    }
}
