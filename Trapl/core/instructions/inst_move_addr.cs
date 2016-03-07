using System;


namespace Trapl.Core
{
    public class InstructionMoveAddr : InstructionMove
    {
        public DataAccess source;


        public static InstructionMoveAddr Of(Diagnostics.Span span, DataAccess destination, DataAccess source)
        {
            return new InstructionMoveAddr { span = span, destination = destination, source = source };
        }


        public override void PrintToConsole(string indentation = "")
        {
            Console.Write(indentation);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("move ");
            Console.ResetColor();
            Console.Write(this.destination.GetString());
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" <- &");
            Console.ResetColor();
            Console.WriteLine(this.source.GetString());
            Console.ResetColor();
        }
    }
}
