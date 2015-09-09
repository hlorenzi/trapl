using System;


namespace Trapl.Semantics
{
    public class InternalException : Exception
    {
        public InternalException(string msg) : base(msg)
        {
            
        }
    }

    public class CheckException : Exception
    {

    }
}
