using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class DefFunct : Def
    {
        public class Variable
        {
            public string name;
            public Type type;
            public Diagnostics.Span declSpan;
            public bool outOfScope;

            public Variable()
            {
            }

            public Variable(string name, Type type, Diagnostics.Span declSpan)
            {
                this.name = name;
                this.type = type;
                this.declSpan = declSpan;
                this.outOfScope = false;
            }
        }

        public List<Variable> arguments = new List<Variable>();
        public Type returnType;

        public List<Variable> localVariables = new List<Variable>();
        public CodeSegment body;


        public void ResolveSignature(Interface.Session session, TopDecl topDecl, PatternReplacementCollection subst, Grammar.ASTNode defNode)
        {
            foreach (var argNode in defNode.EnumerateChildren())
            {
                if (argNode.kind != Grammar.ASTNodeKind.FunctArgDecl)
                    continue;

                var argName = argNode.Child(0).GetExcerpt();

                var argDef = new DefFunct.Variable();
                argDef.name = argName;
                argDef.declSpan = argNode.Span();

                try
                {
                    session.diagn.PushContext(new MessageContext("while resolving type '" + ASTTypeUtil.GetString(argNode.Child(1)) + "'", argNode.GetOriginalSpan()));
                    argDef.type = ASTTypeUtil.Resolve(session, subst, argNode.Child(1), false);
                    arguments.Add(argDef);
                    localVariables.Add(argDef);
                }
                catch (Semantics.CheckException) { }
                finally { session.diagn.PopContext(); }
            }


            returnType = new TypeVoid();
            foreach (var retNode in defNode.EnumerateChildren())
            {
                if (retNode.kind != Grammar.ASTNodeKind.FunctReturnDecl)
                    continue;

                try
                {
                    session.diagn.PushContext(new MessageContext("while resolving type '" + ASTTypeUtil.GetString(retNode.Child(0)) + "'", retNode.GetOriginalSpan()));
                    returnType = ASTTypeUtil.Resolve(session, subst, retNode.Child(0), true);
                }
                catch (Semantics.CheckException) { }
                finally { session.diagn.PopContext(); }
            }
        }


        public void ResolveBody(Interface.Session session, TopDecl topDecl, PatternReplacementCollection subst, Grammar.ASTNode defNode)
        {
            session.diagn.PushContext(new MessageContext("in funct '" + topDecl.GetString() + "'", topDecl.qualifiedNameASTNode.Span()));
            try
            {
                body = CodeAnalyzer.Analyze(session, defNode.ChildWithKind(Grammar.ASTNodeKind.Block), localVariables);
            }
            finally { session.diagn.PopContext(); }
        }
    }
}
