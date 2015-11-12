using System;
using System.Collections.Generic;
using Trapl.Diagnostics;
using Trapl.Infrastructure;


namespace Trapl.Semantics
{
    public class DeclFunct : Decl
    {
        public List<Variable> arguments = new List<Variable>();
        public Type returnType;

        public CodeBody body;


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
                    UtilASTTemplate.ResolveTemplateFromName(session, argNode.Child(0), true));
                arg.declSpan = argNode.Span();

                try
                {
                    arg.type = TypeASTUtil.Resolve(session, argNode.Child(1), true);
                    this.arguments.Add(arg);
                }
                catch (Semantics.CheckException) { }
            }


            returnType = new TypeTuple();
            foreach (var retNode in this.defASTNode.EnumerateChildren())
            {
                if (retNode.kind != Grammar.ASTNodeKind.FunctReturnType)
                    continue;

                try
                {
                    this.returnType = TypeASTUtil.Resolve(session, retNode.Child(0), true);
                }
                catch (Semantics.CheckException) { }
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
                body = CodeASTConverter.Convert(
                    session, 
                    this.defASTNode.ChildWithKind(Grammar.ASTNodeKind.FunctBody).Child(0),
                    new List<Variable>(this.arguments),
                    returnType);

                CodeTypeInferenceAnalyzer.Analyze(session, body);
                CodeTypeChecker.Check(session, body);
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
