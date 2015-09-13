using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TraplTest
{
    [TestClass]
    public class PatternSubstitutionTests
    {
        [TestMethod]
        public void TestPatternSubstitution()
        {
            ShouldPass("<>", "<>",
                "A", "A",
                "A::<B>", "A::<B>");

            ShouldPass("<gen T>", "<A>",
                "gen T", "A",
                "&gen T", "&A",
                "&&gen T", "&&A",
                "gen T::<gen T>", "A::<A>",
                "gen T::<&gen T>", "A::<&A>",
                "&gen T::<gen T>", "&A::<A>",
                "&gen T::<&gen T>", "&A::<&A>",
                "gen T::<B>", "A::<B>",
                "gen T::<&B>", "A::<&B>",
                "&gen T::<B>", "&A::<B>",
                "&gen T::<&B>", "&A::<&B>",
                "gen T::<B, C>", "A::<B, C>",
                "gen T::<B::<C>>", "A::<B::<C>>",
                "B::<gen T>", "B::<A>",
                "B::<B::<gen T>>", "B::<B::<A>>",
                "B::<C, gen T>", "B::<C, A>");

            ShouldPass("<gen T>", "<&A>",
                "gen T", "&A",
                "&gen T", "&&A",
                "gen T::<gen T>", "&A::<&A>");

            ShouldPass("<&gen T>", "<&A>",
                "gen T", "A",
                "&gen T", "&A",
                "gen T::<gen T>", "A::<A>");

            ShouldPass("<&gen T>", "<&&A>",
                "gen T", "&A",
                "&gen T", "&&A",
                "gen T::<gen T>", "&A::<&A>");

            ShouldPass("<gen T>", "<A::<B>>",
                "gen T", "A::<B>",
                "C::<gen T>", "C::<A::<B>>",
                "C::<D, gen T>", "C::<D, A::<B>>");

            ShouldPass("<gen T>", "<A::<&B>>",
                "gen T", "A::<&B>",
                "C::<gen T>", "C::<A::<&B>>",
                "C::<D, gen T>", "C::<D, A::<&B>>");

            ShouldPass("<gen T::<gen U>>", "<A::<B>>",
                "gen T", "A",
                "gen U", "B",
                "gen T::<gen U>", "A::<B>",
                "gen U::<gen T>", "B::<A>",
                "gen T::<gen T>", "A::<A>",
                "gen U::<gen U>", "B::<B>",
                "gen T::<gen T::<gen U>>", "A::<A::<B>>",
                "gen T::<gen U::<gen U>>", "A::<B::<B>>",
                "gen U::<gen U::<gen U>>", "B::<B::<B>>",
                "gen U::<gen T::<gen U>>", "B::<A::<B>>",
                "gen T::<gen U::<gen T>>", "A::<B::<A>>",
                "gen T::<gen U, gen T>", "A::<B, A>");


            ShouldFail("<>", "<>",
                "A", "B",
                "A::<B>", "A::<C>",
                "gen T", "Irrelevant");

            ShouldFail("<gen T>", "<A>",
                "gen T", "B",
                "gen T::<gen T>", "A::<B>",
                "gen T::<gen T>", "B::<A>",
                "gen T::<gen T>", "A::<A::<A>>",
                "gen T::<gen T>", "A::<A::<B>>",
                "gen T::<B>", "A::<A>",
                "gen T::<B>", "B::<A>",
                "B::<gen T>", "A::<A>");

            ShouldFail("<gen T>", "<A::<B>>",
                "gen T", "A",
                "gen T", "B",
                "gen T::<B>", "A::<B>",
                "gen T::<B>", "A::<B::<B>>",
                "gen T::<gen T>", "A::<B>",
                "gen T::<gen T>", "A::<A::<B>>",
                "C::<gen T>", "C",
                "C::<gen T>", "C::<A>",
                "C::<gen T>", "C::<A, B>",
                "C::<D, gen T>", "C::<A::<B>>");

            ShouldFail("<gen T>", "<&A>",
                "gen T", "A");

            ShouldFail("<&gen T>", "<&A>",
                "gen T", "&A");

            ShouldFail("<&gen T>", "<&&A>",
                "gen T", "&&A");

            ShouldFail("<gen T::<gen U>>", "<A::<B>>",
                "gen T", "B",
                "gen U", "A",
                "gen T::<gen U>", "A",
                "gen T::<gen U>", "B",
                "gen T::<gen U>", "A::<A>",
                "gen T::<gen U>", "B::<B>",
                "gen T::<gen U>", "B::<A>");
        }


        public void ShouldPass(string genericPatternStr, string concretePatternStr, params string[] substStrs)
        {
            for (int i = 0; i < substStrs.Length; i += 2)
            {
                var toSubstStr = substStrs[i];
                var correctStr = substStrs[i + 1];
                Assert.IsTrue(CompileAndTest(genericPatternStr, concretePatternStr, toSubstStr, correctStr),
                    "Failed at test case: '" + genericPatternStr + "', '" + concretePatternStr + "', " +
                    "'" + toSubstStr + "', '" + correctStr + "'");
            }
        }


        public void ShouldFail(string genericPatternStr, string concretePatternStr, params string[] substStrs)
        {
            for (int i = 0; i < substStrs.Length; i += 2)
            {
                var toSubstStr = substStrs[i];
                var correctStr = substStrs[i + 1];
                Assert.IsFalse(CompileAndTest(genericPatternStr, concretePatternStr, toSubstStr, correctStr),
                    "Failed at test case: '" + genericPatternStr + "', '" + concretePatternStr + "', " +
                    "'" + toSubstStr + "', '" + correctStr + "'");
            }
        }


        public bool CompileAndTest(string genericPatternStr, string concretePatternStr, string toBeSubstitutedStr, string correctSubstitutionStr)
        {
            var session = new Trapl.Interface.Session();
            session.diagn = new Trapl.Diagnostics.Collection();

            var genericSrc = Trapl.Interface.SourceCode.MakeFromString(genericPatternStr);
            var concreteSrc = Trapl.Interface.SourceCode.MakeFromString(concretePatternStr);
            var toSubstSrc = Trapl.Interface.SourceCode.MakeFromString(toBeSubstitutedStr);
            var correctSrc = Trapl.Interface.SourceCode.MakeFromString(correctSubstitutionStr);

            var genericTokens = Trapl.Grammar.Tokenizer.Tokenize(session, genericSrc);
            var concreteTokens = Trapl.Grammar.Tokenizer.Tokenize(session, concreteSrc);
            var toSubstTokens = Trapl.Grammar.Tokenizer.Tokenize(session, toSubstSrc);
            var correctTokens = Trapl.Grammar.Tokenizer.Tokenize(session, correctSrc);

            var genericAST = Trapl.Grammar.ASTParser.ParsePattern(session, genericTokens);
            var concreteAST = Trapl.Grammar.ASTParser.ParsePattern(session, concreteTokens);
            var toSubstAST = Trapl.Grammar.ASTParser.ParseType(session, toSubstTokens);
            var correctAST = Trapl.Grammar.ASTParser.ParseType(session, correctTokens);

            if (session.diagn.HasErrors())
                Assert.Inconclusive();

            var repl = Trapl.Semantics.ASTPatternMatcher.Match(genericAST, concreteAST);
            if (repl == null)
                Assert.Inconclusive();

            Trapl.Grammar.ASTNode substAST = null;

            try
            {
                substAST = Trapl.Semantics.ASTPatternReplacer.CloneReplaced(session, toSubstAST, repl);
            }
            catch (Trapl.Semantics.CheckException)
            {
                session.diagn.PrintToConsole();
                return false;
            }

            session.diagn.PrintToConsole();
            if (session.diagn.HasErrors())
                return false;

            return Compare(substAST, correctAST);
        }


        public bool Compare(Trapl.Grammar.ASTNode ast1, Trapl.Grammar.ASTNode ast2)
        {
            if (ast1.kind != ast2.kind)
                return false;

            if (ast1.kind == Trapl.Grammar.ASTNodeKind.Identifier &&
                ast1.GetExcerpt() != ast2.GetExcerpt())
                return false;

            if (ast1.ChildNumber() != ast2.ChildNumber())
                return false;

            for (int i = 0; i < ast1.ChildNumber(); i++)
                if (!Compare(ast1.Child(i), ast2.Child(i)))
                    return false;

            return true;
        }
    }
}
