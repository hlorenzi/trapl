using System;


namespace Trapl.Core
{
    public class InstructionMoveData : InstructionMove
    {
        public DataAccess source;


        public static InstructionMoveData Of(Diagnostics.Span span, DataAccess destination, DataAccess source)
        {
            return new InstructionMoveData { span = span, destination = destination, source = source };
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
            Console.WriteLine(this.source.GetString());
            Console.ResetColor();
        }
    }
}
