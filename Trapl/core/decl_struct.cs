using System.Collections.Generic;


namespace Trapl.Core
{
    public class DeclStruct
    {
        public List<string> lifetimes = new List<string>();
        public NameTree<int> fieldNames = new NameTree<int>();
        public List<Type> fieldTypes = new List<Type>();


        public int AddField(Name name, Type fieldType)
        {
            var fieldIndex = this.fieldTypes.Count;
            this.fieldTypes.Add(fieldType);
            this.fieldNames.Add(name, fieldIndex);
            return fieldIndex;
        }
    }
}
