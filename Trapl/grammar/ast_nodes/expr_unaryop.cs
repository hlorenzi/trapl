using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeExprUnaryOp : ASTNodeExpr
    {
        public enum Operator
        {
            Minus, Exclamation, Ampersand, Asterisk, AsteriskMut, At
        }


        public Operator oper;
        public ASTNodeExpr operand;


        public void SetOperator(Operator oper)
        {
            this.oper = oper;
        }


        public void SetOperandNode(ASTNodeExpr expr)
        {
            expr.SetParent(this);
            this.AddSpan(expr.GetSpan());
            this.operand = expr;
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.operand;
        }


        public override string GetName()
        {
            string op = null;
            switch (this.oper)
            {
                case Operator.Minus: { op = "-"; break; }
                case Operator.Exclamation: { op = "!"; break; }
                case Operator.Ampersand: { op = "&"; break; }
                case Operator.Asterisk: { op = "*"; break; }
                case Operator.AsteriskMut: { op = "*mut "; break; }
                case Operator.At: { op = "@"; break; }
            }

            return base.GetName() + " (" + op + ")";
        }
    }
}
