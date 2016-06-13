using System.Collections.Generic;


namespace Trapl.Core
{
    public class DeclStruct
    {
        public bool primitive;

        public List<string> lifetimes = new List<string>();
        public NameTree<int> fieldNames = new NameTree<int>();
        public List<Type> fieldTypes = new List<Type>();
        public List<Grammar.ASTNodeDeclStructField> fieldASTNodes = new List<Grammar.ASTNodeDeclStructField>();

        public Grammar.ASTNodeDeclStruct declASTNode;


        public int AddField(Name name, Type fieldType, Grammar.ASTNodeDeclStructField astNode)
        {
            var fieldIndex = this.fieldTypes.Count;
            this.fieldTypes.Add(fieldType);
            this.fieldNames.Add(name, fieldIndex);
            this.fieldASTNodes.Add(astNode);
            return fieldIndex;
        }


        public Diagnostics.Span GetNameSpan()
        {
            if (declASTNode != null)
                return declASTNode.name.GetSpan();

            return new Diagnostics.Span();
        }


        public Diagnostics.Span GetFieldNameSpan(int fieldIndex)
        {
            if (fieldASTNodes[fieldIndex] != null)
                return fieldASTNodes[fieldIndex].name.GetSpan();

            return new Diagnostics.Span();
        }
    }
}
