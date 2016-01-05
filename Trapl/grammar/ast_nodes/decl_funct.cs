using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeDeclFunct : ASTNode
    {
        public ASTNodeName name;
        public List<ASTNodeDeclFunctParam> parameters = new List<ASTNodeDeclFunctParam>();
        public ASTNodeType returnType;
        public ASTNodeExpr bodyExpression;


        public void SetNameNode(ASTNodeName name)
        {
            name.SetParent(this);
            this.AddSpan(name.GetSpan());
            this.name = name;
        }


        public void AddParameterNode(ASTNodeDeclFunctParam param)
        {
            param.SetParent(this);
            this.AddSpan(param.GetSpan());
            this.parameters.Add(param);
        }


        public void SetReturnTypeNode(ASTNodeType retType)
        {
            retType.SetParent(this);
            this.AddSpan(retType.GetSpan());
            this.returnType = retType;
        }


        public void SetBodyNode(ASTNodeExpr expr)
        {
            expr.SetParent(this);
            this.AddSpan(expr.GetSpan());
            this.bodyExpression = expr;
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.name;

            foreach (var param in this.parameters)
                yield return param;

            yield return this.returnType;

            if (this.bodyExpression != null)
                yield return this.bodyExpression;
        }
    }



    public class ASTNodeDeclFunctParam : ASTNode
    {
        public ASTNodeName name;
        public ASTNodeType type;


        public void SetNameNode(ASTNodeName name)
        {
            name.SetParent(this);
            this.AddSpan(name.GetSpan());
            this.name = name;
        }


        public void SetTypeNode(ASTNodeType type)
        {
            type.SetParent(this);
            this.AddSpan(type.GetSpan());
            this.type = type;
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.name;
            yield return this.type;
        }
    }
}
