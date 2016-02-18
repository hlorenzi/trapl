using System;
using System.Collections.Generic;


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


    public class FieldAccesses
    {
        public List<int> indices = new List<int>();
        public List<string> names = new List<string>();


        public string GetString()
        {
            var result = "";
            for (var i = 0; i < this.indices.Count; i++)
            {
                result += ".";
                if (this.names[i] == null)
                    result += this.indices[i];
                else
                    result += this.names[i];
            }

            return result;
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
        public FieldAccesses fieldAccesses = new FieldAccesses();


        public static DataAccessRegister ForRegister(Diagnostics.Span span, int registerIndex)
        {
            return new DataAccessRegister { span = span, registerIndex = registerIndex };
        }


        public void AddFieldAccessByName(string name)
        {
            this.fieldAccesses.indices.Add(-1);
            this.fieldAccesses.names.Add(name);
        }


        public override string GetString()
        {
            return "#r" + registerIndex + fieldAccesses.GetString();
        }
    }
}
