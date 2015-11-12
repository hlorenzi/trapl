using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TraplTest
{
    [TestClass]
    public class FunctTests : Base
    {
        [TestMethod]
        public void TestFunctHeaders()
        {
            ContainsNoError(Compile("fn test() {}"));
            ContainsNoError(Compile("fn test() -> () {}"));
            ContainsNoError(Compile("fn test(x: ()) -> () {}"));

            ContainsErrors(Compile("fn error_ret() -> UnknownType {}"));
            ContainsErrors(Compile("fn error_arg(x: UnknownType) {}"));
        }


        [TestMethod]
        public void TestTypeInference()
        {
            var supportCode =
                "struct Apple {}" +
                "struct Banana {}" +

                "struct List<Apple> {}" +
                "struct List<Banana> {}" +

                "struct Queue<Apple> {}" +
                "struct Queue<Banana> {}" +

                "fn get_nothing() {}" +
                "fn get_apple() -> Apple { Apple {} }" +
                "fn get_banana() -> Banana { Banana {} }" +

                "fn take<Apple>(a: Apple) {}" +
                "fn take<Banana>(b: Banana) {}" +

                "fn push<Apple>(list: List<Apple>, item: Apple) {}" +
                "fn push<Banana>(list: List<Banana>, item: Banana) {}" +
                "fn push<Apple>(queue: Queue<Apple>, item: Apple) {}" +
                "fn push<Banana>(queue: Queue<Banana>, item: Banana) {}";

            ForEach
            (
                (str) =>
                {
                    var session = Compile(supportCode + "fn test() {" + str + "}");

                    if (session.diagn.ContainsErrors())
                        session.diagn.PrintToConsole(session);

                    Assert.IsTrue(session.diagn.ContainsNoError());
                    Assert.IsTrue(CheckLocalType(session, "a", "Apple"));
                    Assert.IsTrue(CheckLocalType(session, "b", "Banana"));
                    Assert.IsTrue(CheckLocalType(session, "ra", "&Apple"));
                    Assert.IsTrue(CheckLocalType(session, "rb", "&Banana"));
                    Assert.IsTrue(CheckLocalType(session, "la", "List<Apple>"));
                    Assert.IsTrue(CheckLocalType(session, "lb", "List<Banana>"));
                    Assert.IsTrue(CheckLocalType(session, "qa", "Queue<Apple>"));
                    Assert.IsTrue(CheckLocalType(session, "qb", "Queue<Banana>"));
                    Assert.IsTrue(CheckLocalType(session, "n", "()"));
                },

                "let a: Apple",
                "let b: Banana",
                "let ra: &Apple",
                "let rb: &Banana",
                "let la: List<Apple>",
                "let lb: List<Banana>",
                "let n: ()",

                "let a; a = Apple {}",
                "let b; b = Banana {}",
                "let a = Apple {}",
                "let b = Banana {}",

                "let a; a = get_apple()",
                "let b; b = get_banana()",
                "let n; n = get_nothing()",
                "let a = get_apple()",
                "let b = get_banana()",
                "let n = get_nothing()",
                "let a; let z; z = Apple {}; a = z",

                "let a: Apple; let ra = &a",
                "let b: Banana; let rb = &b",
                "let a; let ra: &Apple = &a",
                "let b; let rb: &Banana = &b",
                "let a: Apple; let ra; a = @ra",
                "let b: Banana; let rb; b = @rb",
                "let a; let ra: &Apple; a = @ra",
                "let b; let rb: &Banana; b = @rb",

                "let a: Apple; let la: List; push(la, a)",
                "let b: Banana; let lb: List; push(lb, b)",
                "let a: Apple; let qa: Queue; push(qa, a)",
                "let b: Banana; let qb: Queue; push(qb, b)",
                "let a: Apple; let la: List<_>; push(la, a)",
                "let b: Banana; let lb: List<_>; push(lb, b)",
                "let a: Apple; let qa: Queue<_>; push(qa, a)",
                "let b: Banana; let qb: Queue<_>; push(qb, b)",
                "let a; let la: List<Apple>; push(la, a)",
                "let b; let lb: List<Banana>; push(lb, b)",
                "let a; let qa: Queue<Apple>; push(qa, a)",
                "let b; let qb: Queue<Banana>; push(qb, b)"
            );

            ForEach
            (
                (str) =>
                {
                    var session = Compile(supportCode + "fn test() {" + str + "}");

                    Assert.IsTrue(session.diagn.ContainsErrors());
                },

                "let a",
                "let a: _",

                "let a: Apple; let la; push(la, a)",
                "let b: Banana; let lb; push(lb, b)",

                "let a: Apple; let ra: &Apple = &&a",
                "let a: Apple; let ra: &Apple = @@a"
            );
        }
    }
}
