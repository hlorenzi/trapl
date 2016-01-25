using System.Collections.Generic;


namespace Trapl.Core
{
    public class TypeTuple : Type
    {
        public static TypeTuple Empty()
        {
            return new TypeTuple();
        }


        public override string GetString(Core.Session session)
        {
            return "()";
        }
    }
}
