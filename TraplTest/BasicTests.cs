using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trapl.Diagnostics;


namespace TraplTest
{
    [TestClass]
    public class BasicTests
    {
        [TestMethod]
        public void Run()
        {
            // Passing tests.
            Assert.IsTrue(Compile("").Passed());
            Assert.IsTrue(Compile("test: funct() { } test2: struct { }").Passed());

            Assert.IsTrue(Compile("test: struct { }").Passed());
            Assert.IsTrue(Compile("test: struct { x: Int8 }").Passed());
            Assert.IsTrue(Compile("test: struct { x: Int8, y: Int8 }").Passed());
            Assert.IsTrue(Compile("test: struct { x: Int8, y: Int16, }").Passed());

            Assert.IsTrue(Compile("test: funct() { }").Passed());
            Assert.IsTrue(Compile("test: funct(x: Int8) { }").Passed());
            Assert.IsTrue(Compile("test: funct(x: Int8, y: Int8) { }").Passed());

            Assert.IsTrue(Compile("test: trait { }").Passed());

            // Failing tests.
            Assert.IsTrue(Compile("test").Failed());
            Assert.IsTrue(Compile("test:").Failed());
            Assert.IsTrue(Compile("test: { }").Failed());
            Assert.IsTrue(Compile("test: test { }").Failed());
            Assert.IsTrue(Compile("test: 123 { }").Failed());
            Assert.IsTrue(Compile("test: funct() { }; test2: struct { }").Failed());

            Assert.IsTrue(Compile("test struct { }").Failed());
            Assert.IsTrue(Compile("test: struct").Failed());
            Assert.IsTrue(Compile("test: struct {").Failed());
            Assert.IsTrue(Compile("test: struct { x: Int8 y: Int16 }").Failed());

            Assert.IsTrue(Compile("test funct() { }").Failed());
            Assert.IsTrue(Compile("test: funct").Failed());
            Assert.IsTrue(Compile("test: funct(").Failed());
            Assert.IsTrue(Compile("test: funct { }").Failed());
            Assert.IsTrue(Compile("test: funct()").Failed());
            Assert.IsTrue(Compile("test: funct() {").Failed());
            Assert.IsTrue(Compile("test: funct() { }; test2: struct { }").Failed());
            Assert.IsTrue(Compile("test: funct(x: Int8 y: Int8) { }").Failed());
        }


        private Trapl.Diagnostics.MessageList Compile(string str)
        {
            var src = Trapl.Source.FromString(str);
            var diagn = new Trapl.Diagnostics.MessageList();

            var lex = Trapl.Lexer.Analyzer.Pass(src, diagn);
            var syn = Trapl.Syntax.Analyzer.Pass(lex, src, diagn);
            var struc = Trapl.Structure.Analyzer.Pass(syn, src, diagn);
            var semantics = Trapl.Semantics.Analyzer.Pass(struc, diagn);

            diagn.Print();

            return diagn;
        }
    }
}
