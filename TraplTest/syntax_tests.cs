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
            ShouldPass("Test { x: A }");
            ShouldPass("Test { x: A, }");
            ShouldPass("Test { x: A, y: B }");
            ShouldPass("Test { x: A, y: B, }");
            ShouldPass("Test1 { x: A } Test2 { x: B }");

            ShouldFail("{ }");
            ShouldFail("Test");
            ShouldFail("Test { ");
            ShouldFail("Test: { }");
            ShouldFail("123 { }");
            ShouldFail("Test1 { }; Test2 { }");
            ShouldFail("Test { , }");
            ShouldFail("Test { x: Int8 y: Int8 }");
            ShouldFail("Test { x: Int8, y: Int8,, }");
            ShouldFail("Test { x: Int8,, y: Int8 }");
        }


        private void ShouldPass(string sourceStr)
        {
            var session = new Trapl.Infrastructure.Session();
            session.AddUnit(Trapl.Infrastructure.Unit.MakeFromString(sourceStr));
            Assert.IsTrue(session.diagn.ContainsNoError());
        }


        private void ShouldFail(string sourceStr)
        {
            var session = new Trapl.Infrastructure.Session();
            session.AddUnit(Trapl.Infrastructure.Unit.MakeFromString(sourceStr));
            Assert.IsTrue(session.diagn.ContainsErrors());
        }
    }
}
