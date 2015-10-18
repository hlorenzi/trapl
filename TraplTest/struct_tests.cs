using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TraplTest
{
    [TestClass]
    public class StructTests
    {
        [TestMethod]
        public void TestStructMembers()
        {
            ShouldPass("Test1 { }");
            ShouldPass("Test1 { } Test2 { x: Test1 }");
            ShouldPass("Test1 { } Test2 { x: Test1, y: Test1 }");
            ShouldPass("Test1 { } Test2 { x: Test1 } Test3 { x: Test2 }");
            ShouldPass("Test1 { } Test2 { x: Test3 } Test3 { x: Test1 }");

            ShouldFail("Test { x: UnknownType }");
            ShouldFail("Test { } DuplicateFields { x: Test, x: Test }");
            ShouldFail("Test { } DuplicateFields { x: Test, y: Test, x: Test }");
            ShouldFail("Recursive { x: Recursive }");
            ShouldFail("Recursive1 { x: Recursive2 } Recursive2 { x: Recursive1 }");
            ShouldFail("Recursive1 { x: Recursive2 } Recursive2 { x: Recursive3 } Recursive3 { x: Recursive1 }");
            ShouldFail("Recursive1 { x: Recursive3 } Recursive2 { x: Recursive1 } Recursive3 { x: Recursive2 }");
        }


        private void ShouldPass(string sourceStr)
        {
            Assert.IsTrue(CompileAndTest(sourceStr));
        }


        private void ShouldFail(string sourceStr)
        {
            Assert.IsFalse(CompileAndTest(sourceStr));
        }


        private bool CompileAndTest(string sourceStr)
        {
            var session = new Trapl.Infrastructure.Session();
            session.AddUnit(Trapl.Infrastructure.Unit.MakeFromString(sourceStr));

            if (session.diagn.ContainsErrors())
                Assert.Inconclusive();

            session.Resolve();

            return session.diagn.ContainsNoError();
        }
    }
}
