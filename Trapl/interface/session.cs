using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trapl.Interface
{
    public class Session
    {
        public static void Compile(SourceCode src)
        {
            var session = new Session();
            session.diagn = new Diagnostics.Collection();

            var tokenCollection = Grammar.Tokenizer.Tokenize(session, src);
            //tokenCollection.PrintDebug(src);

            var ast = Grammar.ASTParser.Parse(session, tokenCollection, src);
            //ast.PrintDebug(src);

            Semantics.DefinitionGatherer.Gather(session, ast, src);
            Semantics.TemplateConsistencyChecker.Check(session);

            if (session.diagn.HasNoError())
            {
                // FIXME! Rewrite this loop for efficiency.
                while (true)
                {
                    whileStart:
                    for (int i = 0; i < session.structDefs.Count; i++)
                    {
                        for (int j = 0; j < session.structDefs[i].defs.Count; j++)
                        {
                            var st = session.structDefs[i].defs[j];
                            if (!st.templateList.IsGeneric() && !st.resolved)
                            {
                                Semantics.ResolverStruct.Resolve(session, new Semantics.TemplateSubstitution(), st);
                                goto whileStart;
                            }
                        }
                    }
                    break;
                }
            }

            session.PrintDefs();
            session.diagn.PrintToConsole();
        }


        public Diagnostics.Collection diagn;
        public List<Semantics.Definition<Semantics.DefinitionStruct>> structDefs = new List<Semantics.Definition<Semantics.DefinitionStruct>>();
        public List<Semantics.Definition<Semantics.DefinitionFunct>> functDefs = new List<Semantics.Definition<Semantics.DefinitionFunct>>();


        public void Merge(Session other)
        {
            foreach (var otherDef in other.functDefs)
            {
                var thisDef = this.functDefs.Find(def => def.fullName == otherDef.fullName);
                if (thisDef != null)
                    thisDef.defs.AddRange(otherDef.defs);
                else
                    this.functDefs.Add(otherDef);
            }

            foreach (var otherDef in other.structDefs)
            {
                var thisDef = this.structDefs.Find(def => def.fullName == otherDef.fullName);
                if (thisDef != null)
                    thisDef.defs.AddRange(otherDef.defs);
                else
                    this.structDefs.Add(otherDef);
            }
        }


        public void PrintDefs()
        {
            foreach (var def in structDefs)
            {
                Console.Out.WriteLine("STRUCT " + def.fullName + " (" + def.defs.Count + ")");
                foreach (var st in def.defs)
                {
                    Console.Out.Write("  ::" + st.templateList.GetName(this));
                    Console.Out.WriteLine(st.synthesized ? " synthesized" : "");
                    if (!st.resolved)
                        Console.Out.WriteLine("    Unresolved");
                    else
                    {
                        foreach (var member in st.members)
                        {
                            Console.Out.WriteLine("    " + member.name + ": " + Semantics.ResolverType.GetName(this, member.type));
                        }
                    }
                    Console.Out.WriteLine();
                }
                Console.Out.WriteLine();
            }
            foreach (var def in functDefs)
            {
                Console.Out.WriteLine("FUNCT " + def.fullName.PadRight(20) + " (" + def.defs.Count + ")");
                foreach (var f in def.defs)
                    Console.Out.WriteLine("  ::" + f.templateList.GetName(this));
            }
        }
    }
}
