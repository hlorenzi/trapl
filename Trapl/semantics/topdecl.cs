using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class TopDecl
    {
        public Grammar.ASTNode declASTNode;

        public string qualifiedName;
        public Grammar.ASTNode qualifiedNameASTNode;

        public Grammar.ASTNode patternASTNode;

        public PatternReplacementCollection patternRepl = new PatternReplacementCollection();

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
                defStruct.Resolve(session, this, this.patternRepl, this.defASTNode);
                this.resolved = true;
            }
            else if (this.defASTNode.kind == Grammar.ASTNodeKind.FunctDecl)
            {
                var defFunct = new DefFunct();
                this.def = defFunct;
                defFunct.ResolveSignature(session, this, this.patternRepl, this.defASTNode);
                this.resolved = true;
            }
            else
                throw new InternalException("unexpected def node");
        }


        public void ResolveBody(Interface.Session session)
        {
            if (this.generic || !(this.def is DefFunct))
                return;

            var funct = (DefFunct)this.def;
            funct.ResolveBody(session, this, this.patternRepl, this.defASTNode);
        }


        public TopDecl Clone()
        {
            var newDecl = (TopDecl)this.MemberwiseClone();
            newDecl.def = null;
            newDecl.resolved = false;
            newDecl.generic = false;
            newDecl.synthesized = true;
            return newDecl;
        }


        public string GetString()
        {
            return (this.qualifiedName + 
                (Semantics.ASTPatternUtil.IsEmpty(this.patternASTNode) ?
                "" :
                "::" + Semantics.ASTPatternUtil.GetString(this.patternASTNode)));
        }
    }


    public abstract class Def
    {
        public virtual void PrintToConsole(Interface.Session session, int indentLevel) { }
    }
}
