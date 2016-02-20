using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TraplTest
{
    static class TestFunctHelpers
    {
        public static Trapl.Core.Session LocalOk(this Trapl.Core.Session session, string localName, string typeName)
        {
            Util.LocalTypeOk(session, "test", localName, typeName);
            return session;
        }
    }


    [TestClass]
    public class TestFunct
    {
        [TestMethod]
        public void TestFunctInference()
        {
            System.Func<string, Trapl.Core.Session> CompileFunctBody = (src) =>
            {
                return Util.Compile("test: fn() -> () { " + src + " }");
            };

            CompileFunctBody("let x: Bool;").Ok().LocalOk("x", "Bool");
            CompileFunctBody("let x: Int;").Ok().LocalOk("x", "Int");
            CompileFunctBody("let x = false;").Ok().LocalOk("x", "Bool");
            CompileFunctBody("let x = true;").Ok().LocalOk("x", "Bool");
            CompileFunctBody("let x = 0;").Ok().LocalOk("x", "Int");
            CompileFunctBody("let x: Bool = false;").Ok().LocalOk("x", "Bool");
            CompileFunctBody("let x: Bool = true;").Ok().LocalOk("x", "Bool");
            CompileFunctBody("let x: Int = 0;").Ok().LocalOk("x", "Int");

            CompileFunctBody("let x;").Fail();
            CompileFunctBody("let x: Bool = 0;").Fail().LocalOk("x", "Bool");
            CompileFunctBody("let x: Int = true;").Fail().LocalOk("x", "Int");
        }
    }
}
