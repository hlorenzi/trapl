using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TraplTest
{
    [TestClass]
    public class StructTests : Base
    {
        [TestMethod]
        public void TestStructMembers()
        {
            ContainsNoError(Compile("struct Test { x: () }"));
            ContainsNoError(Compile("struct Test { x: (), y: () }"));
            ContainsNoError(Compile("struct Test1 { x: () } struct Test2 { x: Test1 }"));
            ContainsNoError(Compile("struct Test1 { x: Test2 } struct Test2 { x: () }"));

            ContainsErrors(Compile("struct ErrorField { x: UnknownType }"));
            ContainsErrors(Compile("struct DuplicateFields { x: (), x: () }"));
            ContainsErrors(Compile("struct DuplicateFields { x: (), y: (), x: () }"));
            ContainsErrors(Compile("struct Recursive { x: Recursive }"));
            ContainsErrors(Compile("struct Recursive1 { x: Recursive2 } struct Recursive2 { x: Recursive1 }"));
            ContainsErrors(Compile("struct Recursive1 { x: Recursive2 } struct Recursive2 { x: Recursive3 } struct Recursive3 { x: Recursive1 }"));
            ContainsErrors(Compile("struct Recursive1 { x: Recursive3 } struct Recursive2 { x: Recursive1 } struct Recursive3 { x: Recursive2 }"));
        }
    }
}
