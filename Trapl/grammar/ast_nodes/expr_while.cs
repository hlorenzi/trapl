using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeExprWhile : ASTNodeExpr
    {
        public ASTNodeExpr conditionExpr;
        public ASTNodeExpr bodyExpr;


        public void SetConditionNode(ASTNodeExpr expr)
        {
            expr.SetParent(this);
            this.AddSpan(expr.GetSpan());
            this.conditionExpr = expr;
        }


        public void SetBodyNode(ASTNodeExpr expr)
        {
            expr.SetParent(this);
            this.AddSpan(expr.GetSpan());
            this.bodyExpr = expr;
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.conditionExpr;
            yield return this.bodyExpr;
        }
    }
}
