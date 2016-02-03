namespace Trapl.Core
{
    public class TypePlaceholder : Type
    {
        public int index;


        public static TypePlaceholder Of(int index)
        {
            return new TypePlaceholder { index = index };
        }


        public override string GetString(Core.Session session)
        {
            return "_" + index;
        }
    }
}
