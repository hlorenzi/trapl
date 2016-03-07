using System;


namespace Trapl.Core
{
    public class InstructionMoveLiteralBool : InstructionMove
    {
        public bool value;


        public static InstructionMoveLiteralBool Of(Diagnostics.Span span, DataAccess destination, bool value)
        {
            return new InstructionMoveLiteralBool { span = span, destination = destination, value = value };
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
            Console.WriteLine(this.value ? "true" : "false");
            Console.ResetColor();
        }
    }
}
