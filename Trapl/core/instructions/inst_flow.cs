using System;


namespace Trapl.Core
{
    public class InstructionEnd : Instruction
    {
        public override void PrintToConsole(string indentation = "")
        {
            Console.Write(indentation);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("end");
            Console.ResetColor();
        }
    }


    public class InstructionGoto : Instruction
    {
        public int destinationSegment;


        public static InstructionGoto To(int destinationSegment)
        {
            return new InstructionGoto { destinationSegment = destinationSegment };
        }


        public override void PrintToConsole(string indentation = "")
        {
            Console.Write(indentation);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("goto ");
            Console.ResetColor();
            Console.WriteLine("#s" + this.destinationSegment.ToString());
            Console.ResetColor();
        }
    }


    public class InstructionBranch : Instruction
    {
        public DataAccess conditionReg;
        public int destinationSegmentIfTaken;
        public int destinationSegmentIfNotTaken;


        public override void PrintToConsole(string indentation = "")
        {
            Console.Write(indentation);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("branch ");
            Console.ResetColor();
            Console.Write(this.conditionReg.GetString());
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" ? ");
            Console.ResetColor();
            Console.Write("#s" + this.destinationSegmentIfTaken.ToString());
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" : ");
            Console.ResetColor();
            Console.WriteLine("#s" + this.destinationSegmentIfNotTaken.ToString());
            Console.ResetColor();
        }
    }
}
