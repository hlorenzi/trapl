using System;


namespace Trapl.Diagnostics
{
    public struct Span
    {
        public Interface.SourceCode src;
        public int start, end;


        public Span(Interface.SourceCode src)
        {
            this.src = src;
            this.start = 0;
            this.end = 0;
        }


        public Span(Interface.SourceCode src, int start, int end)
        {
            this.src = src;
            this.start = start;
            this.end = end;
        }


        public Span Merge(Span other)
        {
            if (this.src != other.src)
                throw new InvalidOperationException("trying to merge spans from different sources");

            return new Span(
                this.src,
                Math.Min(this.start, other.start),
                Math.Max(this.end, other.end));
        }


        public Span JustBefore()
        {
            return new Span(this.src, this.start, this.start);
        }


        public Span JustAfter()
        {
            return new Span(this.src, this.end, this.end);
        }


        public int Length()
        {
            return this.end - this.start;
        }


        public string GetExcerpt()
        {
            if (this.src == null)
                return "<<unknown source>>";
            else
                return this.src.GetExcerpt(this.start, this.end);
        }


        public override string ToString()
        {
            return GetExcerpt();
        }
    }
}
