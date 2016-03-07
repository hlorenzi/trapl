using System;


namespace Trapl.Core
{
    public class InstructionMoveLiteralInt : InstructionMove
    {
        public long value;
        public Core.Type type;


        public static InstructionMoveLiteralInt Of(Diagnostics.Span span, DataAccess destination, Core.Type type, long value)
        {
            return new InstructionMoveLiteralInt { span = span, destination = destination, type = type, value = value };
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
            Console.WriteLine(this.value.ToString());
            Console.ResetColor();
        }
    }
}
