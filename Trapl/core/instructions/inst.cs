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
}
