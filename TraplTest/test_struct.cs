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

            Util.Compile("struct A { x: _ }").Fail();
        }
    }
}
