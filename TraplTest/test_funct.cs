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
        public void TestFunctTypes()
        {
            System.Func<string, Trapl.Core.Session> CompileFunctBody = (src) =>
            {
                return Util.Compile("test: fn() -> () { " + src + " }");
            };

            CompileFunctBody("let x: Bool").Ok().LocalOk("x", "Bool");
            CompileFunctBody("let x: Int").Ok().LocalOk("x", "Int");
            CompileFunctBody("let x: (Bool, Int, Bool)").Ok().LocalOk("x", "(Bool, Int, Bool)");

            CompileFunctBody("let x: Bool = true").Ok().LocalOk("x", "Bool");
            CompileFunctBody("let x: Bool = false").Ok().LocalOk("x", "Bool");
            CompileFunctBody("let x: Int = 0").Ok().LocalOk("x", "Int");

            CompileFunctBody("let x: (Int, Bool, Int); x.0 = 0; x.1 = true; x.2 = 0").Ok().LocalOk("x", "(Int, Bool, Int)");

            CompileFunctBody("let x: Bool; x = true").Ok().LocalOk("x", "Bool");
            CompileFunctBody("let x: Bool; x = false").Ok().LocalOk("x", "Bool");
            CompileFunctBody("let x: Int; x = 0").Ok().LocalOk("x", "Int");
        }


        [TestMethod]
        public void TestFunctInference()
        {
            System.Func<string, Trapl.Core.Session> CompileFunctBody = (src) =>
            {
                return Util.Compile("test: fn() -> () { " + src + " }");
            };

            CompileFunctBody("let x = true").Ok().LocalOk("x", "Bool");
            CompileFunctBody("let x = false").Ok().LocalOk("x", "Bool");
            CompileFunctBody("let x = 0").Ok().LocalOk("x", "Int");

            CompileFunctBody("let x: _ = true").Ok().LocalOk("x", "Bool");
            CompileFunctBody("let x: _ = false").Ok().LocalOk("x", "Bool");
            CompileFunctBody("let x: _ = 0").Ok().LocalOk("x", "Int");

            CompileFunctBody("let x; x = true").Ok().LocalOk("x", "Bool");
            CompileFunctBody("let x; x = false").Ok().LocalOk("x", "Bool");
            CompileFunctBody("let x; x = 0").Ok().LocalOk("x", "Int");

            CompileFunctBody("let x: _; x = true").Ok().LocalOk("x", "Bool");
            CompileFunctBody("let x: _; x = false").Ok().LocalOk("x", "Bool");
            CompileFunctBody("let x: _; x = 0").Ok().LocalOk("x", "Int");

            CompileFunctBody("let x").Fail();
            CompileFunctBody("let x: _").Fail();
        }


        [TestMethod]
        public void TestFunctDotOperator()
        {
            System.Func<string, Trapl.Core.Session> CompileFunctBody = (src) =>
            {
                return Util.Compile(
                    "Data: struct { i: Int, b: Bool }" +
                    "test: fn() -> () { " + src + " }");
            };

            CompileFunctBody("let x: Data; x.i = 0; x.b = true").Ok();
            CompileFunctBody("let x: (Int, Bool); x.0 = 0; x.1 = true").Ok();
            CompileFunctBody("let x: (Int, Data); x.0 = 0; (x.1).i = 0; (x.1).b = true").Ok();
            CompileFunctBody("let x: (Int, Data); let y: Data; y.i = 0; y.b = true; x.0 = 0; x.1 = y").Ok();

            CompileFunctBody("let x: Int; x.z = 0").Fail();
            CompileFunctBody("let x: Int; x.0 = 0").Fail();
            CompileFunctBody("let x: Data; x.z = 0").Fail();
            CompileFunctBody("let x: Data; x.0 = 0").Fail();
            CompileFunctBody("let x: (Int, Bool); x.2 = 0").Fail();
            CompileFunctBody("let x: (Int, Bool); x.i = 0").Fail();
        }
    }
}
