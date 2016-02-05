using System.Collections.Generic;


namespace Trapl.Core
{
    public abstract class InstructionMove : Instruction
    {
        public DataAccess destination;
    }


    public class InstructionMoveLiteralInt : InstructionMove
    {
        public long value;


        public static InstructionMoveLiteralInt Of(Diagnostics.Span span, DataAccess destination, long value)
        {
            return new InstructionMoveLiteralInt { span = span, destination = destination, value = value };
        }


        public override string GetString()
        {
            return "move " + destination.GetString() + " <- " + value.ToString();
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


        public override string GetString()
        {
            return "()";
        }
    }
}
