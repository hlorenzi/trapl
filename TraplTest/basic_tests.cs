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
            ShouldSucceed("");

            ShouldSucceed("Test: struct { }");
            ShouldSucceed("Test: struct { x: Int8 }");
            ShouldSucceed("Test: struct { x: Int8, y: Int8 }");
            ShouldSucceed("Test: struct { x: Int8, y: Int16, }");
            ShouldSucceed("Test1: struct { x: Test2 } Test2: struct { x: Int8 }");
            ShouldSucceed("Test1: struct { x: Int8 } Test2: struct { x: Test1 }");

            ShouldSucceed("test: funct() { }");
            ShouldSucceed("test: funct(x: Int8) { }");
            ShouldSucceed("test: funct(x: Int8, y: Int8) { }");
            ShouldSucceed("test: funct(x: Int8, y: Int8,) { }");
            ShouldSucceed("test: funct(-> Int8) { return 0; }");
            ShouldSucceed("test: funct(x: Int8 -> Int8) { return 0; }");
            ShouldSucceed("test: funct(x: Int8, y: Int8 -> Int8) { return 0; }");
            ShouldSucceed("test: funct(x: Int8, y: Int8, -> Int8) { return 0; }");

            ShouldSucceed("Test: trait { }");

            ShouldSucceed("test: funct() { } Test2: struct { }");

            GrammarShouldFail("{ }");
            GrammarShouldFail("test");
            GrammarShouldFail("test { }");
            GrammarShouldFail("test:");
            GrammarShouldFail("test: { }");
            GrammarShouldFail("test: test { }");
            GrammarShouldFail("test: 123 { }");
            GrammarShouldFail("123: struct { }");
            GrammarShouldFail(": struct { }");
            GrammarShouldFail("test: funct() { }; Test2: struct { }");

            GrammarShouldFail("struct { }");
            GrammarShouldFail("Test struct { }");
            GrammarShouldFail("Test: struct");
            GrammarShouldFail("Test: struct {");
            GrammarShouldFail("Test: struct { x: Int8 y: Int8 }");

            GrammarShouldFail("funct() { }");
            GrammarShouldFail("test funct() { }");
            GrammarShouldFail("test: funct");
            GrammarShouldFail("test: funct(");
            GrammarShouldFail("test: funct { }");
            GrammarShouldFail("test: funct()");
            GrammarShouldFail("test: funct() {");
            GrammarShouldFail("test: funct( -> { }");
            GrammarShouldFail("test: funct( -> ) { }");
            GrammarShouldFail("test: funct() { }; Test2: struct { }");
            GrammarShouldFail("test: funct(x: Int8 y: Int8) { }");
            GrammarShouldFail("test: funct(x: Int8, y: Int8 -> ) { }");
            GrammarShouldFail("test: funct(x: Int8, y: Int8 - > Int8) { return 0; }");

            SemanticsShouldFail("Test: struct { x: UnknownType }");
            //SemanticsShouldFail("RepeatedMembers: struct { x: Int8, x: Int16 }");
            SemanticsShouldFail("Recursive: struct { x: Recursive }");
            SemanticsShouldFail("Recursive1: struct { x: Recursive2 } Recursive2: struct { x: Recursive1 }");
            SemanticsShouldFail("Recursive1: struct { x: Recursive2 } Recursive2: struct { x: Recursive3 } Recursive3: struct { x: Recursive1 }");
            SemanticsShouldFail("Recursive1: struct { x: Recursive3 } Recursive2: struct { x: Recursive1 } Recursive3: struct { x: Recursive2 }");
            SemanticsShouldFail("VoidMember: struct { x: Void }");
            //SemanticsShouldFail("SameName: struct { } SameName: struct { }");

            SemanticsShouldFail("void_argument: funct(x: Void) { }");
            SemanticsShouldFail("void_return_type: funct(x: Int32 -> Void) { }");
            SemanticsShouldFail("same_name: funct() { } same_name: funct() { }");
        }


        delegate string EmbedDelegate(string str);


        [TestMethod]
        public void CodeTests()
        {
            EmbedDelegate Embed = str => { return "test: funct() { " + str + " }"; };
            EmbedDelegate Embed2 = str => { return "test: funct() { let x: Int32 = 0; let ptr: &Int32 = &x; " + str + " }"; };

            ShouldSucceed(Embed(""));
            ShouldSucceed(Embed("{ }"));
            ShouldSucceed(Embed("let x = 0"));
            ShouldSucceed(Embed("let x = 0;"));
            ShouldSucceed(Embed("let x: Int32;"));
            ShouldSucceed(Embed("let x: Int32 = 0;"));

            ShouldSucceed(Embed("let x: Int32; let x: Int64"));
            ShouldSucceed(Embed("let x: &Int32"));
            ShouldSucceed(Embed("let x: &&Int32"));
            ShouldSucceed(Embed("let x: &&&&&Int32"));
            ShouldSucceed(Embed("let x: Int32; let ptr: &Int32; ptr = &x"));
            ShouldSucceed(Embed("let x: Int32; let ptr: &Int32; @ptr = x"));

            ShouldSucceed(Embed2("@ptr = x"));
            ShouldSucceed(Embed2("x = @ptr"));
            ShouldSucceed(Embed2("@&@ptr = x"));
            ShouldSucceed(Embed2("x = @&@ptr"));
            ShouldSucceed(Embed2("ptr = &@&x"));
            ShouldSucceed(Embed2("let ptrptr: &&Int32 = &ptr"));

            GrammarShouldFail(Embed(";"));
            GrammarShouldFail(Embed("let;"));
            GrammarShouldFail(Embed("let = 0;"));

            SemanticsShouldFail(Embed("let x"));
            SemanticsShouldFail(Embed2("ptr = x"));
            SemanticsShouldFail(Embed2("x = ptr"));
            SemanticsShouldFail(Embed2("&x = ptr"));
        }


        private void ShouldSucceed(string str)
        {
            /*var diagn = new Trapl.Diagnostics.Collection();

            try
            {
                var src = Trapl.Interface.SourceCode.MakeFromString(str);
                var lex = Trapl.Grammar.Tokenizer.Tokenize(src, diagn);
                var syn = Trapl.Grammar.ASTParser.Parse(lex, src, diagn);
                var struc = Trapl.Semantics.DefinitionGatherer.Gather(syn, src, diagn);
                var semantics = Trapl.Semantics.DefinitionGatherer.Pass(struc, diagn);

                diagn.PrintToConsole();
            }
            catch
            {
                Assert.Inconclusive();
            }
               
            Assert.IsTrue(diagn.HasNoError());*/
        }


        private void GrammarShouldFail(string str)
        {
            /*var diagn = new Trapl.Diagnostics.Collection();

            try
            {
                var src = Trapl.SourceCode.MakeFromString(str);
                var lex = Trapl.Grammar.Tokenizer.Tokenize(src, diagn);
                var syn = Trapl.Grammar.ASTParser.Parse(lex, src, diagn);
                var struc = Trapl.Semantics.DefinitionGatherer.Gather(syn, src, diagn);

                diagn.PrintToConsole();
            }
            catch
            {
                Assert.Inconclusive();
            }

            Assert.IsTrue(diagn.HasErrors());*/
        }


        private void SemanticsShouldFail(string str)
        {
            /*var diagn = new Trapl.Diagnostics.Collection();

            try
            {
                var src = Trapl.SourceCode.MakeFromString(str);
                var lex = Trapl.Grammar.Tokenizer.Tokenize(src, diagn);
                var syn = Trapl.Grammar.ASTParser.Parse(lex, src, diagn);
                var struc = Trapl.Semantics.DefinitionGatherer.Gather(syn, src, diagn);
                Assert.IsTrue(diagn.HasNoError());

                var semantics = Trapl.Semantics.DefinitionGatherer.Pass(struc, diagn);

                diagn.PrintToConsole();
            }
            catch
            {
                Assert.Inconclusive();
            }

            Assert.IsTrue(diagn.HasErrors()); */
        }
    }
}
