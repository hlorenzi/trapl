using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class TopDecl
    {
        public Interface.SourceCode source;

        public string qualifiedName;
        public Grammar.ASTNode qualifiedNameASTNode;

        public DeclPattern pattern;
        public Grammar.ASTNode patternASTNode;

        public DeclPatternSubstitution patternSubst = new DeclPatternSubstitution();

        public Def def;
        public Grammar.ASTNode defASTNode;

        public bool resolved;
        public bool generic;
        public bool synthesized;


        public void Resolve(Interface.Session session)
        {
            Interface.Debug.BeginSection("TOPDECL RESOLVE '" + qualifiedName + "::" + pattern.GetString(session) + "' " + patternSubst.GetString());

            if (this.resolved || this.generic)
            {
                Interface.Debug.Note(this.resolved ? "already resolved" : "is generic");
                Interface.Debug.EndSection();
                return;
            }

            if (this.defASTNode.kind == Grammar.ASTNodeKind.StructDecl)
            {
                if (this.def != null)
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.StructRecursion,
                        "infinite struct recursion", this.source, this.defASTNode.Span());
                    throw new Semantics.CheckException();
                }

                var defStruct = new DefStruct();
                this.def = defStruct;
                defStruct.Resolve(session, this, this.patternSubst, this.source, this.defASTNode);
                this.resolved = true;
            }
            else
                throw new InternalException("unexpected def node");

            Interface.Debug.EndSection();
        }


        public TopDecl CloneAndSubstitute(Interface.Session session, DeclPatternSubstitution subst)
        {
            var newDecl = (TopDecl)this.MemberwiseClone();
            newDecl.patternSubst = subst;
            newDecl.def = null;
            newDecl.resolved = false;
            newDecl.generic = false;
            newDecl.synthesized = true;
            newDecl.defASTNode = ASTPatternSubstitution.CloneAndSubstitute(session, newDecl.source, newDecl.defASTNode, subst);
            newDecl.patternASTNode = ASTPatternSubstitution.CloneAndSubstitute(session, newDecl.source, newDecl.patternASTNode, subst);
            newDecl.pattern = new DeclPattern(newDecl.source, newDecl.patternASTNode);
            Interface.Debug.BeginSection("SUBSTITUTED PATTERN");
            Interface.Debug.PrintAST(newDecl.source, newDecl.patternASTNode);
            Interface.Debug.EndSection();
            return newDecl;
        }
    }


    public abstract class Def
    {
        public virtual void PrintToConsole(Interface.Session session, int indentLevel) { }
    }
}
