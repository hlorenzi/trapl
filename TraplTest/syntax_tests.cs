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

            ContainsNoError(ParseGrammar("struct Test { }"));
            ContainsNoError(ParseGrammar("struct Test { x: A }"));
            ContainsNoError(ParseGrammar("struct Test { x: A, }"));
            ContainsNoError(ParseGrammar("struct Test { x: A, y: B }"));
            ContainsNoError(ParseGrammar("struct Test { x: A, y: B, }"));
            ContainsNoError(ParseGrammar("struct Test1 { x: A } struct Test2 { x: B }"));
            ContainsNoError(ParseGrammar("struct Test<T> { x: A, y: B }"));
            ContainsNoError(ParseGrammar("struct Test::<T> { x: A, y: B }"));
            ContainsNoError(ParseGrammar("struct Test::Test<T> { x: A, y: B }"));
            ContainsNoError(ParseGrammar("struct Test::Test::<T> { x: A, y: B }"));

            ContainsNoError(ParseGrammar("fn test(){}"));
            ContainsNoError(ParseGrammar("fn test() { }"));
            ContainsNoError(ParseGrammar("fn test() -> C { }"));
            ContainsNoError(ParseGrammar("fn test(x: A) { }"));
            ContainsNoError(ParseGrammar("fn test(x: A,) { }"));
            ContainsNoError(ParseGrammar("fn test(x: A, y: B) { }"));
            ContainsNoError(ParseGrammar("fn test(x: A, y: B,) { }"));
            ContainsNoError(ParseGrammar("fn test(x: A, y: B) -> C { }"));
            ContainsNoError(ParseGrammar("fn test(x: A, y: B,) -> C { }"));
            ContainsNoError(ParseGrammar("fn test<T>(x: A, y: B) -> C { }"));
            ContainsNoError(ParseGrammar("fn test::<T>(x: A, y: B) -> C { }"));
            ContainsNoError(ParseGrammar("fn Test::test<T>(x: A, y: B) -> C { }"));
            ContainsNoError(ParseGrammar("fn Test::test::<T>(x: A, y: B) -> C { }"));

            ContainsErrors(ParseGrammar("test"));
            ContainsErrors(ParseGrammar("test {}"));
            ContainsErrors(ParseGrammar("test() {}"));

            ContainsErrors(ParseGrammar("struct"));
            ContainsErrors(ParseGrammar("struct {"));
            ContainsErrors(ParseGrammar("struct { }"));
            ContainsErrors(ParseGrammar("struct Test"));
            ContainsErrors(ParseGrammar("struct Test { "));
            ContainsErrors(ParseGrammar("struct Test: { }"));
            ContainsErrors(ParseGrammar("struct 123 { }"));
            ContainsErrors(ParseGrammar("struct Test1 { } Test2 { }"));
            ContainsErrors(ParseGrammar("struct Test1 { }, struct Test2 { }"));
            ContainsErrors(ParseGrammar("struct Test1 { }; struct Test2 { }"));
            ContainsErrors(ParseGrammar("struct Test { , }"));
            ContainsErrors(ParseGrammar("struct Test { x: A y: B }"));
            ContainsErrors(ParseGrammar("struct Test { x: A, y: B,, }"));
            ContainsErrors(ParseGrammar("struct Test { x: A,, y: B }"));
            ContainsErrors(ParseGrammar("struct Test< { x: A,, y: B }"));
            ContainsErrors(ParseGrammar("struct Test<T { x: A,, y: B }"));
            ContainsErrors(ParseGrammar("struct Test<"));
            ContainsErrors(ParseGrammar("struct Test<T"));

            ContainsErrors(ParseGrammar("fn"));
            ContainsErrors(ParseGrammar("fn test"));
            ContainsErrors(ParseGrammar("fn test("));
            ContainsErrors(ParseGrammar("fn test()"));
            ContainsErrors(ParseGrammar("fn test() {"));
            ContainsErrors(ParseGrammar("fn test() ->"));
            ContainsErrors(ParseGrammar("fn test() -> { }"));
            ContainsErrors(ParseGrammar("fn test -> C { }"));
            ContainsErrors(ParseGrammar("fn test(x A) { }"));
            ContainsErrors(ParseGrammar("fn test(x) { }"));
            ContainsErrors(ParseGrammar("fn test(x:) { }"));
            ContainsErrors(ParseGrammar("fn test(x: A,, y: B) { }"));
            ContainsErrors(ParseGrammar("fn test(x: A, y: B,,) { }"));
            ContainsErrors(ParseGrammar("fn test() - > C { }"));
            ContainsErrors(ParseGrammar("fn test() -> C ()"));
            ContainsErrors(ParseGrammar("fn test<"));
            ContainsErrors(ParseGrammar("fn test<T"));
            ContainsErrors(ParseGrammar("fn test<>"));
            ContainsErrors(ParseGrammar("fn test<>()"));
            ContainsErrors(ParseGrammar("fn test<() {}"));
            ContainsErrors(ParseGrammar("fn test<T() {}"));
        }


        public void TestNumberSyntax()
        {
            ContainsNoError(ParseTokens("0"));
            ContainsNoError(ParseTokens("1"));
            ContainsNoError(ParseTokens("1_"));
            ContainsNoError(ParseTokens("0123456789"));
            ContainsNoError(ParseTokens("1234567890"));
            ContainsNoError(ParseTokens("1_000_000_000"));
            ContainsNoError(ParseTokens("1__000__000__000"));
            ContainsNoError(ParseTokens("0b0"));
            ContainsNoError(ParseTokens("0b1"));
            ContainsNoError(ParseTokens("0b01"));
            ContainsNoError(ParseTokens("0b01010101"));
            ContainsNoError(ParseTokens("0b1100_0110_0101"));
            ContainsNoError(ParseTokens("0b_1100_0110_0101"));
            ContainsNoError(ParseTokens("0o0"));
            ContainsNoError(ParseTokens("0o1"));
            ContainsNoError(ParseTokens("0o10"));
            ContainsNoError(ParseTokens("0o01234567"));
            ContainsNoError(ParseTokens("0o567_345_234"));
            ContainsNoError(ParseTokens("0o_567_345_234"));
            ContainsNoError(ParseTokens("0x0"));
            ContainsNoError(ParseTokens("0x1"));
            ContainsNoError(ParseTokens("0x10"));
            ContainsNoError(ParseTokens("0x0123456789abcdef"));
            ContainsNoError(ParseTokens("0x0123456789ABCDEF"));
            ContainsNoError(ParseTokens("0x0123456789aBcDeF"));
            ContainsNoError(ParseTokens("0x_0123456789abcdef"));
            ContainsNoError(ParseTokens("0xff_c4_a8_35"));

            ContainsErrors(ParseTokens("0r0"));
            ContainsErrors(ParseTokens("0x0x0"));
            ContainsErrors(ParseTokens("1x123"));
            ContainsErrors(ParseTokens("0_x123abc"));
            ContainsErrors(ParseTokens("123abc"));
            ContainsErrors(ParseTokens("123xyz"));
            ContainsErrors(ParseTokens("0b012"));
            ContainsErrors(ParseTokens("0b012abc"));
            ContainsErrors(ParseTokens("0o678"));
            ContainsErrors(ParseTokens("0o678abc"));
            ContainsErrors(ParseTokens("0x123cdfg"));
        }
    }
}
