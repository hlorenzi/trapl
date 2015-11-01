using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TraplTest
{
    [TestClass]
    public class StructTests : Base
    {
        [TestMethod]
        public void TestStructMembers()
        {
            ContainsNoError(Compile("Test { x: () }"));
            ContainsNoError(Compile("Test { x: (), y: () }"));
            ContainsNoError(Compile("Test1 { x: () } Test2 { x: Test1 }"));
            ContainsNoError(Compile("Test1 { x: Test2 } Test2 { x: () }"));

            ContainsErrors(Compile("ErrorField { x: UnknownType }"));
            ContainsErrors(Compile("DuplicateFields { x: (), x: () }"));
            ContainsErrors(Compile("DuplicateFields { x: (), y: (), x: () }"));
            ContainsErrors(Compile("Recursive { x: Recursive }"));
            ContainsErrors(Compile("Recursive1 { x: Recursive2 } Recursive2 { x: Recursive1 }"));
            ContainsErrors(Compile("Recursive1 { x: Recursive2 } Recursive2 { x: Recursive3 } Recursive3 { x: Recursive1 }"));
            ContainsErrors(Compile("Recursive1 { x: Recursive3 } Recursive2 { x: Recursive1 } Recursive3 { x: Recursive2 }"));
        }
    }
}
