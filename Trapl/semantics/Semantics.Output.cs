using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trapl.Semantics
{
    /*public class DeclarationCollectionOLD
    {
        public List<StructDef> structDefs = new List<StructDef>();
        public List<FunctDef> functDefs = new List<FunctDef>();
        public List<Semantics.Declaration> templStructDecls = new List<Semantics.Declaration>();
        public List<Semantics.Declaration> templFunctDecls = new List<Semantics.Declaration>();


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
    }*/
}
