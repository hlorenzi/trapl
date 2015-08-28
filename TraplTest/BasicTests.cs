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
            Passes("");

            Passes("Test: struct { }");
            Passes("Test: struct { x: Int8 }");
            Passes("Test: struct { x: Int8, y: Int8 }");
            Passes("Test: struct { x: Int8, y: Int16, }");

            Passes("test: funct() { }");
            Passes("test: funct(x: Int8) { }");
            Passes("test: funct(x: Int8, y: Int8) { }");
            Passes("test: funct(x: Int8, y: Int8,) { }");
            Passes("test: funct(-> Int8) { return 0; }");
            Passes("test: funct(x: Int8 -> Int8) { return 0; }");
            Passes("test: funct(x: Int8, y: Int8 -> Int8) { return 0; }");
            Passes("test: funct(x: Int8, y: Int8, -> Int8) { return 0; }");

            Passes("Test: trait { }");

            Passes("test: funct() { } Test2: struct { }");

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
            SyntaxFails("test: funct() { }; test2: struct { }");
            SyntaxFails("test: funct(x: Int8 y: Int8) { }");
            SyntaxFails("test: funct(x: Int8, y: Int8 -> ) { }");
        }


        [TestMethod]
        public void StructuralSemanticsTests()
        {
            Passes("Test1: struct { x: Test2 } Test2: struct { x: Int8 }");
            Passes("Test1: struct { x: Int8 } Test2: struct { x: Test1 }");

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
            EmbedDelegate Embed = str => { return "Main: funct() { " + str + " }"; };

            Passes(Embed(""));
            Passes(Embed("{ }"));
            Passes(Embed("let x = 0"));
            Passes(Embed("let x = 0;"));
            Passes(Embed("let x: Int8;"));
            Passes(Embed("let x: Int8 = 0;"));
            Passes(Embed("let x: Int8 = 0; x = x + x;"));

            SyntaxFails(Embed(";"));
            SyntaxFails(Embed("let;"));
            SyntaxFails(Embed("let = 0;"));
        }


        private void Passes(string str)
        {
            var src = Trapl.Source.FromString(str);
            var diagn = new Trapl.Diagnostics.MessageList();

            var lex = Trapl.Lexer.Analyzer.Pass(src, diagn);
            var syn = Trapl.Syntax.Analyzer.Pass(lex, src, diagn);
            var struc = Trapl.Structure.Analyzer.Pass(syn, src, diagn);
            var semantics = Trapl.Semantics.Analyzer.Pass(struc, diagn);

            diagn.Print();
            Assert.IsTrue(diagn.Passed());
        }


        private void SyntaxFails(string str)
        {
            var src = Trapl.Source.FromString(str);
            var diagn = new Trapl.Diagnostics.MessageList();

            var lex = Trapl.Lexer.Analyzer.Pass(src, diagn);
            var syn = Trapl.Syntax.Analyzer.Pass(lex, src, diagn);
            var struc = Trapl.Structure.Analyzer.Pass(syn, src, diagn);

            diagn.Print();
            Assert.IsTrue(diagn.Failed());
        }


        private void SemanticsFail(string str)
        {
            var src = Trapl.Source.FromString(str);
            var diagn = new Trapl.Diagnostics.MessageList();

            var lex = Trapl.Lexer.Analyzer.Pass(src, diagn);
            var syn = Trapl.Syntax.Analyzer.Pass(lex, src, diagn);
            var struc = Trapl.Structure.Analyzer.Pass(syn, src, diagn);
            Assert.IsTrue(diagn.Passed());

            var semantics = Trapl.Semantics.Analyzer.Pass(struc, diagn);

            diagn.Print();
            Assert.IsTrue(diagn.Failed());
        }
    }
}
