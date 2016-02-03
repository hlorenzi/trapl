using System.Collections.Generic;


namespace Trapl.Core
{
    public class DeclFunct
    {
        public List<Name> localNames = new List<Name>();
        public List<Type> localTypes = new List<Type>();
        public int parameterNum;
        public Type returnType;
    }
}
