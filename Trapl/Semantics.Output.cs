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


    public abstract class VariableType
    {
        public virtual string Name() { return "error"; }
        public virtual bool IsCompatible(VariableType other) { return false; }
    }


    public class VariableTypeStruct : VariableType
    {
        public StructDef structDef;

        public override string Name() { return structDef.name; }
    }
}
