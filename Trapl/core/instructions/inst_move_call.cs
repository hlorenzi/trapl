using System;


namespace Trapl.Core
{
    public class InstructionMoveCallResult : InstructionMove
    {
        public DataAccess callTargetSource;
        public DataAccess[] argumentSources;


        public static InstructionMoveCallResult For(
            Diagnostics.Span span,
            DataAccess destination,
            DataAccess callTarget,
            DataAccess[] arguments)
        {
            return new InstructionMoveCallResult
            {
                span = span,
                destination = destination,
                callTargetSource = callTarget,
                argumentSources = arguments
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
            Console.Write(" <- call ");
            Console.ResetColor();
            Console.Write(this.callTargetSource.GetString());
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" (");

            for (var i = 0; i < this.argumentSources.Length; i++)
            {
                Console.ResetColor();
                Console.Write(this.argumentSources[i].GetString());
                Console.ForegroundColor = ConsoleColor.DarkGray;
                if (i < this.argumentSources.Length - 1)
                    Console.Write(", ");
            }

            Console.WriteLine(")");
            Console.ResetColor();
        }
    }
}
