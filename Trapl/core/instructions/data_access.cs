using System;
using System.Collections.Generic;


namespace Trapl.Core
{
    public class FieldAccesses
    {
        public List<int> indices = new List<int>();


        public string GetString()
        {
            var result = "";
            for (var i = 0; i < this.indices.Count; i++)
                result += "." + this.indices[i];

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
        public bool dereference;


        public static DataAccessRegister ForRegister(Diagnostics.Span span, int registerIndex)
        {
            return new DataAccessRegister { span = span, registerIndex = registerIndex };
        }


        public void AddFieldAccess(int index)
        {
            this.fieldAccesses.indices.Add(index);
        }


        public override string GetString()
        {
            return (dereference ? "@" : "") + "#r" + registerIndex + fieldAccesses.GetString();
        }
    }
}
