using System;


namespace Trapl.Core
{
    public class InstructionMoveLiteralStruct : InstructionMove
    {
        public DataAccess[] fieldSources = new DataAccess[0];
        public Diagnostics.Span[] fieldDestSpans = new Diagnostics.Span[0];
        public int structIndex;


        public static InstructionMoveLiteralStruct Of(
            Diagnostics.Span span, DataAccess destination, int structIndex,
            DataAccess[] fieldSources, Diagnostics.Span[] fieldDestSpans)
        {
            return new InstructionMoveLiteralStruct
            {
                span = span,
                destination = destination,
                structIndex = structIndex,
                fieldSources = fieldSources,
                fieldDestSpans = fieldDestSpans
            };
        }


        public override void PrintToConsole(string indentation = "")
        {
            Console.Write(indentation);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("move ");
            Console.ResetColor();
            Console.Write(this.destination.GetString());
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" <- struct[");
            Console.ResetColor();
            Console.Write(this.structIndex);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("] {");
            Console.ResetColor();

            for (var i = 0; i < this.fieldSources.Length; i++)
            {
                Console.Write(this.fieldSources[i].GetString());
                if (i < this.fieldSources.Length - 1)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(", ");
                    Console.ResetColor();
                }
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("}");
            Console.ResetColor();
        }
    }
}
