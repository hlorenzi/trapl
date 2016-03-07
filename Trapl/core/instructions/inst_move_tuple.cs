using System;


namespace Trapl.Core
{
    public class InstructionMoveLiteralTuple : InstructionMove
    {
        public DataAccess[] sourceElements = new DataAccess[0];


        public static InstructionMoveLiteralTuple Empty(Diagnostics.Span span, DataAccess destination)
        {
            return new InstructionMoveLiteralTuple { span = span, destination = destination };
        }


        public override void PrintToConsole(string indentation = "")
        {
            Console.Write(indentation);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("move ");
            Console.ResetColor();
            Console.Write(this.destination.GetString());
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" <- (");
            Console.ResetColor();

            for (var i = 0; i < this.sourceElements.Length; i++)
            {
                Console.Write(this.sourceElements[i].GetString());
                if (i < this.sourceElements.Length - 1)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(", ");
                    Console.ResetColor();
                }
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(")");
            Console.ResetColor();
        }
    }
}
