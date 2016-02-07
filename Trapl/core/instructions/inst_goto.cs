namespace Trapl.Core
{
    public class InstructionGoto : Instruction
    {
        public int destinationSegment;


        public static InstructionGoto To(int destinationSegment)
        {
            return new InstructionGoto { destinationSegment = destinationSegment };
        }


        public override string GetString()
        {
            return "goto #s" + destinationSegment;
        }
    }
}
