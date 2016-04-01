using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeTypePointer : ASTNodeType
    {
        public bool mutable;
        public ASTNodeType referenced;


        public void SetReferencedNode(ASTNodeType referenced)
        {
            referenced.SetParent(this);
            this.AddSpan(referenced.GetSpan());
            this.referenced = referenced;
        }


        public void SetMutability(bool mutable)
        {
            this.mutable = mutable;
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.referenced;
        }
    }
}
