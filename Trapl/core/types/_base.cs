namespace Trapl.Core
{
    public abstract class Type
    {
        public virtual bool IsSame(Core.Type other)
        {
            return false;
        }


        public virtual bool IsResolved()
        {
            return false;
        }


        public virtual bool IsError()
        {
            return false;
        }


        public virtual bool IsEmptyTuple()
        {
            return false;
        }


        public abstract string GetString(Core.Session session);
    }


    public class TypeError : Type
    {
        public override bool IsError()
        {
            return true;
        }


        public override string GetString(Core.Session session)
        {
            return "<error>";
        }
    }
}
