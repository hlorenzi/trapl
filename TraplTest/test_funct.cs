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
            System.Func<string, Trapl.Core.Session> Funct = (src) =>
            {
                return Util.Compile("test: fn() -> () { " + src + " }");
            };

            Funct("").Ok();

            Funct("let x: Bool").Ok().LocalOk("x", "Bool");
            Funct("let x: Int").Ok().LocalOk("x", "Int");
            Funct("let x: (Bool, Int, Bool)").Ok().LocalOk("x", "(Bool, Int, Bool)");

            Funct("let x: Bool = true").Ok().LocalOk("x", "Bool");
            Funct("let x: Bool = false").Ok().LocalOk("x", "Bool");
            Funct("let x: Int = 0").Ok().LocalOk("x", "Int");

            Funct("let x: (Int, Bool, Int); x.0 = 0; x.1 = true; x.2 = 0").Ok().LocalOk("x", "(Int, Bool, Int)");

            Funct("let x: Bool; x = true").Ok().LocalOk("x", "Bool");
            Funct("let x: Bool; x = false").Ok().LocalOk("x", "Bool");
            Funct("let x: Int; x = 0").Ok().LocalOk("x", "Int");
        }


        [TestMethod]
        public void TestFunctInference()
        {
            System.Func<string, Trapl.Core.Session> Funct = (src) =>
            {
                return Util.Compile("test: fn() -> () { " + src + " }");
            };

            Funct("").Ok();

            Funct("let x = true").Ok().LocalOk("x", "Bool");
            Funct("let x = false").Ok().LocalOk("x", "Bool");
            Funct("let x = 0").Ok().LocalOk("x", "Int");

            Funct("let x: _ = true").Ok().LocalOk("x", "Bool");
            Funct("let x: _ = false").Ok().LocalOk("x", "Bool");
            Funct("let x: _ = 0").Ok().LocalOk("x", "Int");

            Funct("let x; x = true").Ok().LocalOk("x", "Bool");
            Funct("let x; x = false").Ok().LocalOk("x", "Bool");
            Funct("let x; x = 0").Ok().LocalOk("x", "Int");

            Funct("let x: _; x = true").Ok().LocalOk("x", "Bool");
            Funct("let x: _; x = false").Ok().LocalOk("x", "Bool");
            Funct("let x: _; x = 0").Ok().LocalOk("x", "Int");

            Funct("let x").Fail();
            Funct("let x: _").Fail();
        }


        [TestMethod]
        public void TestFunctDotOperator()
        {
            System.Func<string, Trapl.Core.Session> Funct = (src) =>
            {
                return Util.Compile(
                    "Data: struct { i: Int, b: Bool }" +
                    "test: fn() -> () { " + src + " }");
            };

            Funct("").Ok();

            Funct("let x: Data; x.i = 0; x.b = true").Ok();
            Funct("let x: (Int, Bool); x.0 = 0; x.1 = true").Ok();
            Funct("let x: (Int, Data); x.0 = 0; (x.1).i = 0; (x.1).b = true").Ok();
            Funct("let x: (Int, Data); let y: Data; y.i = 0; y.b = true; x.0 = 0; x.1 = y").Ok();

            Funct("let x: Int; x.z = 0").Fail();
            Funct("let x: Int; x.0 = 0").Fail();
            Funct("let x: Data; x.z = 0").Fail();
            Funct("let x: Data; x.0 = 0").Fail();
            Funct("let x: (Int, Bool); x.2 = 0").Fail();
            Funct("let x: (Int, Bool); x.i = 0").Fail();
        }


        [TestMethod]
        public void TestFunctReturn()
        {
            System.Func<string, Trapl.Core.Session> VoidFunct = (src) =>
            {
                return Util.Compile("test: fn() -> () { " + src + " }");
            };

            System.Func<string, Trapl.Core.Session> IntFunct = (src) =>
            {
                return Util.Compile("test: fn() -> Int { " + src + " }");
            };

            VoidFunct("").Ok();
            VoidFunct("123").Ok();
            IntFunct("return 123").Ok();

            VoidFunct("return 123").Fail();
            IntFunct("").Fail();
            IntFunct("123").Fail();
            IntFunct("return true").Fail();
        }
    }
}
