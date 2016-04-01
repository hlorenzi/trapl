using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeExprBinaryOp : ASTNodeExpr
    {
        public enum Operator
        {
            Equal,
            Dot,
            Plus, Minus, Asterisk, Slash,
            Ampersand, VerticalBar, Circumflex
        }


        public Operator oper;
        public ASTNodeExpr lhsOperand, rhsOperand;


        public void SetOperator(Operator oper)
        {
            this.oper = oper;
        }


        public void SetLeftOperandNode(ASTNodeExpr expr)
        {
            expr.SetParent(this);
            this.AddSpan(expr.GetSpan());
            this.lhsOperand = expr;
        }


        public void SetRightOperandNode(ASTNodeExpr expr)
        {
            expr.SetParent(this);
            this.AddSpan(expr.GetSpan());
            this.rhsOperand = expr;
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.lhsOperand;
            yield return this.rhsOperand;
        }


        public override string GetName()
        {
            string op = null;
            switch (this.oper)
            {
                case Operator.Equal: { op = "="; break; }
                case Operator.Dot: { op = "."; break; }
                case Operator.Plus: { op = "+"; break; }
                case Operator.Minus: { op = "-"; break; }
                case Operator.Asterisk: { op = "*"; break; }
                case Operator.Slash: { op = "/"; break; }
                case Operator.Ampersand: { op = "&"; break; }
                case Operator.VerticalBar: { op = "|"; break; }
                case Operator.Circumflex: { op = "^"; break; }
            }

            return base.GetName() + " (" + op + ")";
        }
    }
}
