using System;
using System.Collections.Generic;


namespace Trapl.Core
{
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


    public class DataAccessDereference : DataAccess
    {
        public DataAccess innerAccess;


        public static DataAccessDereference Of(Diagnostics.Span span, DataAccess innerAccess)
        {
            return new DataAccessDereference { span = span, innerAccess = innerAccess };
        }


        public override string GetString()
        {
            return "@(" + innerAccess.GetString() + ")";
        }
    }


    public class DataAccessField : DataAccess
    {
        public DataAccess baseAccess;
        public int fieldIndex;


        public static DataAccessField Of(Diagnostics.Span span, DataAccess baseAccess, int fieldIndex)
        {
            return new DataAccessField { span = span, baseAccess = baseAccess, fieldIndex = fieldIndex };
        }


        public override string GetString()
        {
            return baseAccess.GetString() + "." + fieldIndex;
        }
    }
}
