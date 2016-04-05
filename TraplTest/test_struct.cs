using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TraplTest
{
    [TestClass]
    public class TestStruct
    {
        [TestMethod]
        public void TestStructNames()
        {
            Util.Compile("struct A { }").Ok();
            Util.Compile("struct A { } struct B { }").Ok();

            Util.Compile("struct A { } struct A { }").Fail();
        }


        [TestMethod]
        public void TestStructFields()
        {
            Util.Compile("struct A { x: Int }").Ok();
            Util.Compile("struct A { x: Int, y: Bool }").Ok();
            Util.Compile("struct Rec { x: *Rec }").Ok();
            Util.Compile("struct Rec { x: (Int, *Rec) }").Ok();
            Util.Compile("struct Rec { x: *(Int, Rec) }").Ok();

            Util.Compile("struct PlaceholderField { x: _ }").Fail();
            Util.Compile("struct DuplicateField { x: Int, x: Bool }").Fail();
            Util.Compile("struct DuplicateField { x: Int, y: Bool, x: Int }").Fail();
            Util.Compile("struct Rec { x: Rec }").Fail();
            Util.Compile("struct Rec { x: (Int, Rec) }").Fail();
            Util.Compile("struct Rec1 { x: Rec2 } struct Rec2 { x: Rec1 }").Fail();
            Util.Compile("struct Rec1 { x: Rec2 } struct Rec2 { x: Rec3 } struct Rec3 { x: Rec1 }").Fail();
        }
    }
}
