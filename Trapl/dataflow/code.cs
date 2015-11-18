using System;
using System.Collections.Generic;
using Trapl.Infrastructure;


namespace Trapl.Dataflow
{
    public class CodeBody
    {
        public List<CodeSegment> segments = new List<CodeSegment>();
        public List<Variable> localVariables = new List<Variable>();
        public Infrastructure.Type returnType;


        public virtual string GetDebugString(Infrastructure.Session session) { return ""; }


        public void PrintDebug(Infrastructure.Session session, int indentLevel)
        {
            string indentation =
                new string(' ', indentLevel * 2);

            for (int i = 0; i < segments.Count; i++)
            {
                Console.Out.WriteLine(indentation + "=== Segment " + i + " ===");
                foreach (var node in segments[i].nodes)
                    node.PrintDebug(session, indentLevel + 1);
            }
        }
    }


    public class CodeSegment
    {
        public List<CodeNode> nodes = new List<CodeNode>();
    }


    public abstract class CodeNode
    {
        public virtual string GetDebugString(Infrastructure.Session session) { return ""; }


        public void PrintDebug(Infrastructure.Session session, int indentLevel)
        {
            string indentation =
                new string(' ', indentLevel * 2);

            string text =
                this.GetType().Name.Substring("CodeNode".Length) + " " +
                GetDebugString(session);

            Console.Out.WriteLine(indentation + text);
        }
    }


    public class CodeNodePop : CodeNode
    {

    }


    public class CodeNodePushLocalValue : CodeNode
    {
        public int localIndex = -1;


        public CodeNodePushLocalValue(int index)
        {
            localIndex = index;
        }


        public override string GetDebugString(Infrastructure.Session session)
        {
            return localIndex.ToString();
        }
    }


    public class CodeNodePushLocalReference : CodeNode
    {
        public int localIndex = -1;


        public CodeNodePushLocalReference(int index)
        {
            localIndex = index;
        }


        public override string GetDebugString(Infrastructure.Session session)
        {
            return localIndex.ToString();
        }
    }


    public class CodeNodePushNumberLiteral : CodeNode
    {
        public string value;
        public Infrastructure.Type type;
    }


    public class CodeNodeAccess : CodeNode
    {
        public int fieldIndex = -1;


        public CodeNodeAccess(int index)
        {
            fieldIndex = index;
        }


        public override string GetDebugString(Infrastructure.Session session)
        {
            return fieldIndex.ToString();
        }
    }


    public class CodeNodeAssign : CodeNode
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


    public class CodeNodeReturn : CodeNode
    {

    }


    public class CodeNodeGoto : CodeNode
    {
        public int destinationSegment = -1;


        public CodeNodeGoto(int dest)
        {
            destinationSegment = dest;
        }


        public override string GetDebugString(Infrastructure.Session session)
        {
            return destinationSegment.ToString();
        }
    }


    public class CodeNodeBranch : CodeNode
    {
        public int destinationSegmentOnTrue = -1;
        public int destinationSegmentOnFalse = -1;


        public CodeNodeBranch(int destTrue, int destFalse)
        {
            destinationSegmentOnTrue = destTrue;
            destinationSegmentOnFalse = destFalse;
        }


        public override string GetDebugString(Infrastructure.Session session)
        {
            return destinationSegmentOnTrue.ToString() + ", " + destinationSegmentOnFalse.ToString();
        }
    }
}
