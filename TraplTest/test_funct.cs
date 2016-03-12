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
                    "Data: struct { i: Int, b: Bool, p: &Int }" +
                    "test: fn() -> () { " + src + " }");
            };

            Funct("").Ok();

            Funct("let d: Data; d.i = 0").Ok();
            Funct("let d: Data; d.b = true").Ok();
            Funct("let d: Data; let x = 0; d.p = &x").Ok();

            Funct("let t: (Int, Int, Int); t.0 = 0; t.1 = 1; t.2 = 2").Ok();
            Funct("let t: (Data); let x = 0; (t.0).i = 0; (t.0).b = true; (t.0).p = &x").Ok();
            Funct("let t: (Data); let d: Data; let x = 0; d.i = 0; d.b = true; d.p = &x; t.0 = d").Ok();

            Funct("let i: Int; i.z = 0").Fail();
            Funct("let i: Int; i.0 = 0").Fail();
            Funct("let d: Data; d.z = 0").Fail();
            Funct("let d: Data; d.0 = 0").Fail();
            Funct("let t: (Int, Bool); t.2 = 0").Fail();
            Funct("let t: (Int, Bool); t.i = 0").Fail();
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


        [TestMethod]
        public void TestFunctInitChecker()
        {
            System.Func<string, Trapl.Core.Session> Funct = (src) =>
            {
                return Util.Compile(
                    "Data: struct { i: Int, b: Bool, p: &Int }" +
                    "test: fn() -> () { " + src + " }");
            };

            Funct("").Ok();

            Funct("let i: Int = 0; let j = i").Ok();
            Funct("let i: Int = 0; let j = &i").Ok();
            Funct("let i: Int;     let j = i").Fail();
            Funct("let i: Int;     let j = &i").Fail();

            Funct("let d: Data; d.i = 0; let i = d.i").Ok();
            Funct("let d: Data; d.i = 0; let i = &d.i").Ok();
            Funct("let d: Data;          let i = d.i").Fail();
            Funct("let d: Data;          let i = &d.i").Fail();

            Funct("let d: Data; let i = 0; d.i = 0; d.b = true; d.p = &i; let d2 = &d").Ok();
            Funct("let d: Data; let i = 0;          d.b = true; d.p = &i; let d2 = &d").Fail();
            Funct("let d: Data; let i = 0; d.i = 0;             d.p = &i; let d2 = &d").Fail();
            Funct("let d: Data; let i = 0; d.i = 0; d.b = true;           let d2 = &d").Fail();
            Funct("let d: Data;                                           let d2 = &d").Fail();

            Funct("let d: Data; let i = 0; d.p = &i").Ok();
            Funct("let d: Data;            d.p = &i").Fail();

            Funct("let i = 0;  let pi = &i;  let vi = @pi").Ok();
            Funct("let i = 0;                let vi = @pi").Fail();
            Funct("let i: Int; let pi = &i;  let vi = @pi").Fail();

            Funct("let pi: &Int; let i = 0; pi = &i; let vi = @pi").Ok();
            Funct("let pi: &Int;                     let vi = @pi").Fail();

            Funct("let d: Data; let i = 0; d.p = &i; let vi = @(d.p)").Ok();
            Funct("let d: Data;                      let vi = @(d.p)").Fail();

            Funct("let d: Data; let i = 0; d.i = 0; d.b = true; d.p = &i; let d2 = &d; let d3 = @d2").Ok();
            Funct("let d: Data; let i = 0; d.i = 0; d.b = true; d.p = &i; let d2 = &d; let j = @d2.i").Ok();
            Funct("let d: Data; let i = 0; d.i = 0; d.b = true; d.p = &i; let d2 = &d; let j = @d2.b").Ok();
            Funct("let d: Data; let i = 0; d.i = 0; d.b = true; d.p = &i; let d2 = &d; let j = @d2.p").Ok();
            Funct("let d: Data; let i = 0; d.i = 0; d.b = true; d.p = &i; let d2 = &d; let j = @(@d2.p)").Ok();

            Funct("let i: Int; if true { i = 1 } else { i = 0 }; let j = i").Ok();
            Funct("let i: Int; if true { i = 1 } else {       }; let j = i").Fail();
            Funct("let i: Int; if true {       } else { i = 0 }; let j = i").Fail();

            Funct("let i: Int; if true { i = 1; let j = i } else { i = 0; let j = i }").Ok();
            Funct("let i: Int; if true { i = 1; let j = i } else {                  }").Ok();
            Funct("let i: Int; if true {                  } else { i = 0; let j = i }").Ok();
        }
    }
}
