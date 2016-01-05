using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeTypeReference : ASTNodeType
    {
        public ASTNodeType referenced;
        public ASTNodeLifetime lifetime;


        public void SetReferencedNode(ASTNodeType referenced)
        {
            referenced.SetParent(this);
            this.AddSpan(referenced.GetSpan());
            this.referenced = referenced;
        }


        public void SetLifetimeNode(ASTNodeLifetime lifetime)
        {
            lifetime.SetParent(this);
            this.AddSpan(lifetime.GetSpan());
            this.lifetime = lifetime;
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            if (this.lifetime != null)
                yield return this.lifetime;

            yield return this.referenced;
        }
    }
}
