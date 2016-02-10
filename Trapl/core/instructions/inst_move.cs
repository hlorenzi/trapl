using System;
using System.Collections.Generic;


namespace Trapl.Core
{
    public abstract class InstructionMove : Instruction
    {
        public DataAccess destination;
    }


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


    public class InstructionMoveLiteralInt : InstructionMove
    {
        public long value;


        public static InstructionMoveLiteralInt Of(Diagnostics.Span span, DataAccess destination, long value)
        {
            return new InstructionMoveLiteralInt { span = span, destination = destination, value = value };
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


    public class InstructionMoveLiteralBool : InstructionMove
    {
        public bool value;
    }


    public class InstructionMoveLiteralTuple : InstructionMove
    {
        public List<DataAccess> sourceElements = new List<DataAccess>();


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
            Console.Write(" <- ");
            Console.ResetColor();
            Console.WriteLine("()");
            Console.ResetColor();
        }
    }


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
