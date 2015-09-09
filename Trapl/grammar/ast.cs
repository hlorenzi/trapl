using System;
using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class AST
    {
        public List<ASTNode> topDecls;


        public AST()
        {
            this.topDecls = new List<ASTNode>();
        }


        public void PrintDebug(Interface.SourceCode src)
        {
            foreach (var node in this.topDecls)
                PrintDebugRecursive(src, node, 0);
        }


        private void PrintDebugRecursive(Interface.SourceCode src, ASTNode node, int indentLevel)
        {
            string firstColumn =
                new string(' ', indentLevel * 2) +
                System.Enum.GetName(typeof(ASTNodeKind), node.kind);

            string excerpt = src.GetExcerpt(node.Span());
            string secondColumn =
                new string(' ', indentLevel * 2) +
                excerpt.Substring(0, Math.Min(excerpt.Length, 20)).Replace("\n", " ").Replace("\r", "").Replace("\t", " ") +
                (excerpt.Length > 20 ? "..." : "");

            Console.Out.Write(firstColumn.PadRight(40));
            Console.Out.Write(": ");
            Console.Out.WriteLine(secondColumn);
            foreach (var child in node.EnumerateChildren())
                PrintDebugRecursive(src, child, indentLevel + 1);
        }
    }


    public enum ASTNodeKind
    {
        TopLevelDecl,
        FunctDecl, FunctArgDecl, FunctReturnDecl,
        StructDecl, StructMemberDecl,
        TraitDecl, TraitMemberDecl,
        Name, NumberLiteral, TypeName,
        Identifier, TemplateList, TemplateType,
        Block,
        BinaryOp, UnaryOp, Operator, Call,
        ControlLet, ControlIf, ControlWhile, ControlReturn,
    }


    public class ASTNode
    {
        public ASTNodeKind kind;
        private Diagnostics.Span span;
        private Diagnostics.Span spanWithDelimiters;
        private List<ASTNode> children = new List<ASTNode>();


        public ASTNode(ASTNodeKind kind)
        {
            this.kind = kind;
        }


        public ASTNode(ASTNodeKind kind, Diagnostics.Span span)
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


        public void AddChild(ASTNode node)
        {
            this.children.Add(node);
        }


        public ASTNode Child(int index)
        {
            return this.children[index];
        }


        public ASTNode ChildWithKind(ASTNodeKind kind)
        {
            return this.children.Find(c => c.kind == kind);
        }


        public bool ChildIs(int index, ASTNodeKind kind)
        {
            if (index < 0 || index >= this.children.Count)
                return false;
            return (this.children[index].kind == kind);
        }


        public int ChildNumber()
        {
            return this.children.Count;
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


        public virtual IEnumerable<ASTNode> EnumerateChildren()
        {
            foreach (var child in this.children)
                yield return child;
        }
    }
}
