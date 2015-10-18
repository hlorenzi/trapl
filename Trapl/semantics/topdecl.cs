using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class TopDecl
    {
        public Grammar.ASTNode declASTNode;

        public Grammar.ASTNode nameASTNode;
        public Grammar.ASTNode pathASTNode;
        public Template template;

        public Def def;
        public Grammar.ASTNode defASTNode;

        public bool resolved;
        public bool bodyResolved;


        public void Resolve(Infrastructure.Session session)
        {
            if (this.resolved)
                return;

            if (this.defASTNode.kind == Grammar.ASTNodeKind.StructDecl)
            {
                if (this.def != null)
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.StructRecursion,
                        "infinite struct recursion", this.nameASTNode.Span());
                    throw new Semantics.CheckException();
                }

                var defStruct = new DefStruct(this);
                this.def = defStruct;
                defStruct.Resolve(session, this, this.defASTNode);
                this.resolved = true;
            }
            else if (this.defASTNode.kind == Grammar.ASTNodeKind.FunctDecl)
            {
                var defFunct = new DefFunct(this);
                this.def = defFunct;
                defFunct.ResolveSignature(session, this, this.defASTNode);
                this.resolved = true;
            }
            else
                throw new InternalException("unexpected def node");
        }


        public void ResolveBody(Infrastructure.Session session)
        {
            if (this.bodyResolved || !(this.def is DefFunct))
                return;

            this.bodyResolved = true;
            var funct = (DefFunct)this.def;
            funct.ResolveBody(session, this, this.defASTNode);
        }


        public TopDecl Clone()
        {
            var newDecl = (TopDecl)this.MemberwiseClone();
            newDecl.def = null;
            newDecl.resolved = false;
            newDecl.bodyResolved = false;
            return newDecl;
        }


        public string GetString()
        {
            return ASTPathUtil.GetString(this.pathASTNode);
        }


        public override string ToString()
        {
            return "TopDecl '" + GetString() + "'";
        }
    }


    public abstract class Def
    {
        public TopDecl topDecl;

        public Def(TopDecl topDecl)
        {
            this.topDecl = topDecl;
        }

        public virtual void PrintToConsole(Infrastructure.Session session, int indentLevel) { }
    }
}
