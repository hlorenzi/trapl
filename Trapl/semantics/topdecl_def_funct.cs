using System;
using System.Collections.Generic;
using Trapl.Diagnostics;
using Trapl.Infrastructure;


namespace Trapl.Semantics
{
    public class DefFunct : Def
    {
        public List<Variable> arguments = new List<Variable>();
        public Type returnType;

        public CodeBody body;


        public DefFunct(TopDecl topDecl) : base(topDecl)
        {

        }


        public void ResolveSignature(Infrastructure.Session session, TopDecl topDecl, Grammar.ASTNode defNode)
        {
            foreach (var argNode in defNode.EnumerateChildren())
            {
                if (argNode.kind != Grammar.ASTNodeKind.FunctArg)
                    continue;

                var arg = new Variable();
                arg.pathASTNode = argNode.Child(0).Child(0);
                arg.template = TemplateASTUtil.ResolveTemplateFromName(session, argNode.Child(0));
                arg.declSpan = argNode.Span();

                try
                {
                    //session.diagn.PushContext(new MessageContext("while resolving type '" + ASTTypeUtil.GetString(argNode.Child(1)) + "'", argNode.GetOriginalSpan()));
                    arg.type = TypeASTUtil.Resolve(session, argNode.Child(1));
                    this.arguments.Add(arg);
                }
                catch (Semantics.CheckException) { }
                finally { /*session.diagn.PopContext();*/ }
            }


            returnType = new TypeTuple();
            foreach (var retNode in defNode.EnumerateChildren())
            {
                if (retNode.kind != Grammar.ASTNodeKind.FunctReturnType)
                    continue;

                try
                {
                    //session.diagn.PushContext(new MessageContext("while resolving type '" + ASTTypeUtil.GetString(retNode.Child(0)) + "'", retNode.GetOriginalSpan()));
                    this.returnType = TypeASTUtil.Resolve(session, retNode.Child(0));
                }
                catch (Semantics.CheckException) { }
                finally { /*session.diagn.PopContext();*/ }
            }
        }


        public void ResolveBody(Infrastructure.Session session, TopDecl topDecl, Grammar.ASTNode defNode)
        {
            session.diagn.PushContext(new MessageContext("in funct '" + topDecl.GetString() + "'", topDecl.pathASTNode.Span()));
            try
            {
                body = CodeASTConverter.Convert(
                    session, 
                    defNode.ChildWithKind(Grammar.ASTNodeKind.FunctBody).Child(0),
                    new List<Variable>(this.arguments),
                    returnType);

                CodeTypeInferenceAnalyzer.Analyze(session, body);
                CodeTypeChecker.Check(session, body);
            }
            finally { session.diagn.PopContext(); }
        }


        public override void PrintToConsole(Session session, int indentLevel)
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

            Console.Out.WriteLine(
                "  RETURNS " + this.returnType.GetString(session));

            Console.Out.WriteLine();

            this.body.code.PrintDebugRecursive(session, 1, 1);

            /*var segments = new List<CodeSegment>();
            segments.Add(this.body);

            for (int i = 0; i < segments.Count; i++)
            {
                Console.Out.WriteLine("  === Segment " + i + " ===");
                foreach (var c in segments[i].nodes)
                {
                    Console.Out.WriteLine("    " + c.Name());
                }

                var goesToStr = "";
                for (int j = 0; j < segments[i].outwardPaths.Count; j++)
                {
                    int index = segments.FindIndex(seg => seg == segments[i].outwardPaths[j]);
                    if (index < 0)
                    {
                        segments.Add(segments[i].outwardPaths[j]);
                        index = segments.Count - 1;
                    }
                    goesToStr += index;
                    if (j < segments[i].outwardPaths.Count - 1)
                        goesToStr += ", ";
                }

                if (segments[i].outwardPaths.Count == 0)
                    goesToStr = "end";

                Console.Out.WriteLine("    -> Goes to " + goesToStr);
                Console.Out.WriteLine();
            }*/
        }
    }
}
