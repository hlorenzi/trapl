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


        protected Trapl.Infrastructure.Session ParseTokens(string sourceStr)
        {
            var session = new Trapl.Infrastructure.Session();
            Trapl.Grammar.Tokenizer.Tokenize(session, Trapl.Infrastructure.Unit.MakeFromString(sourceStr));
            return session;
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
            var functs = session.functDecls.GetDeclsClone(
                Trapl.Grammar.ASTParser.ParseName(session,
                    Trapl.Grammar.Tokenizer.Tokenize(session,
                        Trapl.Infrastructure.Unit.MakeFromString("test"))).Child(0));

            if (functs.Count != 1)
                Assert.Inconclusive();

            var varNameASTNode = Trapl.Grammar.ASTParser.ParseName(session,
                    Trapl.Grammar.Tokenizer.Tokenize(session,
                        Trapl.Infrastructure.Unit.MakeFromString(varName)));

            var typeASTNode = Trapl.Grammar.ASTParser.ParseType(session,
                    Trapl.Grammar.Tokenizer.Tokenize(session,
                        Trapl.Infrastructure.Unit.MakeFromString(typeName)));

            var local = functs[0].body.localVariables.Find(
                v => v.name.Compare(varNameASTNode.Child(0), new Trapl.Semantics.Template()));
            if (local == null)
                return true;

            var type = Trapl.Semantics.TypeASTUtil.Resolve(session, typeASTNode, true);

            return local.type.IsSame(type);
        }
    }
}
