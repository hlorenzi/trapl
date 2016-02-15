namespace Trapl.Core
{
    public class TypePlaceholder : Type
    {
        public override string GetString(Core.Session session)
        {
            return "_";
        }
    }
}
