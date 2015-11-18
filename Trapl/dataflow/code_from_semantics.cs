using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trapl.Dataflow
{
    public class CodeSemanticConverter
    {
        public static CodeBody Convert(Infrastructure.Session session, Semantics.CodeBody semanticBody)
        {
            var converter = new CodeSemanticConverter(session, semanticBody);
            converter.Convert(semanticBody.code, 0);
            return converter.dataflowBody;
        }


        private Infrastructure.Session session;
        private Semantics.CodeBody semanticBody;
        private Dataflow.CodeBody dataflowBody = new CodeBody();
        private Stack<bool> inLhsContext = new Stack<bool>();


        private CodeSemanticConverter(Infrastructure.Session session, Semantics.CodeBody semanticBody)
        {
            this.session = session;
            this.semanticBody = semanticBody;
            this.dataflowBody.segments.Add(new CodeSegment());
            this.inLhsContext.Push(false);
        }


        private int AddSegment()
        {
            dataflowBody.segments.Add(new CodeSegment());
            return dataflowBody.segments.Count - 1;
        }


        private void AddNode(int segment, CodeNode node)
        {
            dataflowBody.segments[segment].nodes.Add(node);
        }


        private int Convert(Semantics.CodeNode node, int entrySegment)
        {
            if (node is Semantics.CodeNodeSequence)
                return ConvertSequence((Semantics.CodeNodeSequence)node, entrySegment);
            else if (node is Semantics.CodeNodeControlLet)
                return ConvertControlLet((Semantics.CodeNodeControlLet)node, entrySegment);
            else if (node is Semantics.CodeNodeAssign)
                return ConvertAssignment((Semantics.CodeNodeAssign)node, entrySegment);
            else if (node is Semantics.CodeNodeLocal)
                return ConvertLocal((Semantics.CodeNodeLocal)node, entrySegment);
            else if (node is Semantics.CodeNodeIntegerLiteral)
                return ConvertIntegerLiteral((Semantics.CodeNodeIntegerLiteral)node, entrySegment);
            else
                throw new Infrastructure.InternalException("not implemented");
        }


        private int ConvertSequence(Semantics.CodeNodeSequence node, int entrySegment)
        {
            int curSegment = AddSegment();
            AddNode(entrySegment, new CodeNodeGoto(curSegment));

            foreach (var child in node.children)
            {
                curSegment = Convert(child, curSegment);
            }

            return curSegment;
        }


        private int ConvertControlLet(Semantics.CodeNodeControlLet node, int entrySegment)
        {
            if (node.children.Count == 1)
            {
                AddNode(entrySegment, new CodeNodePushLocalReference(node.localIndex));
                int exitSegment = Convert(node.children[0], entrySegment);
                AddNode(exitSegment, new CodeNodeAssign());
                return exitSegment;
            }
            else
                return entrySegment;
        }


        private int ConvertAssignment(Semantics.CodeNodeAssign node, int entrySegment)
        {
            inLhsContext.Push(true);
            int lhsExitSegment = Convert(node.children[0], entrySegment);
            inLhsContext.Pop();
            int rhsExitSegment = Convert(node.children[1], lhsExitSegment);
            AddNode(rhsExitSegment, new CodeNodeAssign());
            return rhsExitSegment;
        }


        private int ConvertLocal(Semantics.CodeNodeLocal node, int entrySegment)
        {
            if (inLhsContext.Peek())
                AddNode(entrySegment, new CodeNodePushLocalReference(node.localIndex));
            else
                AddNode(entrySegment, new CodeNodePushLocalValue(node.localIndex));

            return entrySegment;
        }


        private int ConvertIntegerLiteral(Semantics.CodeNodeIntegerLiteral node, int entrySegment)
        {
            AddNode(entrySegment, new CodeNodePushNumberLiteral());
            return entrySegment;
        }
    }
}
