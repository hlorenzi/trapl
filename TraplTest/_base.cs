using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TraplTest
{
    public class Base
    {
        protected delegate string EmbedDelegate(string input);


        protected EmbedDelegate MakeEmbedder(string before, string after)
        {
            return (input) => (before + input + after);
        }


        protected void ShouldPass(string sourceStr)
        {
            Assert.IsTrue(CompileAndTest(sourceStr, true));
        }


        protected void ShouldFail(string sourceStr)
        {
            Assert.IsFalse(CompileAndTest(sourceStr, false));
        }


        private bool CompileAndTest(string sourceStr, bool printErrors)
        {
            var session = new Trapl.Infrastructure.Session();
            session.AddUnit(Trapl.Infrastructure.Unit.MakeFromString(sourceStr));

            if (session.diagn.ContainsErrors())
                Assert.Inconclusive();

            session.Resolve();

            if (printErrors && session.diagn.ContainsErrors())
                session.diagn.PrintToConsole(session);

            return session.diagn.ContainsNoError();
        }
    }
}
