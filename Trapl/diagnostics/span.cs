using System;


namespace Trapl.Diagnostics
{
    public struct Span
    {
        public int start, end;


        public Span(int start, int end)
        {
            this.start = start;
            this.end = end;
        }


        public Span Merge(Span other)
        {
            return new Span(
                Math.Min(this.start, other.start),
                Math.Max(this.end, other.end));
        }


        public Span JustBefore()
        {
            return new Span(this.start, this.start);
        }


        public Span JustAfter()
        {
            return new Span(this.end, this.end);
        }


        public int Length()
        {
            return this.end - this.start;
        }
    }
}
