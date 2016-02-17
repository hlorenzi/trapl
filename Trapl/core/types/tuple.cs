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


        public override bool IsSame(Type other)
        {
            var otherTuple = other as Core.TypeTuple;
            if (otherTuple == null)
                return false;

            if (this.elementTypes.Length != otherTuple.elementTypes.Length)
                return false;

            for (var i = 0; i < this.elementTypes.Length; i++)
            {
                if (!this.elementTypes[i].IsSame(otherTuple.elementTypes[i]))
                    return false;
            }

            return true;
        }


        public override bool IsResolved()
        {
            for (var i = 0; i < this.elementTypes.Length; i++)
            {
                if (!this.elementTypes[i].IsResolved())
                    return false;
            }

            return true;
        }


        public override bool IsError()
        {
            for (var i = 0; i < this.elementTypes.Length; i++)
            {
                if (this.elementTypes[i].IsError())
                    return true;
            }

            return false;
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
