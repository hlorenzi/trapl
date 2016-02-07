namespace Trapl.Core
{
    public abstract class Instruction
    {
        public Diagnostics.Span span;


        public virtual string GetString()
        {
            return "";
        }
    }


    public abstract class DataAccess
    {
        public abstract string GetString();
    }


    public class DataAccessDiscard : DataAccess
    {
        public override string GetString()
        {
            return "_";
        }
    }


    public class DataAccessRegister : DataAccess
    {
        public int registerIndex;


        public static DataAccessRegister ForRegister(int registerIndex)
        {
            return new DataAccessRegister { registerIndex = registerIndex };
        }


        public override string GetString()
        {
            return "#r" + registerIndex;
        }
    }
}
