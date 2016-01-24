using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeExprReturn : ASTNodeExpr
    {
        public ASTNodeExpr expr;


        public void SetExpressionNode(ASTNodeExpr expr)
        {
            expr.SetParent(this);
            this.AddSpan(expr.GetSpan());
            this.expr = expr;
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.expr;
        }
    }
}
