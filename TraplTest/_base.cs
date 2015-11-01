using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TraplTest
{
    public class Base
    {
        protected void ForEach<T>(System.Action<T> func, params T[] args)
        {
            foreach (var arg in args)
            {
                func(arg);
            }
        }


        protected void ContainsNoError(Trapl.Infrastructure.Session session)
        {
            Assert.IsTrue(session.diagn.ContainsNoError());
        }


        protected void ContainsErrors(Trapl.Infrastructure.Session session)
        {
            Assert.IsTrue(session.diagn.ContainsErrors());
        }


        protected Trapl.Infrastructure.Session ParseGrammar(string sourceStr)
        {
            var session = new Trapl.Infrastructure.Session();
            session.AddUnit(Trapl.Infrastructure.Unit.MakeFromString(sourceStr));
            return session;
        }


        protected Trapl.Infrastructure.Session Compile(string sourceStr)
        {
            var session = new Trapl.Infrastructure.Session();
            session.AddUnit(Trapl.Infrastructure.Unit.MakeFromString(sourceStr));

            if (session.diagn.ContainsErrors())
                Assert.Inconclusive();

            session.Resolve();
            return session;
        }


        protected bool CheckLocalType(Trapl.Infrastructure.Session session, string varName, string typeName)
        {
            var functs = Trapl.Semantics.TopDeclFinder.FindFunctsNamed(session,
                Trapl.Grammar.ASTParser.ParseName(session,
                    Trapl.Grammar.Tokenizer.Tokenize(session,
                        Trapl.Infrastructure.Unit.MakeFromString("test"))));

            if (functs.Count != 1)
                Assert.Inconclusive();

            var varNameASTNode = Trapl.Grammar.ASTParser.ParseName(session,
                    Trapl.Grammar.Tokenizer.Tokenize(session,
                        Trapl.Infrastructure.Unit.MakeFromString(varName)));

            var typeASTNode = Trapl.Grammar.ASTParser.ParseType(session,
                    Trapl.Grammar.Tokenizer.Tokenize(session,
                        Trapl.Infrastructure.Unit.MakeFromString(typeName)));

            var local = ((Trapl.Semantics.DefFunct)functs[0].def).body.localVariables.Find(v => Trapl.Semantics.PathASTUtil.Compare(v.pathASTNode, varNameASTNode.Child(0)));
            if (local == null)
                return true;

            var type = Trapl.Semantics.TypeASTUtil.Resolve(session, typeASTNode);

            return local.type.IsSame(type);
        }
    }
}
