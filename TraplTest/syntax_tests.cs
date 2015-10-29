using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TraplTest
{
    [TestClass]
    public class SyntaxTests
    {
        [TestMethod]
        public void TestTopDeclSyntax()
        {
            ShouldPassSyntax("");

            ShouldPassSyntax("Test { }");
            ShouldPassSyntax("Test { x: A }");
            ShouldPassSyntax("Test { x: A, }");
            ShouldPassSyntax("Test { x: A, y: B }");
            ShouldPassSyntax("Test { x: A, y: B, }");
            ShouldPassSyntax("Test1 { x: A } Test2 { x: B }");
            ShouldPassSyntax("Test<T> { x: A, y: B }");

            ShouldPassSyntax("test(){}");
            ShouldPassSyntax("test() { }");
            ShouldPassSyntax("test() -> C { }");
            ShouldPassSyntax("test(x: A) { }");
            ShouldPassSyntax("test(x: A,) { }");
            ShouldPassSyntax("test(x: A, y: B) { }");
            ShouldPassSyntax("test(x: A, y: B,) { }");
            ShouldPassSyntax("test(x: A, y: B) -> C { }");
            ShouldPassSyntax("test(x: A, y: B,) -> C { }");
            ShouldPassSyntax("test<T>(x: A, y: B) -> C { }");

            ShouldFailSyntax("{ }");
            ShouldFailSyntax("Test");
            ShouldFailSyntax("Test { ");
            ShouldFailSyntax("Test: { }");
            ShouldFailSyntax("123 { }");
            ShouldFailSyntax("Test1 { }, Test2 { }");
            ShouldFailSyntax("Test1 { }; Test2 { }");
            ShouldFailSyntax("Test { , }");
            ShouldFailSyntax("Test { x: A y: B }");
            ShouldFailSyntax("Test { x: A, y: B,, }");
            ShouldFailSyntax("Test { x: A,, y: B }");

            ShouldFailSyntax("test()");
            ShouldFailSyntax("test() {");
            ShouldFailSyntax("test() ->");
            ShouldFailSyntax("test() -> { }");
            ShouldFailSyntax("test -> C { }");
            ShouldFailSyntax("test(x A) { }");
            ShouldFailSyntax("test(x) { }");
            ShouldFailSyntax("test(x:) { }");
            ShouldFailSyntax("test(x: A,, y: B) { }");
            ShouldFailSyntax("test(x: A, y: B,,) { }");
            ShouldFailSyntax("test() - > C { }");
            ShouldFailSyntax("test() -> C ()");
        }


        private void ShouldPassSyntax(string sourceStr)
        {
            var session = new Trapl.Infrastructure.Session();
            session.AddUnit(Trapl.Infrastructure.Unit.MakeFromString(sourceStr));
            if (session.diagn.ContainsErrors())
                session.diagn.PrintToConsole(session);
            Assert.IsTrue(session.diagn.ContainsNoError());
        }


        private void ShouldFailSyntax(string sourceStr)
        {
            var session = new Trapl.Infrastructure.Session();
            session.AddUnit(Trapl.Infrastructure.Unit.MakeFromString(sourceStr));
            Assert.IsTrue(session.diagn.ContainsErrors());
        }
    }
}
