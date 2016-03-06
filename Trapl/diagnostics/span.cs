using System;


namespace Trapl.Diagnostics
{
    public struct Span
    {
        public Core.TextInput unit;
        public int start, end;


        public Span(Core.TextInput unit, int start, int end)
        {
            this.unit = unit;
            this.start = start;
            this.end = end;
        }


        public Span Merge(Span other)
        {
            if (this.unit == null)
                return other;

            if (other.unit == null)
                return this;

            if (this.unit != other.unit)
                throw new InvalidOperationException("attempted to merge spans from different units");

            return new Span(
                this.unit,
                Math.Min(this.start, other.start),
                Math.Max(this.end, other.end));
        }


        public Span Displace(int startDelta, int endDelta)
        {
            return new Span(
                this.unit,
                this.start + startDelta,
                this.end + endDelta);
        }


        public Span JustBefore()
        {
            return new Span(this.unit, this.start, this.start);
        }


        public Span JustAfter()
        {
            return new Span(this.unit, this.end, this.end);
        }


        public int Length()
        {
            return this.end - this.start;
        }


        public string GetExcerpt()
        {
            if (this.unit == null)
                return "<<unknown source>>";
            else
                return this.unit.GetExcerpt(this.start, this.end);
        }


        public override string ToString()
        {
            return GetExcerpt();
        }
    }
}
