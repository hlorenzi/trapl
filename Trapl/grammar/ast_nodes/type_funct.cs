using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeTypeFunct : ASTNodeType
    {
        public ASTNodeType returnType;
        public List<ASTNodeType> parameters = new List<ASTNodeType>();


        public void SetReturnType(ASTNodeType returnType)
        {
            returnType.SetParent(this);
            this.returnType = returnType;
        }


        public void AddParameterType(ASTNodeType parameterType)
        {
            parameterType.SetParent(this);
            this.parameters.Add(parameterType);
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.returnType;
            foreach (var parameter in this.parameters)
                yield return parameter;
        }
    }
}
