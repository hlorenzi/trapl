using System;
using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Infrastructure
{
    public class DeclFunct : Decl
    {
        public List<Variable> arguments = new List<Variable>();
        public Type returnType;

        public Semantics.CodeBody body;


        public DeclFunct() { }


        public override void Resolve(Infrastructure.Session session)
        {
            if (this.resolved)
                return;

            foreach (var argNode in this.defASTNode.EnumerateChildren())
            {
                if (argNode.kind != Grammar.ASTNodeKind.FunctArg)
                    continue;

                var arg = new Variable();
                arg.name = new Name(
                    argNode.Child(0).Span(),
                    argNode.Child(0).Child(0),
                    Semantics.TemplateUtil.ResolveFromNameAST(session, argNode.Child(0), true));
                arg.declSpan = argNode.Span();

                try
                {
                    arg.type = Semantics.TypeUtil.ResolveFromAST(session, argNode.Child(1), true);
                    this.arguments.Add(arg);
                }
                catch (CheckException) { }
            }


            returnType = new TypeTuple();
            foreach (var retNode in this.defASTNode.EnumerateChildren())
            {
                if (retNode.kind != Grammar.ASTNodeKind.FunctReturnType)
                    continue;

                try
                {
                    this.returnType = Semantics.TypeUtil.ResolveFromAST(session, retNode.Child(0), true);
                }
                catch (CheckException) { }
            }

            this.resolved = true;
        }


        public override void ResolveBody(Infrastructure.Session session)
        {
            if (this.bodyResolved)
                return;

            session.diagn.PushContext(new MessageContext("in funct '" + GetString(session) + "'", this.nameASTNode.Span()));
            try
            {
                body = Semantics.CodeASTConverter.Convert(
                    session, 
                    this.defASTNode.ChildWithKind(Grammar.ASTNodeKind.FunctBody).Child(0),
                    new List<Variable>(this.arguments),
                    returnType);

                Semantics.CodeTypeInferenceAnalyzer.Analyze(session, body);
                Semantics.CodeTypeChecker.Check(session, body);
            }
            finally { session.diagn.PopContext(); }
        }


        public override void PrintToConsole(Session session, int indentLevel)
        {
            if (this.body != null)
            {
                for (int i = 0; i < this.body.localVariables.Count; i++)
                {
                    Console.Out.WriteLine(
                        "  " +
                        (i < this.arguments.Count ? "PARAM " : "LOCAL ") +
                        i + " = " +
                        this.body.localVariables[i].GetString(session) + ": " +
                        this.body.localVariables[i].type.GetString(session));
                }
            }

            Console.Out.WriteLine(
                "  RETURNS " + this.returnType.GetString(session));

            Console.Out.WriteLine();

            if (this.body != null)
                this.body.code.PrintDebugRecursive(session, 1, 1);
        }
    }
}
