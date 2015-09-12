using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TraplTest
{
    [TestClass]
    public class PatternMatchingTests
    {
        [TestMethod]
        public void TestPatternMatching()
        {
            ShouldPass("<>",                    "<>");
            ShouldPass("<A>",                   "<A>");
            ShouldPass("<A, A>",                "<A, A>");
            ShouldPass("<A, B>",                "<A, B>");
            ShouldPass("<A::<B>>",              "<A::<B>>");
            ShouldPass("<A::<B::<C>>>",         "<A::<B::<C>>>");
            ShouldPass("<A::<B>, C::<D::<E>>>", "<A::<B>, C::<D::<E>>>");
            ShouldPass("<A::<B, C, D>>",        "<A::<B, C, D>>");
            ShouldPass("<A::<B, C::<D>, E>>",   "<A::<B, C::<D>, E>>");

            ShouldPass("<&A>",         "<&A>");
            ShouldPass("<&&A>",        "<&&A>");
            ShouldPass("<&&&A>",       "<&&&A>");
            ShouldPass("<&A, &B>",     "<&A, &B>");
            ShouldPass("<&A, &&B>",    "<&A, &&B>");
            ShouldPass("<&A, &&&B>",   "<&A, &&&B>");
            ShouldPass("<&&&A, &&&B>", "<&&&A, &&&B>");
            ShouldPass("<A::<&B>>",    "<A::<&B>>");
            ShouldPass("<&A::<B>>",    "<&A::<B>>");
            ShouldPass("<&A::<&B>>",   "<&A::<&B>>");
            ShouldPass("<&&A::<&&B>>", "<&&A::<&&B>>");

            ShouldPass("<gen T>",                   "<A>");
            ShouldPass("<gen A>",                   "<A>");
            ShouldPass("<gen T, gen U>",            "<A, A>");
            ShouldPass("<gen T, gen U>",            "<A, B>");
            ShouldPass("<gen T, gen U, gen V>",     "<A, A, A>");
            ShouldPass("<gen T, gen U, gen V>",     "<A, B, C>");
            ShouldPass("<gen T>",                   "<A::<B>>");
            ShouldPass("<gen T>",                   "<A::<B, C, D>>");
            ShouldPass("<gen T, gen U>",            "<A::<B, C, D>, E::<F, G::<H, I>, J>>");
            ShouldPass("<gen T, gen U>",            "<A::<B>, C::<D::<E, F, G>, H>>");
            ShouldPass("<gen T, gen U, gen V>",     "<A::<B>, C::<D::<E>>, F::<G::<H::<I>, J, K>>>");
            ShouldPass("<gen T::<gen U>>",          "<A::<B>>");
            ShouldPass("<gen T::<gen U>>",          "<A::<B::<C, D, E>>>");
            ShouldPass("<gen T::<gen U>>",          "<A::<B::<C::<D>>>>");
            ShouldPass("<gen T::<gen U>, gen V>",   "<A::<B>, C>");
            ShouldPass("<gen T::<gen U>, gen V>",   "<A::<B>, C::<D>>");
            ShouldPass("<gen T::<gen U>, gen V>",   "<A::<B::<C::<D>>>, E>");
            ShouldPass("<gen T::<gen U>, gen V>",   "<A::<B::<C::<D>>>, E::<F, G::<H>>>");
            ShouldPass("<gen T::<gen U::<gen V>>>", "<A::<B::<C>>>");
            ShouldPass("<gen T::<gen U::<gen V>>>", "<A::<B::<C::<D, E, F>>>>");

            ShouldPass("<gen T...>",               "<>");
            ShouldPass("<gen T...>",               "<A>");
            ShouldPass("<gen T...>",               "<A, B>");
            ShouldPass("<gen T...>",               "<A, B, C>");
            ShouldPass("<gen T...>",               "<A, B, C, D, E, F, G, H, I>");
            ShouldPass("<gen T...>",               "<A, B, C::<D>, E, F::<G::<H, I>>, J>");
            ShouldPass("<gen T, gen U...>",        "<A>");
            ShouldPass("<gen T, gen U...>",        "<A, B>");
            ShouldPass("<gen T, gen U...>",        "<A, B, C>");
            ShouldPass("<gen T, gen U...>",        "<A, B, C, D, E, F, G, H, I>");
            ShouldPass("<gen T, gen U, gen V...>", "<A, B>");
            ShouldPass("<gen T, gen U, gen V...>", "<A, B, C>");
            ShouldPass("<gen T, gen U, gen V...>", "<A, B, C, D, E, F, G, H, I>");

            ShouldPass("<gen T::<gen U...>...>", "<>");
            ShouldPass("<gen T::<gen U...>...>", "<A>");
            ShouldPass("<gen T::<gen U...>...>", "<A, B>");
            ShouldPass("<gen T::<gen U...>...>", "<A::<B>, C>");
            ShouldPass("<gen T::<gen U...>...>", "<A, B::<C>>");
            ShouldPass("<gen T::<gen U...>...>", "<A::<B>, C::<D>>");
            ShouldPass("<gen T::<gen U...>...>", "<A::<B, C, D>, E::<F, G>>");
            ShouldPass("<gen T::<gen U...>...>", "<A::<B, C, D>, E::<F, G>, H::<I::<J, K>, L>>");


            ShouldFail("<>",       "<A>");
            ShouldFail("<A>",      "<>");
            ShouldFail("<A>",      "<B>");
            ShouldFail("<A>",      "<A::<B>>");
            ShouldFail("<A>",      "<B, A>");
            ShouldFail("<>",       "<A, A>");
            ShouldFail("<A, A>",   "<>");
            ShouldFail("<A>",      "<B, B>");
            ShouldFail("<A, B>",   "<A>");
            ShouldFail("<A, A>",   "<B, B>");
            ShouldFail("<A, A>",   "<A, B>");
            ShouldFail("<A, B>",   "<B, B>");
            ShouldFail("<A::<B>>", "<A>");
            ShouldFail("<A::<B>>", "<A, B>");
            ShouldFail("<A::<B>>", "<A::<B>, C>");
            ShouldFail("<A::<B>>", "<C, A::<B>>");

            ShouldFail("<>",         "<&A>");
            ShouldFail("<&A>",       "<>");
            ShouldFail("<&A>",       "<A>");
            ShouldFail("<A>",        "<&A>");
            ShouldFail("<>",         "<&&A>");
            ShouldFail("<&&A>",      "<>");
            ShouldFail("<A>",        "<&&A>");
            ShouldFail("<&&A>",      "<A>");
            ShouldFail("<&A>",       "<&&A>");
            ShouldFail("<&&A>",      "<&A>");
            ShouldFail("<&&A>",      "<&&&A>");
            ShouldFail("<A::<&B>>",  "<A::<B>>");
            ShouldFail("<A::<&B>>",  "<&A::<B>>");
            ShouldFail("<A::<&B>>",  "<A::<&&B>>");
            ShouldFail("<A::<&B>>",  "<&A::<&B>>");
            ShouldFail("<A::<&B>>",  "<&A::<&&B>>");
            ShouldFail("<&A::<B>>",  "<A::<B>>");
            ShouldFail("<&A::<B>>",  "<A::<&B>>");
            ShouldFail("<&A::<B>>",  "<&&A::<B>>");
            ShouldFail("<&A::<B>>",  "<&A::<&B>>");
            ShouldFail("<&A::<&B>>", "<A::<B>>");
            ShouldFail("<&A::<&B>>", "<&A::<B>>");
            ShouldFail("<&A::<&B>>", "<A::<&B>>");
            ShouldFail("<&A::<&B>>", "<&&A::<&&B>>");

            ShouldFail("<gen T>",                   "<>");
            ShouldFail("<gen T>",                   "<A, A>");
            ShouldFail("<gen T>",                   "<A, B>");
            ShouldFail("<gen T, gen U>",            "<>");
            ShouldFail("<gen T, gen U>",            "<A>");
            ShouldFail("<gen T, gen U>",            "<A, B, C>");
            ShouldFail("<gen T, gen U>",            "<A::<B>>");
            ShouldFail("<gen T, gen U>",            "<A::<B, C>>");
            ShouldFail("<gen T::<gen U>>",          "<A::<B>, C>");
            ShouldFail("<gen T::<gen U>>",          "<A::<B, C>>");
            ShouldFail("<gen T::<gen U::<gen V>>>", "<A>");
            ShouldFail("<gen T::<gen U::<gen V>>>", "<A::<B>>");
            ShouldFail("<gen T::<gen U::<gen V>>>", "<A::<B, C>>");
            ShouldFail("<gen T::<gen U::<gen V>>>", "<A, B, C>");
            ShouldFail("<gen T::<gen U::<gen V>>>", "<A, B::<C::<D>>>");

            ShouldFail("<gen T, gen U...>",        "<>");
            ShouldFail("<gen T, gen U, gen V...>", "<>");
            ShouldFail("<gen T, gen U, gen V...>", "<A>");
        }


        public void ShouldPass(string genericPatternStr, string concretePatternStr)
        {
            Assert.IsTrue(CompileAndTest(genericPatternStr, concretePatternStr));
        }


        public void ShouldFail(string genericPatternStr, string concretePatternStr)
        {
            Assert.IsFalse(CompileAndTest(genericPatternStr, concretePatternStr));
        }


        public bool CompileAndTest(string genericPatternStr, string concretePatternStr)
        {
            var session = new Trapl.Interface.Session();
            session.diagn = new Trapl.Diagnostics.Collection();

            var genericSrc = Trapl.Interface.SourceCode.MakeFromString(genericPatternStr);
            var concreteSrc = Trapl.Interface.SourceCode.MakeFromString(concretePatternStr);

            var genericTokens = Trapl.Grammar.Tokenizer.Tokenize(session, genericSrc);
            var concreteTokens = Trapl.Grammar.Tokenizer.Tokenize(session, concreteSrc);

            var genericAST = Trapl.Grammar.ASTParser.ParsePattern(session, genericTokens);
            var concreteAST = Trapl.Grammar.ASTParser.ParsePattern(session, concreteTokens);

            if (session.diagn.HasErrors())
                Assert.Inconclusive();

            var genericPattern = new Trapl.Semantics.DeclPattern(genericSrc, genericAST);
            var concretePattern = new Trapl.Semantics.DeclPattern(concreteSrc, concreteAST);
            var substitutionList = new Trapl.Semantics.DeclPatternSubstitution();

            return Trapl.Semantics.ASTPatternMatcher.Match(substitutionList, genericPattern, concretePattern);
        }
    }
}
