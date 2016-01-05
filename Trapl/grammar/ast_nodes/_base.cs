using System;
using System.Collections.Generic;


namespace Trapl.Grammar
{
    public abstract class ASTNode
    {
        private Diagnostics.Span span;
        public ASTNode parent;


        public void SetParent(ASTNode parent)
        {
            this.parent = parent;
        }


        public Diagnostics.Span GetSpan()
        {
            return this.span;
        }


        public void SetSpan(Diagnostics.Span span)
        {
            this.span = span;
        }


        public void AddSpan(Diagnostics.Span span)
        {
            this.span = this.span.Merge(span);
        }

        public string GetExcerpt()
        {
            return this.span.GetExcerpt();
        }


        public virtual IEnumerable<ASTNode> EnumerateChildren()
        {
            yield break;
        }


        public virtual string GetName()
        {
            return this.GetType().Name.Substring("ASTNode".Length);
        }


        public void PrintDebugRecursive(string indentation)
        {
            var header = indentation + this.GetName();
            var excerptMaxLength = 75 - header.Length;

            var excerpt = this.GetExcerpt();
            excerpt = excerpt.Replace("\n", " ").Replace("\r", "").Replace("\t", " ").Trim();
            if (excerpt.Length > excerptMaxLength)
                excerpt = excerpt.Substring(0, excerptMaxLength) + "...";

            Console.ResetColor();
            Console.Write(indentation);
            Console.Write(this.GetName());
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" ");
            Console.WriteLine(excerpt);
            Console.ResetColor();

            foreach (var child in this.EnumerateChildren())
                child.PrintDebugRecursive(indentation + "  ");
        }


        public override string ToString()
        {
            var result = "{";
            result +=
                this.GetName() + " " +
                "'" + this.GetExcerpt() + "'";

            foreach (var child in this.EnumerateChildren())
                result += child.ToString();

            return result + "}";
        }
    }
}
