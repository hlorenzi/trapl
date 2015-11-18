using System;
using System.Collections.Generic;
using Trapl.Infrastructure;


namespace Trapl.Semantics
{
    public class CodeBody
    {
        public CodeNode code;
        public List<Variable> localVariables = new List<Variable>();
        public Infrastructure.Type returnType;
    }


    public class CodeNode
    {
        public Diagnostics.Span span;
        public List<CodeNode> children = new List<CodeNode>();
        public Infrastructure.Type outputType;


        public virtual string GetDebugString(Infrastructure.Session session) { return ""; }


        public void PrintDebugRecursive(Infrastructure.Session session, int indentLevel, int firstIndentLevel)
        {
            string indentation =
                new string(' ', firstIndentLevel * 2) +
                (firstIndentLevel >= indentLevel ? "" : "| " + new string(' ', (indentLevel - firstIndentLevel - 1) * 2));

            string firstColumn =
                indentation +
                this.GetType().Name.Substring("CodeNode".Length) + " " +
                GetDebugString(session) +
                ": " + outputType.GetString(session);

            Console.Out.WriteLine(firstColumn);
            foreach (var child in this.children)
                child.PrintDebugRecursive(session, indentLevel + 1, firstIndentLevel);
        }
    }


    public class CodeNodeSequence : CodeNode
    {
    }


    public class CodeNodeControlLet : CodeNode
    {
        public int localIndex = -1;
        public override string GetDebugString(Infrastructure.Session session) { return "LOCAL (" + localIndex + ")"; }
    }


    public class CodeNodeControlIf : CodeNode
    {
    }


    public class CodeNodeControlWhile : CodeNode
    {
    }


    public class CodeNodeControlReturn : CodeNode
    {
    }


    public class CodeNodeAssign : CodeNode
    {
    }


    public class CodeNodeLocal : CodeNode
    {
        public int localIndex = -1;
        public override string GetDebugString(Infrastructure.Session session) { return "LOCAL (" + localIndex + ")"; }
    }


    public class CodeNodeFunct : CodeNode
    {
        public Name nameInference = new Name();
        public List<DeclFunct> potentialFuncts = new List<DeclFunct>();
        public override string GetDebugString(Infrastructure.Session session)
        {
            if (potentialFuncts.Count == 0) return "NO FUNCT";
            else if (potentialFuncts.Count > 1) return "AMBIGUOUS FUNCT";
            else return NameUtil.GetDisplayString(potentialFuncts[0].nameASTNode);
        }
    }


    public class CodeNodeBooleanLiteral : CodeNode
    {
        public bool value;
    }


    public class CodeNodeIntegerLiteral : CodeNode
    {
        public string value;
        public override string GetDebugString(Infrastructure.Session session) { return "VALUE (" + value + ")"; }
    }


    public class CodeNodeStructLiteral : CodeNode
    {
    }


    public class CodeNodeAccess : CodeNode
    {
        public Grammar.ASTNode pathASTNode;
        public Template template;
        public DeclStruct structAccessed;
        public int fieldIndexAccessed = -1;

        public override string GetDebugString(Infrastructure.Session session)
        {
            return "FIELD " +
                (structAccessed == null ? "???" : structAccessed.GetString(session)) + "." +
                PathUtil.GetDisplayString(pathASTNode) + template.GetString(session);
        }
    }


    public class CodeNodeAddress : CodeNode
    {
    }


    public class CodeNodeDereference : CodeNode
    {
    }


    public class CodeNodeCall : CodeNode
    {
    }
}
