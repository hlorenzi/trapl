namespace Trapl.Core
{
    public class InstructionBranch : Instruction
    {
        public DataAccess conditionReg;
        public int destinationSegmentIfTaken;
        public int destinationSegmentIfNotTaken;


        public override string GetString()
        {
            return "branch " + conditionReg.GetString() + " ? #s" +
                destinationSegmentIfTaken + " : #s" + destinationSegmentIfNotTaken;
        }
    }
}
