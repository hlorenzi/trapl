using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TraplTest
{
    [TestClass]
    public class SyntaxTests : Base
    {
        [TestMethod]
        public void TestTopDeclSyntax()
        {
            ContainsNoError(ParseGrammar(""));

            ContainsNoError(ParseGrammar("Test { }"));
            ContainsNoError(ParseGrammar("Test { x: A }"));
            ContainsNoError(ParseGrammar("Test { x: A, }"));
            ContainsNoError(ParseGrammar("Test { x: A, y: B }"));
            ContainsNoError(ParseGrammar("Test { x: A, y: B, }"));
            ContainsNoError(ParseGrammar("Test1 { x: A } Test2 { x: B }"));
            ContainsNoError(ParseGrammar("Test<T> { x: A, y: B }"));

            ContainsNoError(ParseGrammar("test(){}"));
            ContainsNoError(ParseGrammar("test() { }"));
            ContainsNoError(ParseGrammar("test() -> C { }"));
            ContainsNoError(ParseGrammar("test(x: A) { }"));
            ContainsNoError(ParseGrammar("test(x: A,) { }"));
            ContainsNoError(ParseGrammar("test(x: A, y: B) { }"));
            ContainsNoError(ParseGrammar("test(x: A, y: B,) { }"));
            ContainsNoError(ParseGrammar("test(x: A, y: B) -> C { }"));
            ContainsNoError(ParseGrammar("test(x: A, y: B,) -> C { }"));
            ContainsNoError(ParseGrammar("test<T>(x: A, y: B) -> C { }"));

            ContainsErrors(ParseGrammar("{ }"));
            ContainsErrors(ParseGrammar("Test"));
            ContainsErrors(ParseGrammar("Test { "));
            ContainsErrors(ParseGrammar("Test: { }"));
            ContainsErrors(ParseGrammar("123 { }"));
            ContainsErrors(ParseGrammar("Test1 { }, Test2 { }"));
            ContainsErrors(ParseGrammar("Test1 { }; Test2 { }"));
            ContainsErrors(ParseGrammar("Test { , }"));
            ContainsErrors(ParseGrammar("Test { x: A y: B }"));
            ContainsErrors(ParseGrammar("Test { x: A, y: B,, }"));
            ContainsErrors(ParseGrammar("Test { x: A,, y: B }"));

            ContainsErrors(ParseGrammar("test()"));
            ContainsErrors(ParseGrammar("test() {"));
            ContainsErrors(ParseGrammar("test() ->"));
            ContainsErrors(ParseGrammar("test() -> { }"));
            ContainsErrors(ParseGrammar("test -> C { }"));
            ContainsErrors(ParseGrammar("test(x A) { }"));
            ContainsErrors(ParseGrammar("test(x) { }"));
            ContainsErrors(ParseGrammar("test(x:) { }"));
            ContainsErrors(ParseGrammar("test(x: A,, y: B) { }"));
            ContainsErrors(ParseGrammar("test(x: A, y: B,,) { }"));
            ContainsErrors(ParseGrammar("test() - > C { }"));
            ContainsErrors(ParseGrammar("test() -> C ()"));
        }
    }
}
