using System;
using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class DefStruct : Def
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


        public List<Member> members = new List<Member>();


        public DefStruct Clone()
        {
            var def = (DefStruct)this.MemberwiseClone();
            def.members = new List<Member>();
            foreach (var member in this.members)
                def.members.Add(member.Clone());
            return def;
        }


        public void Resolve(Interface.Session session, TopDecl topDecl, PatternReplacementCollection subst, Interface.SourceCode src, Grammar.ASTNode defNode)
        {
            foreach (var memberNode in defNode.EnumerateChildren())
            {
                if (memberNode.kind != Grammar.ASTNodeKind.StructMemberDecl)
                    throw new InternalException("node is not a StructMemberDecl");

                var memberName = memberNode.Child(0).GetExcerpt();

                try
                {
                    var memberDef = new DefStruct.Member();
                    memberDef.name = memberName;
                    memberDef.declSpan = memberNode.Span();
                    memberDef.type = ASTTypeUtil.Resolve(session, subst, src, memberNode.Child(1));
                    members.Add(memberDef);
                }
                catch (Semantics.CheckException) { }
            }
        }


        public override void PrintToConsole(Interface.Session session, int indentLevel)
        {
            foreach (var member in this.members)
            {
                Console.Out.WriteLine(
                    new string(' ', indentLevel * 2) +
                    member.name + ": " +
                    member.type.GetString(session));
            }
        }
    }
}
