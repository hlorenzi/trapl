using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TraplTest
{
    [TestClass]
    public class FunctTests : Base
    {
        [TestMethod]
        public void TestFunctHeaders()
        {
            ContainsNoError(Compile("test() {}"));
            ContainsNoError(Compile("test() -> () {}"));
            ContainsNoError(Compile("test(x: ()) -> () {}"));

            ContainsErrors(Compile("error_ret() -> UnknownType {}"));
            ContainsErrors(Compile("error_arg(x: UnknownType) {}"));
        }


        [TestMethod]
        public void TestTypeInference()
        {
            ForEach
            (
                (str) =>
                {
                    var session = Compile(
                        "Apple {}" +
                        "Banana {}" +
                        "get_nothing() { }" +
                        "get_apple() -> Apple { Apple {} }" +
                        "get_banana() -> Banana { Banana {} }" +
                        "test() {" + str + "}");

                    Assert.IsTrue(CheckLocalType(session, "a", "Apple"));
                    Assert.IsTrue(CheckLocalType(session, "b", "Banana"));
                    Assert.IsTrue(CheckLocalType(session, "n", "()"));
                },

                "let a: Apple",
                "let b: Banana",
                "let n: ()",
                "let a; a = Apple {}",
                "let b; b = Banana {}",
                "let a; a = get_apple()",
                "let b; b = get_banana()",
                "let n; n = get_nothing()",
                "let a; let z; z = Apple {}; a = z"
            );
        }
    }
}
