using System.Collections.Generic;


namespace Trapl.Core
{
    public class InstructionSegment
    {
        public List<Instruction> instructions = new List<Instruction>();
        public SegmentFlow outFlow;


        public void SetFlow(SegmentFlow flow)
        {
            this.outFlow = flow;
        }
    }
}
