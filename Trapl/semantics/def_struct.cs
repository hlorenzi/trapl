using System.Collections.Generic;


namespace Trapl.Semantics
{
    public class DefinitionStruct
    {
        public class Member
        {
            public string name;
            public Type type;
            public Diagnostics.Span declSpan;


            public Member Clone()
            {
                return (Member)this.MemberwiseClone();
            }
        }

        public Template templateList;

        public bool resolved;
        public bool resolving;
        public bool synthesized;
        public Grammar.ASTNode declASTNode;

        public List<Member> members = new List<Member>();

        public Interface.SourceCode source;
        public Diagnostics.Span nameSpan;
        public Diagnostics.Span declSpan;


        public DefinitionStruct Clone()
        {
            var def = (DefinitionStruct)this.MemberwiseClone();
            def.members = new List<Member>();
            foreach (var member in this.members)
                def.members.Add(member.Clone());
            return def;
        }
    }
}
