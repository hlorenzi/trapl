using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeExprBlock : ASTNodeExpr
    {
        public List<ASTNodeExpr> subexprs = new List<ASTNodeExpr>();


        public void AddSubexpressionNode(ASTNodeExpr expr)
        {
            expr.SetParent(this);
            this.AddSpan(expr.GetSpan());
            this.subexprs.Add(expr);
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            foreach (var expr in this.subexprs)
                yield return expr;
        }
    }
}
