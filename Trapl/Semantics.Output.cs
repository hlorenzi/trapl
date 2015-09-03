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
        public List<Structure.Declaration> templStructDecls = new List<Structure.Declaration>();
        public List<Structure.Declaration> templFunctDecls = new List<Structure.Declaration>();


        public void PrintFunctsDebug()
        {
            foreach (var f in functDefs)
            {
                Console.Out.WriteLine("FUNCT " + f.name);
                var segments = new List<CodeSegment>();
                segments.Add(f.body);

                for (int i = 0; i < f.localVariables.Count; i++)
                {
                    Console.Out.WriteLine("  LOCAL " + i + " = " + f.localVariables[i].name + ": " + f.localVariables[i].type.Name());
                }

                Console.Out.WriteLine();

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


    public class TemplateList
    {
        public enum ParameterKind
        {
            Specific, Generic, Variadic
        }


        public class Parameter
        {
            public ParameterKind kind;
            public string genericName;
            public VariableType specificType;
        }


        public List<Parameter> parameters = new List<Parameter>();


        public bool IsGeneric()
        {
            foreach (var p in parameters)
            {
                if (p.kind != ParameterKind.Specific) return true;
            }
            return false;
        }


        public string SpecificName()
        {
            var result = "";
            if (parameters.Count > 0)
            {
                result += "__templ__";
                for (int i = 0; i < parameters.Count; i++)
                {
                    result += parameters[i].specificType.Name();
                    if (i < parameters.Count - 1)
                        result += "__";
                }
            }
            return result;
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
        public TemplateList templateList = new TemplateList();
        public List<Variable> arguments = new List<Variable>();
        public VariableType returnType;
        public List<Variable> localVariables = new List<Variable>();
        public CodeSegment body;
        public Source source;
        public Diagnostics.Span nameSpan;
        public Diagnostics.Span declSpan;


        public FunctDef(string name, Source source, Diagnostics.Span nameSpan, Diagnostics.Span declSpan)
        {
            this.name = name;
            this.source = source;
            this.nameSpan = nameSpan;
            this.declSpan = declSpan;
        }
    }


    public abstract class VariableType
    {
        public bool addressable;

        public virtual string Name() { return "error"; }
        public virtual bool IsSame(VariableType other) { return false; }
    }


    public class VariableTypePointer : VariableType
    {
        public VariableType pointeeType;

        public VariableTypePointer(VariableType pointeeType) { this.pointeeType = pointeeType; }
        public override string Name() { return "&" + pointeeType.Name(); }
        public override bool IsSame(VariableType other)
        {
            if (!(other is VariableTypePointer)) return false;
            return (((VariableTypePointer)other).pointeeType.IsSame(this.pointeeType));
        }
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
    

    public class VariableTypeFunct : VariableType
    {
        public List<VariableType> argumentTypes = new List<VariableType>();
        public VariableType returnType;

        public override string Name()
        {
            var result = "(";
            for (int i = 0; i < argumentTypes.Count; i++)
            {
                result += argumentTypes[i].Name();
                if (i < argumentTypes.Count - 1) result += ", ";
            }
            return result + " -> " + returnType.Name() + ")";
        }

        public override bool IsSame(VariableType other)
        {
            var otherf = other as VariableTypeFunct;
            if (otherf == null) return false;
            if (this.argumentTypes.Count != otherf.argumentTypes.Count) return false;
            for (int i = 0; i < argumentTypes.Count; i++)
                if (!this.argumentTypes[i].IsSame(otherf.argumentTypes[i])) return false;
            return true;
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


    public class CodeNodePushLocalAddress : CodeNode
    {
        public int localIndex;

        public override string Name() { return "PushLocalAddress " + localIndex; }
    }


    public class CodeNodePushLiteral : CodeNode
    {
        public VariableType type;
        public string literalExcerpt;

        public override string Name() { return "PushLiteral " + literalExcerpt; }
    }


    public class CodeNodePushFunct : CodeNode
    {
        public int functIndex;

        public override string Name() { return "PushFunct " + functIndex; }
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
}
