using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeTypeStruct : ASTNodeType
    {
        public ASTNodeName name;


        public void SetNameNode(ASTNodeName name)
        {
            name.SetParent(this);
            this.AddSpan(name.GetSpan());
            this.name = name;
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.name;
        }
    }
}
