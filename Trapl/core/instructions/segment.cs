using System.Collections.Generic;


namespace Trapl.Core
{
    public class InstructionSegment
    {
        public Diagnostics.Span span;
        public List<Instruction> instructions = new List<Instruction>();
        public SegmentFlow outFlow;


        public void SetFlow(SegmentFlow flow)
        {
            this.outFlow = flow;
        }


        public Diagnostics.Span GetSpan()
        {
            var fullSpan = this.span;

            foreach (var inst in instructions)
                fullSpan = fullSpan.Merge(inst.span);

            return fullSpan;
        }
    }
}
