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

            string secondColumn =
                new string(' ', indentLevel * 2) +
                src.Excerpt(node.Span());

            Console.Out.Write(firstColumn.PadRight(20));
            Console.Out.Write(": ");
            Console.Out.WriteLine(secondColumn);
            foreach (var child in node.EnumerateChildren())
                PrintDebugRecursive(src, child, indentLevel + 1);
        }
    }


    public enum NodeKind
    {
        FunctDecl, FunctArgDecl,
        Identifier, TypeName
    }


    public class Node
    {
        public NodeKind kind;
        private Diagnostics.Span span;
        private List<Node> children = new List<Node>();


        public Node(NodeKind kind)
        {
            this.kind = kind;
        }


        public Node(NodeKind kind, Diagnostics.Span span)
        {
            this.kind = kind;
            this.span = span;
        }


        public Diagnostics.Span Span()
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


        public void AddChild(Node node)
        {
            this.children.Add(node);
        }


        public virtual IEnumerable<Node> EnumerateChildren()
        {
            foreach (var child in this.children)
                yield return child;
        }
    }
}
