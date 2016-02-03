using System.Collections.Generic;


namespace Trapl.Core
{
    public class TypeTuple : Type
    {
        public Type[] elementTypes = new Type[0];


        public static TypeTuple Empty()
        {
            return new TypeTuple { elementTypes = new Type[0] };
        }


        public static TypeTuple Of(params Type[] elementTypes)
        {
            return new TypeTuple { elementTypes = elementTypes };
        }


        public override string GetString(Core.Session session)
        {
            var result = "(";
            for (var i = 0; i < this.elementTypes.Length; i++)
            {
                result += this.elementTypes[i].GetString(session);
                if (i < this.elementTypes.Length - 1)
                    result += ", ";
            }
            return result + ")";
        }
    }
}
