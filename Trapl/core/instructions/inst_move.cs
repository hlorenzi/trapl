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


        public override string GetString()
        {
            return "move " + destination.GetString() + " <- " + source.GetString();
        }
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
            return "move " + destination.GetString() + " <- ()";
        }
    }


    public class InstructionMoveLiteralFunct : InstructionMove
    {
        public int functIndex;


        public static InstructionMoveLiteralFunct With(Diagnostics.Span span, DataAccess destination, int functIndex)
        {
            return new InstructionMoveLiteralFunct { span = span, destination = destination, functIndex = functIndex };
        }


        public override string GetString()
        {
            return "move " + destination.GetString() + " <- fn[" + functIndex + "]";
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


        public override string GetString()
        {
            var result = "move " + destination.GetString() + " <- call " +
                callTargetSource.GetString() + " (";

            for (var i = 0; i < this.argumentSources.Length; i++)
            {
                result += this.argumentSources[i].GetString();
                if (i < this.argumentSources.Length - 1)
                    result += ", ";
            }

            return result + ")";
        }
    }
}
