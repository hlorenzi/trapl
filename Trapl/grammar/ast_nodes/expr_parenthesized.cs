using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeExprParenthesized : ASTNodeExpr
    {
        public ASTNodeExpr innerExpr;


        public void SetInnerExpressionNode(ASTNodeExpr expr)
        {
            expr.SetParent(this);
            this.AddSpan(expr.GetSpan());
            this.innerExpr = expr;
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.innerExpr;
        }
    }
}
