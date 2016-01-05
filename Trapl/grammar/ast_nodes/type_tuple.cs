using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeTypeTuple : ASTNodeType
    {
        public List<ASTNodeType> elements = new List<ASTNodeType>();


        public void AddElementNode(ASTNodeType element)
        {
            element.SetParent(this);
            this.elements.Add(element);
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            foreach (var element in this.elements)
                yield return element;
        }
    }
}
