using System.Collections.Generic;


namespace Trapl.Semantics
{
    public class Definition<T>
    {
        public string fullName;

        public List<T> defs = new List<T>();
        public T mainDef = default(T);
    }
}
