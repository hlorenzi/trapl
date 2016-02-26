using System;


namespace Trapl.Core
{
    public abstract class SegmentFlow
    {
        public Diagnostics.Span span;


        public virtual void PrintToConsole(string indentation = "")
        {
            Console.WriteLine(indentation);
        }
    }


    public class SegmentFlowEnd : SegmentFlow
    {
        public override void PrintToConsole(string indentation = "")
        {
            Console.Write(indentation);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("end");
            Console.ResetColor();
        }
    }


    public class SegmentFlowReturn : SegmentFlow
    {
        public DataAccess returnedData;


        public static SegmentFlowReturn Returning(Diagnostics.Span span, DataAccess returnedData)
        {
            return new SegmentFlowReturn { span = span, returnedData = returnedData };
        }


        public override void PrintToConsole(string indentation = "")
        {
            Console.Write(indentation);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("ret ");
            Console.ResetColor();
            Console.WriteLine(this.returnedData.GetString());
        }
    }


    public class SegmentFlowGoto : SegmentFlow
    {
        public int destinationSegment;


        public static SegmentFlowGoto To(int destinationSegment)
        {
            return new SegmentFlowGoto { destinationSegment = destinationSegment };
        }


        public override void PrintToConsole(string indentation = "")
        {
            Console.Write(indentation);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("goto ");
            Console.ResetColor();
            Console.WriteLine("#s" + this.destinationSegment.ToString());
        }
    }


    public class SegmentFlowBranch : SegmentFlow
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
