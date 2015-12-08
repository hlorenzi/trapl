using System;
using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Infrastructure
{
    public class DeclFunct : Decl
    {
        public List<StorageLocation> arguments = new List<StorageLocation>();
        public Type returnType;

        public Semantics.Routine routine;
        public int argumentNum;


        public DeclFunct() { }


        public override void Resolve(Infrastructure.Session session)
        {
            if (this.resolved)
                return;


            this.routine = new Semantics.Routine();


            var retRegisterIndex = this.routine.CreateRegister(new TypePlaceholder());
            var retRegister = this.routine.registers[retRegisterIndex];

            foreach (var retNode in this.defASTNode.EnumerateChildren())
            {
                if (retNode.kind != Grammar.ASTNodeKind.FunctReturnType)
                    continue;

                try
                {
                    retRegister.type =
                        Semantics.TypeUtil.ResolveFromAST(session, retNode.Child(0), true);
                    break;
                }
                catch (CheckException) { }
            }


            foreach (var argNode in this.defASTNode.EnumerateChildren())
            {
                if (argNode.kind != Grammar.ASTNodeKind.FunctArg)
                    continue;

                var argRegisterIndex = this.routine.CreateRegister(new TypePlaceholder());
                var argRegister = this.routine.registers[argRegisterIndex];

                var argBindingIndex = this.routine.CreateBinding(argRegisterIndex);
                var argBinding = this.routine.bindings[argBindingIndex];

                this.argumentNum++;

                argBinding.name = new Name(
                    argNode.Child(0).Span(),
                    argNode.Child(0).Child(0),
                    Semantics.TemplateUtil.ResolveFromNameAST(session, argNode.Child(0), true));
                argBinding.declSpan = argNode.Span();

                try
                {
                    argRegister.type =
                        Semantics.TypeUtil.ResolveFromAST(session, argNode.Child(1), true);
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
                Semantics.RoutineASTParser.Parse(
                    session, 
                    this.routine,
                    this.defASTNode.ChildWithKind(Grammar.ASTNodeKind.FunctBody).Child(0));

                Semantics.RoutineTypeInferencer.DoInference(session, routine);
                //Semantics.CodeTypeChecker.Check(session, routine);
            }
            finally { session.diagn.PopContext(); }
        }


        public override void PrintToConsole(Session session, int indentLevel)
        {
            Console.Out.WriteLine("ARGUMENT NUM = " + this.argumentNum);

            Console.Out.WriteLine();

            if (this.routine != null)
                this.routine.Print(session);

            Console.Out.WriteLine();
        }
    }
}
