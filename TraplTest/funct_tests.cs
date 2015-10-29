using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TraplTest
{
    [TestClass]
    public class FunctTests : Base
    {
        [TestMethod]
        public void TestFunctHeaders()
        {
            ShouldPass("test() {}");
            ShouldPass("test() -> () {}");
            ShouldPass("test(x: ()) -> () {}");

            ShouldFail("error_ret() -> UnknownType {}");
            ShouldFail("error_arg(x: UnknownType) {}");
        }


        [TestMethod]
        public void TestTypeInference()
        {
            var Embed = MakeEmbedder(
                "Apple {}" +
                "Banana {}" +
                "get_nothing() { }" +
                "get_apple() -> Apple { Apple {} }" +
                "get_banana() -> Banana { Banana {} }" +
                "test() {", "}");

            ShouldPass(Embed(""));
            ShouldPass(Embed("let a: Apple;"));
            ShouldPass(Embed("let b: Banana;"));
            ShouldPass(Embed("let a; a = Apple {};"));
            ShouldPass(Embed("let a: Apple; a = Apple {};"));
            ShouldPass(Embed("let a; a = get_apple();"));
            ShouldPass(Embed("let a: Apple; a = get_apple();"));
            ShouldPass(Embed("let a; a = get_apple;"));
            ShouldPass(Embed("let n; n = get_nothing();"));
            ShouldPass(Embed("let n: (); n = get_nothing();"));
            ShouldPass(Embed("let n; n = get_nothing;"));

            ShouldFail(Embed("let a;"));
            ShouldFail(Embed("let a: Apple; let b;"));
            ShouldFail(Embed("let a: Apple; a = Banana {};"));
            ShouldFail(Embed("let a: Apple; a = get_banana();"));
        }
    }
}
