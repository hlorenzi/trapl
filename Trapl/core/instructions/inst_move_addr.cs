using System;


namespace Trapl.Core
{
    public class InstructionMoveAddr : InstructionMove
    {
        public DataAccess source;
        public bool mutable;


        public static InstructionMoveAddr Of(Diagnostics.Span span, DataAccess destination, DataAccess source, bool mutable)
        {
            return new InstructionMoveAddr { span = span, destination = destination, source = source, mutable = mutable };
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
            if (this.mutable)
                Console.Write("mut ");
            Console.Write("(");
            Console.ResetColor();
            Console.Write(this.source.GetString());
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(")");
            Console.ResetColor();
        }
    }
}
