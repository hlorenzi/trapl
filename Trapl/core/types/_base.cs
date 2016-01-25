namespace Trapl.Core
{
    public abstract class Type
    {
        public abstract string GetString(Core.Session session);
    }


    public class TypeError : Type
    {
        public override string GetString(Core.Session session)
        {
            return "<error>";
        }
    }
}
