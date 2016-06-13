namespace Trapl.Core
{
    public class TypeFunct : Type
    {
        public Type returnType;
        public Type[] parameterTypes = new Type[0];


        public static TypeFunct Of(Type returnType, params Type[] parameterTypes)
        {
            return new TypeFunct { returnType = returnType, parameterTypes = parameterTypes };
        }


        public override bool IsSame(Type other)
        {
            var otherFunct = other as TypeFunct;
            if (otherFunct == null)
                return false;

            if (!this.returnType.IsSame(otherFunct.returnType))
                return false;

            if (this.parameterTypes.Length != otherFunct.parameterTypes.Length)
                return false;

            for (var i = 0; i < this.parameterTypes.Length; i++)
            {
                if (!this.parameterTypes[i].IsSame(otherFunct.parameterTypes[i]))
                    return false;
            }

            return true;
        }


        public override bool IsResolved()
        {
            if (!this.returnType.IsResolved())
                return false;

            for (var i = 0; i < this.parameterTypes.Length; i++)
            {
                if (!this.parameterTypes[i].IsResolved())
                    return false;
            }

            return true;
        }


        public override bool IsError()
        {
            if (this.returnType.IsError())
                return true;

            for (var i = 0; i < this.parameterTypes.Length; i++)
            {
                if (this.parameterTypes[i].IsError())
                    return true;
            }

            return false;
        }


        public override bool IsZeroSized(Core.Session session)
        {
            return false;
        }


        public override string GetString(Core.Session session)
        {
            var result = "fn(";
            for (var i = 0; i < this.parameterTypes.Length; i++)
            {
                result += this.parameterTypes[i].GetString(session);
                if (i < this.parameterTypes.Length - 1)
                    result += ", ";
            }
            return result + ") -> " + this.returnType.GetString(session);
        }
    }
}
