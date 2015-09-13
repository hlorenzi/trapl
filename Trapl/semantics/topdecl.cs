using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class TopDecl
    {
        public Interface.SourceCode source;

        public string qualifiedName;
        public Grammar.ASTNode qualifiedNameASTNode;

        public Grammar.ASTNode patternASTNode;

        public PatternReplacementCollection patternSubst = new PatternReplacementCollection();

        public Def def;
        public Grammar.ASTNode defASTNode;

        public bool resolved;
        public bool generic;
        public bool synthesized;


        public void Resolve(Interface.Session session)
        {
            if (this.resolved || this.generic)
                return;

            if (this.defASTNode.kind == Grammar.ASTNodeKind.StructDecl)
            {
                if (this.def != null)
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.StructRecursion,
                        "infinite struct recursion", this.defASTNode.Span());
                    throw new Semantics.CheckException();
                }

                var defStruct = new DefStruct();
                this.def = defStruct;
                defStruct.Resolve(session, this, this.patternSubst, this.source, this.defASTNode);
                this.resolved = true;
            }
            else
                throw new InternalException("unexpected def node");
        }


        public TopDecl CloneAndSubstitute(Interface.Session session, PatternReplacementCollection subst)
        {
            var newDecl = (TopDecl)this.MemberwiseClone();
            newDecl.patternSubst = subst;
            newDecl.def = null;
            newDecl.resolved = false;
            newDecl.generic = false;
            newDecl.synthesized = true;
            newDecl.defASTNode = ASTPatternReplacer.CloneReplaced(session, newDecl.defASTNode, subst);
            newDecl.patternASTNode = ASTPatternReplacer.CloneReplaced(session, newDecl.patternASTNode, subst);
            return newDecl;
        }
    }


    public abstract class Def
    {
        public virtual void PrintToConsole(Interface.Session session, int indentLevel) { }
    }
}
