using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeExprIf : ASTNodeExpr
    {
        public ASTNodeExpr conditionExpr;
        public ASTNodeExpr trueBranchExpr;
        public ASTNodeExpr falseBranchExpr;


        public void SetConditionNode(ASTNodeExpr expr)
        {
            expr.SetParent(this);
            this.AddSpan(expr.GetSpan());
            this.conditionExpr = expr;
        }


        public void SetTrueBranchNode(ASTNodeExpr expr)
        {
            expr.SetParent(this);
            this.AddSpan(expr.GetSpan());
            this.trueBranchExpr = expr;
        }


        public void SetFalseBranchNode(ASTNodeExpr expr)
        {
            expr.SetParent(this);
            this.AddSpan(expr.GetSpan());
            this.falseBranchExpr = expr;
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.conditionExpr;
            yield return this.trueBranchExpr;

            if (this.falseBranchExpr != null)
                yield return this.falseBranchExpr;
        }
    }
}
