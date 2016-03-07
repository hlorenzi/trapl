using System;


namespace Trapl.Core
{
    public class InstructionMoveLiteralFunct : InstructionMove
    {
        public int functIndex;


        public static InstructionMoveLiteralFunct With(Diagnostics.Span span, DataAccess destination, int functIndex)
        {
            return new InstructionMoveLiteralFunct { span = span, destination = destination, functIndex = functIndex };
        }


        public override void PrintToConsole(string indentation = "")
        {
            Console.Write(indentation);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("move ");
            Console.ResetColor();
            Console.Write(this.destination.GetString());
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" <- ");
            Console.ResetColor();
            Console.WriteLine("fn[" + this.functIndex + "]");
            Console.ResetColor();
        }
    }
}
