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


        public static void PrintDebug(Interface.SourceCode src, ASTNode node, int indentLevel)
        {
            node.PrintDebugRecursive(src, indentLevel, indentLevel);
        }
    }


    public enum ASTNodeKind
    {
        TopLevelDecl,
        FunctDecl, FunctArgDecl, FunctReturnDecl,
        StructDecl, StructMemberDecl,
        TraitDecl, TraitMemberDecl,
        Identifier, Name, NumberLiteral,
        TypeName, GenericIdentifier,
        GenericPattern, VariadicGenericPattern,
        Block,
        BinaryOp, UnaryOp, Operator, Call,
        ControlLet, ControlIf, ControlWhile, ControlReturn,
    }


    public class ASTNode
    {
        public ASTNodeKind kind;
        private Diagnostics.Span span;
        private Diagnostics.Span spanWithDelimiters;
        public List<ASTNode> children = new List<ASTNode>();
        public string overwrittenExcerpt = null;


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


        public ASTNode CloneWithoutChildren()
        {
            var newNode = new ASTNode(this.kind);
            newNode.span = this.span;
            newNode.spanWithDelimiters = this.spanWithDelimiters;
            newNode.overwrittenExcerpt = this.overwrittenExcerpt;
            newNode.children = new List<ASTNode>();
            return newNode;
        }


        public ASTNode CloneWithChildren()
        {
            var newNode = new ASTNode(this.kind);
            newNode.span = this.span;
            newNode.spanWithDelimiters = this.spanWithDelimiters;
            newNode.overwrittenExcerpt = this.overwrittenExcerpt;
            newNode.children = new List<ASTNode>();
            foreach (var child in this.children)
                newNode.children.Add(child.CloneWithChildren());
            return newNode;
        }


        public string GetExcerpt(Interface.SourceCode src)
        {
            if (src == null)
                return "<unknown source>";
            else
                return this.overwrittenExcerpt ?? src.GetExcerpt(this.span);
        }


        public string GetExcerptWithComments(Interface.SourceCode src)
        {
            if (src == null)
                return "<unknown source>";
            else if (this.overwrittenExcerpt != null)
                return "***" + this.overwrittenExcerpt + "***";
            else
                return src.GetExcerpt(this.span);
        }


        public void OverwriteExcerpt(string excerpt)
        {
            this.overwrittenExcerpt = excerpt;
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


        public void PrintDebugRecursive(Interface.SourceCode src, int indentLevel, int firstIndentLevel)
        {
            string indentation =
                new string(' ', firstIndentLevel * 2) +
                (firstIndentLevel >= indentLevel ? "" : "| " + new string(' ', (indentLevel - firstIndentLevel - 1) * 2));

            string firstColumn =
                indentation +
                System.Enum.GetName(typeof(ASTNodeKind), this.kind);

            string excerpt = this.GetExcerptWithComments(src);
            string secondColumn =
                new string(' ', indentLevel * 2) +
                excerpt.Substring(0, Math.Min(excerpt.Length, 20)).Replace("\n", " ").Replace("\r", "").Replace("\t", " ") +
                (excerpt.Length > 20 ? "..." : "");

            Console.Out.Write(firstColumn.PadRight(40));
            Console.Out.Write(": ");
            Console.Out.WriteLine(secondColumn);
            foreach (var child in this.EnumerateChildren())
                child.PrintDebugRecursive(src, indentLevel + 1, firstIndentLevel);
        }
    }
}
