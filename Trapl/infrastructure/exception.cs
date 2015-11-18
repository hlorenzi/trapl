using System;


namespace Trapl.Infrastructure
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
