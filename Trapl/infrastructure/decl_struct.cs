using System.Collections.Generic;


namespace Trapl.Infrastructure
{
    public class DeclStruct
    {
        public List<string> lifetimes = new List<string>();
        public NameTree<int> fieldNames = new NameTree<int>();
        public List<Type> fieldTypes = new List<Type>();
    }
}
