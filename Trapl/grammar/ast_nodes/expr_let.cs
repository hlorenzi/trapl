using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeExprLet : ASTNodeExpr
    {
        public ASTNodeExpr declExpr;
        public ASTNodeType type;
        public ASTNodeExpr initExpr;


        public void SetDeclarationNode(ASTNodeExpr expr)
        {
            expr.SetParent(this);
            this.AddSpan(expr.GetSpan());
            this.declExpr = expr;
        }


        public void SetTypeNode(ASTNodeType type)
        {
            type.SetParent(this);
            this.AddSpan(type.GetSpan());
            this.type = type;
        }


        public void SetInitializerNode(ASTNodeExpr expr)
        {
            expr.SetParent(this);
            this.AddSpan(expr.GetSpan());
            this.initExpr = expr;
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.declExpr;

            if (this.type != null)
                yield return this.type;

            if (this.initExpr != null)
                yield return this.initExpr;
        }
    }
}
