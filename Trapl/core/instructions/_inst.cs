using System;


namespace Trapl.Core
{
    public abstract class Instruction
    {
        public Diagnostics.Span span;


        public virtual void PrintToConsole(string indentation = "")
        {
            Console.WriteLine(indentation);
        }
    }


    public abstract class DataAccess
    {
        public Diagnostics.Span span;


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


        public static DataAccessRegister ForRegister(Diagnostics.Span span, int registerIndex)
        {
            return new DataAccessRegister { span = span, registerIndex = registerIndex };
        }


        public override string GetString()
        {
            return "#r" + registerIndex;
        }
    }
}
