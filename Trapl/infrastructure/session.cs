using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trapl.Infrastructure
{
    public class Session
    {
        public Session()
        {
            this.diagn = new Diagnostics.Collection();
        }


        public static void Compile(Unit unit)
        {
            var session = new Session();
            session.AddUnit(unit);
            session.Resolve();
        }


        public void AddUnit(Unit unit)
        {
            var tokenCollection = Grammar.Tokenizer.Tokenize(this, unit);
            var declNodes = Grammar.ASTParser.Parse(this, tokenCollection);

            foreach (var declNode in declNodes)
                Semantics.DeclASTConverter.AddToDecls(this, declNode);
        }


        public void Resolve()
        {
            if (this.diagn.ContainsErrors()) return;
            this.structDecls.ForEach(decl =>
            {
                try { decl.ResolveTemplate(this); }
                catch (Semantics.CheckException) { }
            });

            if (this.diagn.ContainsErrors()) return;
            this.functDecls.ForEach(decl =>
            {
                try { decl.ResolveTemplate(this); }
                catch (Semantics.CheckException) { }
            });

            if (this.diagn.ContainsErrors()) return;
            this.structDecls.ForEach(decl => decl.Resolve(this));

            if (this.diagn.ContainsErrors()) return;
            this.functDecls.ForEach(decl => decl.Resolve(this));

            if (this.diagn.ContainsErrors()) return;
            this.functDecls.ForEach(decl => decl.ResolveBody(this));
        }


        public Diagnostics.Collection diagn = new Diagnostics.Collection();

        public DeclList<Semantics.DeclStruct> structDecls = new DeclList<Semantics.DeclStruct>();
        public DeclList<Semantics.DeclFunct> functDecls = new DeclList<Semantics.DeclFunct>();


        public void PrintDefs()
        {
            foreach (var decl in this.structDecls.Enumerate())
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Out.WriteLine("STRUCT " + decl.GetString(this));
                Console.ResetColor();

                if (!decl.resolved)
                    Console.Out.WriteLine("  unresolved");
                else
                    decl.PrintToConsole(this, 1);

                Console.Out.WriteLine();
            }


            foreach (var decl in this.functDecls.Enumerate())
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Out.WriteLine("FUNCT " + decl.GetString(this));
                Console.ResetColor();

                if (!decl.resolved)
                    Console.Out.WriteLine("  unresolved");
                else
                    decl.PrintToConsole(this, 1);

                Console.Out.WriteLine();
            }
        }
    }
}
