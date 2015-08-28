using System;
using System.Collections.Generic;


namespace Trapl.Syntax
{
    public class Output
    {
        public List<Node> topDecls;


        public Output()
        {
            this.topDecls = new List<Node>();
        }


        public void PrintDebug(Source src)
        {
            foreach (var node in this.topDecls)
                PrintDebugRecursive(src, node, 0);
        }


        private void PrintDebugRecursive(Source src, Node node, int indentLevel)
        {
            string firstColumn =
                new string(' ', indentLevel * 2) +
                System.Enum.GetName(typeof(NodeKind), node.kind);

            string excerpt = src.Excerpt(node.Span());
            string secondColumn =
                new string(' ', indentLevel * 2) +
                excerpt.Substring(0, Math.Min(excerpt.Length, 20)).Replace("\n", " ").Replace("\r", "") +
                (excerpt.Length > 20 ? "..." : "");

            Console.Out.Write(firstColumn.PadRight(40));
            Console.Out.Write(": ");
            Console.Out.WriteLine(secondColumn);
            foreach (var child in node.EnumerateChildren())
                PrintDebugRecursive(src, child, indentLevel + 1);
        }
    }


    public enum NodeKind
    {
        TopLevelDecl,
        FunctDecl, FunctArgDecl, FunctReturnDecl,
        StructDecl, StructMemberDecl,
        TraitDecl, TraitMemberDecl,
        Identifier, NumberLiteral, TypeName,
        Block,
        BinaryOp, Operator, Call,
        ControlLet, ControlIf, ControlWhile, ControlReturn,
    }


    public class Node
    {
        public NodeKind kind;
        private Diagnostics.Span span;
        private Diagnostics.Span spanWithDelimiters;
        private List<Node> children = new List<Node>();


        public Node(NodeKind kind)
        {
            this.kind = kind;
        }


        public Node(NodeKind kind, Diagnostics.Span span)
        {
            this.kind = kind;
            this.span = span;
            this.spanWithDelimiters = span;
        }


        public Diagnostics.Span Span()
        {
            return this.span;
        }


        public Diagnostics.Span SpanWithDelimiters()
        {
            return this.spanWithDelimiters;
        }


        public void SetSpan(Diagnostics.Span span)
        {
            this.span = span;
            this.spanWithDelimiters = span;
        }


        public void AddSpan(Diagnostics.Span span)
        {
            this.span = this.span.Merge(span);
            this.spanWithDelimiters = this.spanWithDelimiters.Merge(span);
        }


        public void AddSpanWithDelimiters(Diagnostics.Span span)
        {
            this.spanWithDelimiters = this.spanWithDelimiters.Merge(span);
        }


        public void AddChild(Node node)
        {
            this.children.Add(node);
        }


        public Node Child(int index)
        {
            return this.children[index];
        }


        public void SetLastChildSpan()
        {
            if (this.children.Count > 0)
                this.SetSpan(this.children[this.children.Count - 1].SpanWithDelimiters());
        }


        public void AddLastChildSpan()
        {
            if (this.children.Count > 0)
                this.AddSpan(this.children[this.children.Count - 1].SpanWithDelimiters());
        }


        public virtual IEnumerable<Node> EnumerateChildren()
        {
            foreach (var child in this.children)
                yield return child;
        }
    }
}
