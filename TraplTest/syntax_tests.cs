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

            ShouldPass("Test: struct { }");
            ShouldPass("Test: struct { x: Int8 }");
            ShouldPass("Test: struct { x: Int8, y: Int8 }");
            ShouldPass("Test: struct { x: Int8, y: Int16, }");
            ShouldPass("Test1: struct { x: Test2 } Test2: struct { x: Int8 }");
            ShouldPass("Test1: struct { x: Int8 } Test2: struct { x: Test1 }");

            ShouldFail("{ }");
            ShouldFail("test");
            ShouldFail("test { }");
            ShouldFail("test:");
            ShouldFail("test: { }");
            ShouldFail("test: test { }");
            ShouldFail("test: 123 { }");
            ShouldFail("123: struct { }");
            ShouldFail(": struct { }");
            ShouldFail("test: funct() { }; Test2: struct { }");

            ShouldFail("struct { }");
            ShouldFail("Test struct { }");
            ShouldFail("Test: struct");
            ShouldFail("Test: struct {");
            ShouldFail("Test: struct { x: Int8 y: Int8 }");

            ShouldFail("funct() { }");
            ShouldFail("test funct() { }");
            ShouldFail("test: funct");
            ShouldFail("test: funct(");
            ShouldFail("test: funct { }");
            ShouldFail("test: funct()");
            ShouldFail("test: funct() {");
            ShouldFail("test: funct( -> { }");
            ShouldFail("test: funct( -> ) { }");
            ShouldFail("test: funct() { }; Test2: struct { }");
            ShouldFail("test: funct(x: Int8 y: Int8) { }");
            ShouldFail("test: funct(x: Int8, y: Int8 -> ) { }");
            ShouldFail("test: funct(x: Int8, y: Int8 - > Int8) { return 0; }");
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
