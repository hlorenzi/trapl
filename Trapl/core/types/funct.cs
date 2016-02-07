namespace Trapl.Core
{
    public class TypeFunct : Type
    {
        public Type returnType;
        public Type[] parameterTypes = new Type[0];


        public static TypeFunct Of(Type returnType, params Type[] parameterTypes)
        {
            return new TypeFunct { parameterTypes = parameterTypes };
        }


        public override string GetString(Core.Session session)
        {
            var result = "(";
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
