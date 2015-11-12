using System;
using System.Collections.Generic;


namespace Trapl.Grammar
{
    public enum ASTNodeKind
    {
        TopLevelDecl,
        FunctDecl, FunctArg, FunctReturnType, FunctBody,
        StructDecl, StructField,
        TraitDecl, TraitFn,
        Type, TupleType, FunctType,
        Name, Path, Identifier,
        NumberLiteral, BooleanLiteral, StructLiteral, StructFieldInit,
        TemplateList, TemplateVariadicList,
        TemplateParameter, TypeParameter, GenericTypeParameter,
        Block,
        BinaryOp, UnaryOp, Operator, Call,
        ControlLet, ControlIf, ControlWhile, ControlReturn,
    }


    public class ASTNode
    {
        public ASTNodeKind kind;
        private Diagnostics.Span span;
        public List<ASTNode> children = new List<ASTNode>();

        private bool hasOriginalSpan = false;
        private Diagnostics.Span originalSpan;

        private string overwrittenExcerpt = null;


        public ASTNode(ASTNodeKind kind)
        {
            this.kind = kind;
        }


        public ASTNode(ASTNodeKind kind, Diagnostics.Span span)
        {
            this.kind = kind;
            this.span = span;
        }


        public ASTNode CloneWithoutChildren()
        {
            var newNode = new ASTNode(this.kind);
            newNode.span = this.span;
            newNode.originalSpan = this.originalSpan;
            newNode.overwrittenExcerpt = this.overwrittenExcerpt;
            newNode.children = new List<ASTNode>();
            return newNode;
        }


        public ASTNode CloneWithChildren()
        {
            var newNode = this.CloneWithoutChildren();
            foreach (var child in this.children)
                newNode.children.Add(child.CloneWithChildren());
            return newNode;
        }


        public string GetExcerpt()
        {
            return (this.overwrittenExcerpt ?? this.span.GetExcerpt());
        }


        public string GetExcerptWithComments()
        {
            return (this.overwrittenExcerpt ?? this.span.GetExcerpt());
        }


        public void OverwriteExcerpt(string str)
        {
            this.overwrittenExcerpt = str;
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


        public Diagnostics.Span GetOriginalSpan()
        {
            return (this.hasOriginalSpan ? this.originalSpan : this.span);
        }


        public void SetOriginalSpan(Diagnostics.Span span)
        {
            this.hasOriginalSpan = true;
            this.originalSpan = span;
        }


        public void AddChild(ASTNode node)
        {
            this.children.Add(node);
            this.AddSpan(node.Span());
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


        public virtual IEnumerable<ASTNode> EnumerateChildren()
        {
            foreach (var child in this.children)
                yield return child;
        }


        public void PrintDebugRecursive(int indentLevel, int firstIndentLevel)
        {
            string indentation =
                new string(' ', firstIndentLevel * 2) +
                (firstIndentLevel >= indentLevel ? "" : "| " + new string(' ', (indentLevel - firstIndentLevel - 1) * 2));

            string firstColumn =
                indentation +
                System.Enum.GetName(typeof(ASTNodeKind), this.kind);

            string excerpt = this.GetExcerptWithComments();
            string secondColumn =
                new string(' ', indentLevel * 2) +
                excerpt.Substring(0, Math.Min(excerpt.Length, 20)).Replace("\n", " ").Replace("\r", "").Replace("\t", " ") +
                (excerpt.Length > 20 ? "..." : "");

            Console.Out.Write(firstColumn.PadRight(40));
            Console.Out.Write(": ");
            Console.Out.WriteLine(secondColumn);
            foreach (var child in this.EnumerateChildren())
                child.PrintDebugRecursive(indentLevel + 1, firstIndentLevel);
        }


        public override string ToString()
        {
            var result = "{";
            result +=
                System.Enum.GetName(typeof(ASTNodeKind), this.kind) + " " +
                "'" + this.GetExcerptWithComments() + "'";

            if (this.children.Count > 0)
                result += " ";

            for (int i = 0; i < this.children.Count; i++)
                result += this.children[i].ToString();

            return result + "}";
        }
    }
}
