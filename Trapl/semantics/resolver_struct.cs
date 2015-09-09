using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class ResolverStruct
    {
        public static void Resolve(Interface.Session session, TemplateSubstitution subst, DefinitionStruct def)
        {
            if (def.resolved)
                return;

            if (def.resolving)
            {
                session.diagn.Add(MessageKind.Error, MessageCode.StructRecursion,
                    "infinite struct recursion", def.source, def.declSpan);
                throw new Semantics.CheckException();
            }

            def.resolving = true;

            foreach (var memberNode in def.declASTNode.EnumerateChildren())
            {
                if (memberNode.kind != Grammar.ASTNodeKind.StructMemberDecl)
                    continue;

                try
                {
                    var memberDef = new DefinitionStruct.Member();
                    memberDef.name = def.source.GetExcerpt(memberNode.Child(0).Span());
                    memberDef.declSpan = memberNode.Span();
                    memberDef.type = ResolverType.Resolve(session, subst, def.source, memberNode.Child(1));
                    def.members.Add(memberDef);
                }
                catch (Semantics.CheckException) { }
            }

            def.resolving = false;
            def.resolved = true;
        }
    }
}
