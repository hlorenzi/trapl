using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trapl.Semantics
{
    public class Output
    {
        public List<StructDef> structDefs = new List<StructDef>();
        public List<FunctDef> functDefs = new List<FunctDef>();
    }


    public class StructDef
    {
        public class Member
        {
            public string name;
            public VariableType type;
            public Diagnostics.Span declSpan;
        }

        public string name;
        public List<Member> members = new List<Member>();
        public Source source;
        public Diagnostics.Span declSpan;


        public StructDef(string name)
        {
            this.name = name;
            this.source = null;
            this.declSpan = new Diagnostics.Span();
        }


        public StructDef(string name, Source source, Diagnostics.Span declSpan)
        {
            this.name = name;
            this.source = source;
            this.declSpan = declSpan;
        }
    }


    public class FunctDef
    {
        public class Variable
        {
            public string name;
            public VariableType type;
            public Diagnostics.Span declSpan;

            public Variable(string name, VariableType type, Diagnostics.Span declSpan)
            {
                this.name = name;
                this.type = type;
                this.declSpan = declSpan;
            }
        }

        public string name;
        public List<Variable> arguments = new List<Variable>();
        public VariableType returnType;
        public List<Variable> localVariables = new List<Variable>();
        public Source source;
        public Diagnostics.Span declSpan;


        public FunctDef(string name, Source source, Diagnostics.Span declSpan)
        {
            this.name = name;
            this.source = source;
            this.declSpan = declSpan;
        }
    }


    public abstract class VariableType
    {
        public virtual string Name() { return "error"; }
        public virtual bool IsSame(VariableType other) { return false; }
    }


    public class VariableTypeStruct : VariableType
    {
        public StructDef structDef;

        public override string Name() { return structDef.name; }
        public override bool IsSame(VariableType other)
        {
            if (!(other is VariableTypeStruct)) return false;
            return (((VariableTypeStruct)other).structDef == this.structDef);
        }
    }


    public class CodeSegment
    {
        public List<CodeSegment> outwardPaths = new List<CodeSegment>();
        public List<CodeNode> nodes = new List<CodeNode>();
    }


    public abstract class CodeNode
    {
        public virtual string Name() { return "error"; }
    }


    public abstract class CodeNodeVariableBegin : CodeNode
    {
        public int localIndex;

        public override string Name() { return "VariableBegin " + localIndex; }
    }


    public abstract class CodeNodeVariableEnd : CodeNode
    {
        public int localIndex;

        public override string Name() { return "VariableEnd " + localIndex; }
    }
}
