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

            //Funct("let x: (Int, Bool, Int); x.0 = 0; x.1 = true; x.2 = 0").Ok().LocalOk("x", "(Int, Bool, Int)");

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
            Funct("let x = 0").Ok().LocalOk("x", "Int");

            Funct("let x: _ = true").Ok().LocalOk("x", "Bool");
            Funct("let x: _ = 0").Ok().LocalOk("x", "Int");

            Funct("let x; x = true").Ok().LocalOk("x", "Bool");
            Funct("let x; x = 0").Ok().LocalOk("x", "Int");

            Funct("let x: _; x = true").Ok().LocalOk("x", "Bool");
            Funct("let x: _; x = 0").Ok().LocalOk("x", "Int");

            Funct("let x = *0").Ok().LocalOk("x", "*Int");
            Funct("let x = *mut 0").Ok().LocalOk("x", "*mut Int");

            Funct("let x").Fail();
            Funct("let x: _").Fail();
        }


        [TestMethod]
        public void TestFunctDotOperator()
        {
            System.Func<string, Trapl.Core.Session> Funct = (src) =>
            {
                return Util.Compile(
                    "Data: struct { i: Int, b: Bool, p: *Int }" +
                    "test: fn() -> () { " + src + " }");
            };

            Funct("").Ok();

            Funct("let mut d = Data { i: 0, b: true, p: *0 }; d.i = 0").Ok();
            Funct("let mut d = Data { i: 0, b: true, p: *0 }; d.b = true").Ok();
            Funct("let mut d = Data { i: 0, b: true, p: *0 }; d.p = *0").Ok();
            Funct("let mut d = Data { i: 0, b: true, p: *0 }; d.z = 0").Fail();

            //Funct("let t: (Int, Int, Int); t.0 = 0; t.1 = 1; t.2 = 2").Ok();
            //Funct("let t: (Data); let x = 0; (t.0).i = 0; (t.0).b = true; (t.0).p = *x").Ok();
            //Funct("let t: (Data); let d: Data; let x = 0; d.i = 0; d.b = true; d.p = *x; t.0 = d").Ok();

            Funct("let i: Int; i.z = 0").Fail();
            Funct("let i: Int; i.0 = 0").Fail();
            //Funct("let t: (Int, Bool); t.2 = 0").Fail();
            //Funct("let t: (Int, Bool); t.i = 0").Fail();
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
                    "Data: struct { i: Int, b: Bool, p: *Int }" +
                    "test: fn() -> () { " + src + " }");
            };

            Funct("").Ok();

            Funct("let i: Int = 0; let j = i").Ok();
            Funct("let i: Int;     let j = i").Fail();

            Funct("let i: Int = 0; let p = *i").Ok();
            Funct("let i: Int;     let p = *i").Fail();

            Funct("let i: *Int = *0; let j = i").Ok();
            Funct("let i: *Int;      let j = i").Fail();

            Funct("let p: *Int = *0; let i = @p").Ok();
            Funct("let p: *Int;      let i = @p").Fail();

            Funct("let d = Data { i: 0, b: true, p: *0       }").Ok();
            Funct("let d = Data {       b: true, p: *0       }").Fail();
            Funct("let d = Data { i: 0,          p: *0       }").Fail();
            Funct("let d = Data { i: 0, b: true,             }").Fail();
            Funct("let d = Data {                            }").Fail();
            Funct("let d = Data { i: 0, b: true, p: *0, i: 1 }").Fail();
            Funct("let d = Data { i: 0, b: true, p: *0, z: 0 }").Fail();
            Funct("let d = Data {                       z: 0 }").Fail();

            Funct("let d = Data { i: 0, b: true, p: *0 }; let i =  d.i").Ok();
            Funct("let d: Data;                           let i =  d.i").Fail();

            Funct("let d = Data { i: 0, b: true, p: *0 }; let p = *d.i").Ok();
            Funct("let d: Data;                           let p = *d.i").Fail();

            Funct("let d = Data { i: 0, b: true, p: *0 }; let i = @(d.p)").Ok();
            Funct("let d: Data;                           let i = @(d.p)").Fail();

            Funct("let d = Data { i: 0, b: true, p: *0 }; let vi = @(d.p)").Ok();
            Funct("let d: Data;                           let vi = @(d.p)").Fail();

            Funct("let d = Data { i: 0, b: true, p: *0 }; let pd = *d;   let d2 = @pd").Ok();
            Funct("                                       let pd: *Data; let d2 = @pd").Fail();

            Funct("let d = Data { i: 0, b: true, p: *0 }; let pd = *d;   let i = @pd.i").Ok();
            Funct("                                       let pd: *Data; let i = @pd.i").Fail();

            Funct("let d = Data { i: 0, b: true, p: *0 }; let pd = *d;   let i = @(@pd.p)").Ok();
            Funct("                                       let pd: *Data; let i = @(@pd.p)").Fail();

            Funct("let i: Int; if true { i = 1 } else { i = 0 }; let j = i").Ok();
            Funct("let i: Int; if true { i = 1 } else {       }; let j = i").Fail();
            Funct("let i: Int; if true {       } else { i = 0 }; let j = i").Fail();

            Funct("let i: Int; if true { i = 1; let j = i } else { i = 0; let j = i }").Ok();
            Funct("let i: Int; if true { i = 1; let j = i } else {                  }").Ok();
            Funct("let i: Int; if true {                  } else { i = 0; let j = i }").Ok();
            Funct("let i: Int; if true { i = 1; let j = i } else {        let j = i }").Fail();
            Funct("let i: Int; if true {        let j = i } else { i = 0; let j = i }").Fail();
        }


        [TestMethod]
        public void TestFunctMutChecker()
        {
            System.Func<string, Trapl.Core.Session> Funct = (src) =>
            {
                return Util.Compile(
                    "Data: struct { i: Int, b: Bool, p: *Int }" +
                    "Ptr: struct { p: *Int }" +
                    "PtrMut: struct { p: *mut Int }" +
                    "test: fn() -> () { " + src + " }");
            };

            Funct("").Ok();

            Funct("let     i = 0").Ok();
            Funct("let mut i = 0").Ok();

            Funct("let     i;     i = 0").Ok();
            Funct("let mut i;     i = 1").Ok();
            Funct("let mut i = 0; i = 1").Ok();

            Funct("let     i = 0; i = 1").Fail();

            Funct("let     i = *0").Ok();
            Funct("let mut i = *0; i = *1").Ok();
            Funct("let     i = *0; i = *1").Fail();
            Funct("let     i = *0; @i = 1").Fail();
            Funct("let mut i = *0; @i = 1").Fail();

            Funct("let     i = *mut 0").Ok();
            Funct("let mut i = *mut 0; i = *mut 1").Ok();
            Funct("let     i = *mut 0; @i = 1").Ok();
            Funct("let mut i = *mut 0; @i = 1").Ok();

            Funct("let     d = Data { i: 0, b: true, p: *0 }").Ok();
            Funct("let mut d = Data { i: 0, b: true, p: *0 }; d.i = 1").Ok();
            Funct("let mut d = Data { i: 0, b: true, p: *0 }; @(d.p) = 1").Fail();
            Funct("let     d = Data { i: 0, b: true, p: *0 }; d.i = 1").Fail();
            Funct("let     d = Data { i: 0, b: true, p: *0 }; @(d.p) = 1").Fail();

            Funct("let     p = Ptr    { p: *    0 }").Ok();
            Funct("let     p = PtrMut { p: *mut 0 }").Ok();
            Funct("let     p = Ptr    { p: *mut 0 }").Ok();
            Funct("let     p = PtrMut { p: *    0 }").Fail();

            Funct("let     p = PtrMut { p: *mut 0 }; @(p.p) = 1").Ok();
            Funct("let mut p = PtrMut { p: *mut 0 }; @(p.p) = 1").Ok();
            Funct("let     p = Ptr    { p: *    0 }; @(p.p) = 1").Fail();
            Funct("let mut p = Ptr    { p: *    0 }; @(p.p) = 1").Fail();

        }
    }
}
