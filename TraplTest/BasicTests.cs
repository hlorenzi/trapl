using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trapl.Diagnostics;


namespace TraplTest
{
    [TestClass]
    public class BasicTests
    {
        [TestMethod]
        public void StructuralTests()
        {
            Passes("");

            Passes("Test: struct { }");
            Passes("Test: struct { x: Int8 }");
            Passes("Test: struct { x: Int8, y: Int8 }");
            Passes("Test: struct { x: Int8, y: Int16, }");
            Passes("Test1: struct { x: Test2 } Test2: struct { x: Int8 }");
            Passes("Test1: struct { x: Int8 } Test2: struct { x: Test1 }");

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
            SyntaxFails("test: funct( -> { }");
            SyntaxFails("test: funct( -> ) { }");
            SyntaxFails("test: funct() { }; Test2: struct { }");
            SyntaxFails("test: funct(x: Int8 y: Int8) { }");
            SyntaxFails("test: funct(x: Int8, y: Int8 -> ) { }");
            SyntaxFails("test: funct(x: Int8, y: Int8 - > Int8) { return 0; }");

            SemanticsFail("Test: struct { x: UnknownType }");
            //SemanticsFail("RepeatedMembers: struct { x: Int8, x: Int16 }");
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
        public void CodeTests()
        {
            EmbedDelegate Embed = str => { return "test: funct() { " + str + " }"; };
            EmbedDelegate Embed2 = str => { return "test: funct() { let x: Int32 = 0; let ptr: &Int32 = &x; " + str + " }"; };

            Passes(Embed(""));
            Passes(Embed("{ }"));
            Passes(Embed("let x = 0"));
            Passes(Embed("let x = 0;"));
            Passes(Embed("let x: Int32;"));
            Passes(Embed("let x: Int32 = 0;"));
            /*Passes(Embed("let x: Int32 = 0; x = 1 + 1;"));
            Passes(Embed("let x: Int32 = 0; x = x + 1;"));
            Passes(Embed("let x: Int32 = 0; x = 1 + x;"));
            Passes(Embed("let x: Int32 = 0; x = x + x;"));
            Passes(Embed("let x: Int32 = 0; x = 1 - 1;"));
            Passes(Embed("let x: Int32 = 0; x = 1 + -1;"));
            Passes(Embed("let x: Int32 = 0; x = 1 + (-1);"));*/

            Passes(Embed("let x: Int32; let x: Int64"));
            Passes(Embed("let x: &Int32"));
            Passes(Embed("let x: &&Int32"));
            Passes(Embed("let x: &&&&&Int32"));
            Passes(Embed("let x: Int32; let ptr: &Int32; ptr = &x"));
            Passes(Embed("let x: Int32; let ptr: &Int32; @ptr = x"));

            Passes(Embed2("@ptr = x"));
            Passes(Embed2("x = @ptr"));
            Passes(Embed2("@&@ptr = x"));
            Passes(Embed2("x = @&@ptr"));
            Passes(Embed2("ptr = &@&x"));
            Passes(Embed2("let ptrptr: &&Int32 = &ptr"));

            SyntaxFails(Embed(";"));
            SyntaxFails(Embed("let;"));
            SyntaxFails(Embed("let = 0;"));

            SemanticsFail(Embed("let x"));
            SemanticsFail(Embed2("ptr = x"));
            SemanticsFail(Embed2("x = ptr"));
            SemanticsFail(Embed2("&x = ptr"));
        }


        private void Passes(string str)
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
