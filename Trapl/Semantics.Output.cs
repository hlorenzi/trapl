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

        public void PrintFunctsDebug()
        {
            foreach (var f in functDefs)
            {
                Console.Out.WriteLine("FUNCT " + f.name);
                var segments = new List<CodeSegment>();
                segments.Add(f.body);

                for (int i = 0; i < segments.Count; i++)
                {
                    Console.Out.WriteLine("  === Segment " + i + " ===");
                    foreach (var c in segments[i].nodes)
                    {
                        Console.Out.WriteLine("    " + c.Name());
                    }

                    var goesToStr = "";
                    for (int j = 0; j < segments[i].outwardPaths.Count; j++)
                    {
                        int index = segments.FindIndex(seg => seg == segments[i].outwardPaths[j]);
                        if (index < 0)
                        {
                            segments.Add(segments[i].outwardPaths[j]);
                            index = segments.Count - 1;
                        }
                        goesToStr += index;
                        if (j < segments[i].outwardPaths.Count - 1)
                            goesToStr += ", ";
                    }

                    if (segments[i].outwardPaths.Count == 0)
                        goesToStr = "end";

                    Console.Out.WriteLine("    -> Goes to " + goesToStr);
                    Console.Out.WriteLine();
                }
            }
        }
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
            public bool outOfScope;

            public Variable(string name, VariableType type, Diagnostics.Span declSpan)
            {
                this.name = name;
                this.type = type;
                this.declSpan = declSpan;
                this.outOfScope = false;
            }
        }

        public string name;
        public List<Variable> arguments = new List<Variable>();
        public VariableType returnType;
        public List<Variable> localVariables = new List<Variable>();
        public CodeSegment body;
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

        public void GoesTo(CodeSegment other)
        {
            outwardPaths.Add(other);
        }
    }


    public abstract class CodeNode
    {
        public virtual string Name() { return "error"; }
    }


    public class CodeNodeVariableBegin : CodeNode
    {
        public int localIndex;

        public override string Name() { return "VariableBegin " + localIndex; }
    }


    public class CodeNodeVariableEnd : CodeNode
    {
        public int localIndex;

        public override string Name() { return "VariableEnd " + localIndex; }
    }


    public class CodeNodeStoreLocal : CodeNode
    {
        public int localIndex;

        public override string Name() { return "StoreLocal " + localIndex; }
    }


    public class CodeNodePushLocal : CodeNode
    {
        public int localIndex;

        public override string Name() { return "PushLocal " + localIndex; }
    }


    public class CodeNodePushLiteral : CodeNode
    {
        public VariableType type;
        public string literalExcerpt;

        public override string Name() { return "PushLiteral " + literalExcerpt; }
    }


    public class CodeNodePop : CodeNode
    {
        public override string Name() { return "Pop"; }
    }


    public class CodeNodeCall : CodeNode
    {
        public override string Name() { return "Call"; }
    }


    public class CodeNodeIf : CodeNode
    {
        public override string Name() { return "If"; }
    }
}
