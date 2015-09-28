using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TraplTest
{
    [TestClass]
    public class SyntaxTests
    {
        [TestMethod]
        public void TestTopDeclSyntax()
        {
            ShouldPass("");

            ShouldPass("Test { }");
            ShouldPass("Test { x: Int8 }");
            ShouldPass("Test { x: Int8, y: Int8 }");
            ShouldPass("Test { x: Int8, y: Int16, }");
            ShouldPass("Test1 { x: Test2 } Test2 { x: Int8 }");
            ShouldPass("Test1 { x: Int8 } Test2 { x: Test1 }");

            ShouldFail("{ }");
            ShouldFail("Test");
            ShouldFail("Test { ");
            ShouldFail("Test: { }");
            ShouldFail("123 { }");
            ShouldFail("Test1 { }; Test2 { }");
            ShouldFail("Test { x: Int8 y: Int8 }");
        }


        private void ShouldPass(string sourceStr)
        {
            var session = new Trapl.Interface.Session();
            var src = Trapl.Interface.SourceCode.MakeFromString(sourceStr);

            var tokens = Trapl.Grammar.Tokenizer.Tokenize(session, src);
            var ast = Trapl.Grammar.ASTParser.Parse(session, tokens);

            Assert.IsTrue(session.diagn.ContainsNoError());
        }


        private void ShouldFail(string sourceStr)
        {
            var session = new Trapl.Interface.Session();
            var src = Trapl.Interface.SourceCode.MakeFromString(sourceStr);

            var tokens = Trapl.Grammar.Tokenizer.Tokenize(session, src);
            var ast = Trapl.Grammar.ASTParser.Parse(session, tokens);

            Assert.IsTrue(session.diagn.ContainsErrors());
        }
    }
}
