using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trapl.Diagnostics;


namespace TraplTest
{
    [TestClass]
    public class BasicTests
    {
        [TestMethod]
        public void StructuralSyntaxTests()
        {
            SyntaxPasses("");

            SyntaxPasses("Test: struct { }");
            SyntaxPasses("Test: struct { x: Int8 }");
            SyntaxPasses("Test: struct { x: Int8, y: Int8 }");
            SyntaxPasses("Test: struct { x: Int8, y: Int16, }");

            SyntaxPasses("test: funct() { }");
            SyntaxPasses("test: funct(x: Int8) { }");
            SyntaxPasses("test: funct(x: Int8, y: Int8) { }");
            SyntaxPasses("test: funct(x: Int8, y: Int8,) { }");
            SyntaxPasses("test: funct(-> Int8) { return 0; }");
            SyntaxPasses("test: funct(x: Int8 -> Int8) { return 0; }");
            SyntaxPasses("test: funct(x: Int8, y: Int8 -> Int8) { return 0; }");
            SyntaxPasses("test: funct(x: Int8, y: Int8, -> Int8) { return 0; }");

            SyntaxPasses("Test: trait { }");

            SyntaxPasses("test: funct() { } Test2: struct { }");

            SyntaxFails("{ }");
            SyntaxFails("test");
            SyntaxFails("test { }");
            SyntaxFails("test:");
            SyntaxFails("test: { }");
            SyntaxFails("test: test { }");
            SyntaxFails("test: 123 { }");
            SyntaxFails("123: struct { }");
            SyntaxFails(": struct { }");
            SyntaxFails("test: funct() { }; Test2: struct { }");

            SyntaxFails("struct { }");
            SyntaxFails("Test struct { }");
            SyntaxFails("Test: struct");
            SyntaxFails("Test: struct {");
            SyntaxFails("Test: struct { x: Int8 y: Int8 }");

            SyntaxFails("funct() { }");
            SyntaxFails("test funct() { }");
            SyntaxFails("test: funct");
            SyntaxFails("test: funct(");
            SyntaxFails("test: funct { }");
            SyntaxFails("test: funct()");
            SyntaxFails("test: funct() {");
            SyntaxFails("test: funct( -> { }");
            SyntaxFails("test: funct( -> ) { }");
            SyntaxFails("test: funct() { }; test2: struct { }");
            SyntaxFails("test: funct(x: Int8 y: Int8) { }");
            SyntaxFails("test: funct(x: Int8, y: Int8 -> ) { }");
            SyntaxFails("test: funct(x: Int8, y: Int8 - > Int8) { return 0; }");
        }


        [TestMethod]
        public void StructuralSemanticsTests()
        {
            SemanticsPass("Test1: struct { x: Test2 } Test2: struct { x: Int8 }");
            SemanticsPass("Test1: struct { x: Int8 } Test2: struct { x: Test1 }");

            SemanticsFail("Test: struct { x: UnknownType }");
            SemanticsFail("Recursive: struct { x: Recursive }");
            SemanticsFail("Recursive1: struct { x: Recursive2 } Recursive2: struct { x: Recursive1 }");
            SemanticsFail("Recursive1: struct { x: Recursive2 } Recursive2: struct { x: Recursive3 } Recursive3: struct { x: Recursive1 }");
            SemanticsFail("Recursive1: struct { x: Recursive3 } Recursive2: struct { x: Recursive1 } Recursive3: struct { x: Recursive2 }");
            SemanticsFail("VoidMember: struct { x: Void }");
            //SemanticsFail("SameName: struct { } SameName: struct { }");

            SemanticsFail("void_argument: funct(x: Void) { }");
            SemanticsFail("void_return_type: funct(x: Int32 -> Void) { }");
            SemanticsFail("same_name: funct() { } same_name: funct() { }");
        }


        delegate string EmbedDelegate(string str);


        [TestMethod]
        public void CodeSyntaxTests()
        {
            EmbedDelegate Embed = str => { return "test: funct() { " + str + " }"; };

            SyntaxPasses(Embed(""));
            SyntaxPasses(Embed("{ }"));
            SyntaxPasses(Embed("let x"));
            SyntaxPasses(Embed("let x = 0"));
            SyntaxPasses(Embed("let x = 0;"));
            SyntaxPasses(Embed("let x: Int8;"));
            SyntaxPasses(Embed("let x: Int8 = 0;"));
            SyntaxPasses(Embed("let x: Int8 = 0; x = x + x;"));

            SyntaxFails(Embed(";"));
            SyntaxFails(Embed("let;"));
            SyntaxFails(Embed("let = 0;"));
        }


        [TestMethod]
        public void CodeSemanticsTests()
        {
            EmbedDelegate Embed = str => { return "test: funct() { " + str + " }"; };

            SemanticsPass(Embed(""));
            SemanticsPass(Embed("{ }"));
            SemanticsPass(Embed("let x: Int8"));
            SemanticsPass(Embed("let x: Int8 = 0"));
            SemanticsPass(Embed("let x = 0"));
            SemanticsPass(Embed("let x: Int8; let x: Int16"));

            SemanticsFail(Embed("let x"));
        }


        private void SyntaxPasses(string str)
        {
            var diagn = new Trapl.Diagnostics.MessageList();

            try
            {
                var src = Trapl.Source.FromString(str);
                var lex = Trapl.Lexer.Analyzer.Pass(src, diagn);
                var syn = Trapl.Syntax.Analyzer.Pass(lex, src, diagn);
                var struc = Trapl.Structure.Analyzer.Pass(syn, src, diagn);

                diagn.Print();
            }
            catch
            {
                Assert.Inconclusive();
            }
               
            Assert.IsTrue(diagn.Passed());
        }


        private void SyntaxFails(string str)
        {
            var diagn = new Trapl.Diagnostics.MessageList();

            try
            {
                var src = Trapl.Source.FromString(str);
                var lex = Trapl.Lexer.Analyzer.Pass(src, diagn);
                var syn = Trapl.Syntax.Analyzer.Pass(lex, src, diagn);
                var struc = Trapl.Structure.Analyzer.Pass(syn, src, diagn);

                diagn.Print();
            }
            catch
            {
                Assert.Inconclusive();
            }

            Assert.IsTrue(diagn.Failed());
        }


        private void SemanticsPass(string str)
        {
            var diagn = new Trapl.Diagnostics.MessageList();

            try
            {
                var src = Trapl.Source.FromString(str);
                var lex = Trapl.Lexer.Analyzer.Pass(src, diagn);
                var syn = Trapl.Syntax.Analyzer.Pass(lex, src, diagn);
                var struc = Trapl.Structure.Analyzer.Pass(syn, src, diagn);
                var semantics = Trapl.Semantics.Analyzer.Pass(struc, diagn);

                diagn.Print();
            }
            catch
            {
                Assert.Inconclusive();
            }

            Assert.IsTrue(diagn.Passed());
        }


        private void SemanticsFail(string str)
        {
            var diagn = new Trapl.Diagnostics.MessageList();

            try
            {
                var src = Trapl.Source.FromString(str);
                var lex = Trapl.Lexer.Analyzer.Pass(src, diagn);
                var syn = Trapl.Syntax.Analyzer.Pass(lex, src, diagn);
                var struc = Trapl.Structure.Analyzer.Pass(syn, src, diagn);
                Assert.IsTrue(diagn.Passed());

                var semantics = Trapl.Semantics.Analyzer.Pass(struc, diagn);

                diagn.Print();
            }
            catch
            {
                Assert.Inconclusive();
            }

            Assert.IsTrue(diagn.Failed());
        }
    }
}
