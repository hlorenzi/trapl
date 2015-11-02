using System;
using System.Collections.Generic;


namespace Trapl.Semantics
{
    public class CodeBody
    {
        public CodeNode code;
        public List<Variable> localVariables = new List<Variable>();
    }


    public class CodeNode
    {
        public Diagnostics.Span span;
        public List<CodeNode> children = new List<CodeNode>();
        public Type outputType;


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


    public class NameInference
    {
        public Grammar.ASTNode pathASTNode;
        public Template template;
    }


    public class CodeNodeSequence : CodeNode
    {
    }


    public class CodeNodeControlLet : CodeNode
    {
        public Variable local;
    }


    public class CodeNodeAssign : CodeNode
    {
    }


    public class CodeNodeLocal : CodeNode
    {
        public int localIndex = -1;
        public override string GetDebugString(Infrastructure.Session session) { return "LOCAL " + localIndex; }
    }


    public class CodeNodeFunct : CodeNode
    {
        public NameInference nameInference = new NameInference();
        public List<DeclFunct> potentialFuncts = new List<DeclFunct>();
        public override string GetDebugString(Infrastructure.Session session)
        {
            if (potentialFuncts.Count == 0) return "NO FUNCT";
            else if (potentialFuncts.Count > 1) return "AMBIGUOUS FUNCT";
            else return NameASTUtil.GetString(potentialFuncts[0].nameASTNode);
        }
    }


    public class CodeNodeStructLiteral : CodeNode
    {
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
