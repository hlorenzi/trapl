using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeExprCall : ASTNodeExpr
    {
        public ASTNodeExpr calledExpr;
        public List<ASTNodeExpr> argumentExprs = new List<ASTNodeExpr>();


        public void SetCalledNode(ASTNodeExpr expr)
        {
            expr.SetParent(this);
            this.AddSpan(expr.GetSpan());
            this.calledExpr = expr;
        }


        public void AddArgumentNode(ASTNodeExpr expr)
        {
            expr.SetParent(this);
            this.AddSpan(expr.GetSpan());
            this.argumentExprs.Add(expr);
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.calledExpr;

            foreach (var arg in this.argumentExprs)
                yield return arg;
        }
    }
}
