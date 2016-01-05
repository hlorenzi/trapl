namespace Trapl.Grammar
{
    public class ASTNodeExprLiteralBool : ASTNodeExpr
    {
        public bool value;


        public void SetValue(bool value)
        {
            this.value = value;
        }
    }


    public class ASTNodeExprLiteralInt : ASTNodeExpr
    {
        public int radix;
        public string value;
        public Integer.Type type;


        public void SetRadix(int radix)
        {
            this.radix = radix;
        }


        public void SetValue(string value)
        {
            this.value = value;
        }


        public void SetType(Integer.Type type)
        {
            this.type = type;
        }
    }
}
