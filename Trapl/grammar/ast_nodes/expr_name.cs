using System.Collections.Generic;


namespace Trapl.Grammar
{
    public abstract class ASTNodeExprName : ASTNodeExpr
    {

    }


    public class ASTNodeExprNameConcrete : ASTNodeExprName
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


    public class ASTNodeExprNamePlaceholder : ASTNodeExprName
    {

    }
}
