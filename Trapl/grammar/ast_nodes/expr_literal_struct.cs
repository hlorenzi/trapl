using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeExprLiteralStructField : ASTNodeExpr
    {
        public ASTNodeName name;
        public ASTNodeExpr expr;


        public void SetNameNode(ASTNodeName name)
        {
            this.name = name;
            this.AddSpan(name.GetSpan());
        }


        public void SetExprNode(ASTNodeExpr expr)
        {
            this.expr = expr;
            this.AddSpan(expr.GetSpan());
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.name;
            yield return this.expr;
        }
    }


    public class ASTNodeExprLiteralStruct : ASTNodeExpr
    {
        public ASTNodeExprName name;
        public List<ASTNodeExprLiteralStructField> fields = new List<ASTNodeExprLiteralStructField>();


        public void SetNameNode(ASTNodeExprName name)
        {
            this.name = name;
            this.AddSpan(name.GetSpan());
        }


        public void AddFieldExpr(ASTNodeExprLiteralStructField field)
        {
            this.fields.Add(field);
            this.AddSpan(field.GetSpan());
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.name;
            foreach (var field in this.fields)
                yield return field;
        }
    }
}
