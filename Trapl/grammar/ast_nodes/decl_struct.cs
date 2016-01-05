using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeDeclStruct : ASTNode
    {
        public ASTNodeName name;
        public List<ASTNodeDeclStructField> fields = new List<ASTNodeDeclStructField>();


        public void SetNameNode(ASTNodeName name)
        {
            name.SetParent(this);
            this.name = name;
        }


        public void AddFieldNode(ASTNodeDeclStructField field)
        {
            field.SetParent(this);
            this.fields.Add(field);
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.name;
            foreach (var field in this.fields)
                yield return field;
        }
    }



    public class ASTNodeDeclStructField : ASTNode
    {
        public ASTNodeName name;
        public ASTNodeType type;


        public void SetNameNode(ASTNodeName name)
        {
            name.SetParent(this);
            this.name = name;
        }


        public void SetTypeNode(ASTNodeType type)
        {
            type.SetParent(this);
            this.type = type;
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            yield return this.name;
            yield return this.type;
        }
    }
}
