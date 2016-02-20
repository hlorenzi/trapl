using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TraplTest
{
    [TestClass]
    public class TestStruct
    {
        [TestMethod]
        public void TestStructNames()
        {
            Util.Compile("A: struct { }").Ok();
            Util.Compile("A: struct { } B: struct { }").Ok();

            Util.Compile("A: struct { } A: struct { }").Fail();
        }


        [TestMethod]
        public void TestStructFields()
        {
            Util.Compile("A: struct { x: Int }").Ok();
            Util.Compile("A: struct { x: Int, y: Bool }").Ok();

            Util.Compile("A: struct { x: _ }").Fail();
        }
    }
}
