using System.Collections.Generic;


namespace Trapl.Semantics
{
    public class CodeSegment
    {
        public List<CodeSegment> outwardPaths = new List<CodeSegment>();
        public List<CodeNode> nodes = new List<CodeNode>();

        public void GoesTo(CodeSegment other)
        {
            outwardPaths.Add(other);
        }
    }


    public abstract class CodeNode
    {
        public virtual string Name() { return "error"; }
    }


    public class CodeNodeLocalBegin : CodeNode
    {
        public int localIndex;

        public override string Name() { return "LocalBegin " + localIndex; }
    }


    public class CodeNodeLocalEnd : CodeNode
    {
        public int localIndex;

        public override string Name() { return "LocalEnd " + localIndex; }
    }


    public class CodeNodePushLocal : CodeNode
    {
        public int localIndex;

        public override string Name() { return "PushLocal " + localIndex; }
    }


    public class CodeNodePushLiteral : CodeNode
    {
        public Type type;
        public string literalExcerpt;

        public override string Name() { return "PushLiteral '" + literalExcerpt + "'"; }
    }


    public class CodeNodePushFunct : CodeNode
    {
        public TopDecl topDecl;

        public override string Name() { return "PushFunct '" + topDecl.GetString() + "'"; }
    }


    public class CodeNodeAccess : CodeNode
    {
        public DefStruct accessedStruct;
        public int memberIndex;

        public CodeNodeAccess(DefStruct st, int memberIndex)
        {
            this.accessedStruct = st;
            this.memberIndex = memberIndex;
        }

        public override string Name() { return "Access '" + accessedStruct.topDecl.GetString() + "." + accessedStruct.members[memberIndex].name + "'"; }
    }


    public class CodeNodeStore : CodeNode
    {
        public override string Name() { return "Store"; }
    }


    public class CodeNodePop : CodeNode
    {
        public override string Name() { return "Pop"; }
    }


    public class CodeNodeAddress : CodeNode
    {
        public override string Name() { return "Address"; }
    }


    public class CodeNodeDereference : CodeNode
    {
        public override string Name() { return "Dereference"; }
    }


    public class CodeNodeCall : CodeNode
    {
        public override string Name() { return "Call"; }
    }


    public class CodeNodeIf : CodeNode
    {
        public override string Name() { return "If"; }
    }


    public class CodeNodeReturn : CodeNode
    {
        public Type exprType;

        public override string Name() { return "Return"; }
    }
}
