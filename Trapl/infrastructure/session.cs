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
            var topDeclNodes = Grammar.ASTParser.Parse(this, tokenCollection);

            foreach (var topDeclNode in topDeclNodes)
            {
                this.topDecls.Add(Semantics.TopDeclASTConverter.Convert(this, topDeclNode));
            }
        }


        public void Resolve()
        {
            var topDeclsToResolve = new List<Semantics.TopDecl>(this.topDecls);
            foreach (var topDecl in topDeclsToResolve)
            {
                topDecl.ResolveTemplate(this);
            }

            topDeclsToResolve = new List<Semantics.TopDecl>(this.topDecls);
            foreach (var topDecl in topDeclsToResolve)
            {
                topDecl.Resolve(this);
            }

            topDeclsToResolve = new List<Semantics.TopDecl>(this.topDecls);
            foreach (var topDecl in topDeclsToResolve)
            {
                topDecl.ResolveBody(this);
            }
        }


        public Diagnostics.Collection diagn = new Diagnostics.Collection();
        public List<Semantics.TopDecl> topDecls = new List<Semantics.TopDecl>();


        public void PrintDefs()
        {
            foreach (var topDecl in this.topDecls)
            {
                Console.ForegroundColor = ConsoleColor.White;
                    //(topDecl.synthesized ? ConsoleColor.Cyan :
                    //(topDecl.generic ? ConsoleColor.Yellow : ConsoleColor.White));
                Console.Out.WriteLine("TOPDECL " +
                    //(topDecl.synthesized ? "SYNTHESIZED TOPDECL " :
                    //(topDecl.generic ? "GENERIC TOPDECL " : "TOPDECL ")) +
                    topDecl.GetString());
                Console.ResetColor();

                /*if (topDecl.generic)
                    Console.Out.WriteLine("  generic, unresolved");
                else*/ if (topDecl.def == null)
                    Console.Out.WriteLine("  unresolved");
                else
                    topDecl.def.PrintToConsole(this, 1);

                Console.Out.WriteLine();
            }
        }
    }
}
