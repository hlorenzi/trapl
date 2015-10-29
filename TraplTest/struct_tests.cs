using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TraplTest
{
    [TestClass]
    public class StructTests : Base
    {
        [TestMethod]
        public void TestStructMembers()
        {
            ShouldPass("Test { x: () }");
            ShouldPass("Test { x: (), y: () }");
            ShouldPass("Test1 { x: () } Test2 { x: Test1 }");
            ShouldPass("Test1 { x: Test2 } Test2 { x: () }");

            ShouldFail("ErrorField { x: UnknownType }");
            ShouldFail("DuplicateFields { x: (), x: () }");
            ShouldFail("DuplicateFields { x: (), y: (), x: () }");
            ShouldFail("Recursive { x: Recursive }");
            ShouldFail("Recursive1 { x: Recursive2 } Recursive2 { x: Recursive1 }");
            ShouldFail("Recursive1 { x: Recursive2 } Recursive2 { x: Recursive3 } Recursive3 { x: Recursive1 }");
            ShouldFail("Recursive1 { x: Recursive3 } Recursive2 { x: Recursive1 } Recursive3 { x: Recursive2 }");
        }
    }
}
