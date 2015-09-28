using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trapl.Interface
{
    public class Session
    {
        public Session()
        {
            this.diagn = new Diagnostics.Collection();
        }


        public static void Compile(SourceCode src)
        {
            var session = new Session();
            session.diagn = new Diagnostics.Collection();

            var tokenCollection = Grammar.Tokenizer.Tokenize(session, src);
            var ast = Grammar.ASTParser.Parse(session, tokenCollection);

            //foreach (var node in ast.topDecls)
            //    Grammar.AST.PrintDebug(node, 0);

            if (session.diagn.ContainsNoError())
                Semantics.CheckTopDecl.Check(session, ast, src);

            if (session.diagn.ContainsNoError())
            {
                var topDeclClones = new List<Semantics.TopDecl>(session.topDecls);
                foreach (var topDecl in topDeclClones)
                {
                    try { topDecl.Resolve(session);  }
                    catch (Semantics.CheckException) { }
                }
            }

            if (session.diagn.ContainsNoError())
            {
                var topDeclClones = new List<Semantics.TopDecl>(session.topDecls);
                foreach (var topDecl in topDeclClones)
                {
                    try { topDecl.ResolveBody(session); }
                    catch (Semantics.CheckException) { }
                }
            }

            session.PrintDefs();
            session.diagn.PrintToConsole(session);

            if (session.diagn.ContainsNoError())
            {
                Console.WriteLine();
                Console.WriteLine("========== CODEGEN ============");
                Console.WriteLine();
                Console.WriteLine(Codegen.CGenerator.Generate(session));
            }
        }


        public Diagnostics.Collection diagn;
        public List<Semantics.TopDecl> topDecls = new List<Semantics.TopDecl>();


        public void PrintDefs()
        {
            foreach (var topDecl in this.topDecls)
            {
                if (topDecl.primitive)
                    continue;

                Console.ForegroundColor = 
                    (topDecl.synthesized ? ConsoleColor.Cyan :
                    (topDecl.generic ? ConsoleColor.Yellow : ConsoleColor.White));
                Console.Out.WriteLine(
                    (topDecl.synthesized ? "SYNTHESIZED TOPDECL " :
                    (topDecl.generic ? "GENERIC TOPDECL " : "TOPDECL ")) +
                    topDecl.GetString());
                Console.ResetColor();

                if (topDecl.generic)
                    Console.Out.WriteLine("  generic, unresolved");
                else if (topDecl.def == null)
                    Console.Out.WriteLine("  unresolved");
                else
                    topDecl.def.PrintToConsole(this, 1);

                Console.Out.WriteLine();
            }
        }
    }
}
