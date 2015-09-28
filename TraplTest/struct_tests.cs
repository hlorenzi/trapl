using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TraplTest
{
    [TestClass]
    public class StructTests
    {
        [TestMethod]
        public void TestStructMembers()
        {
            ShouldPass("Test { }");
            ShouldPass("Test { x: Int8 }");
            ShouldPass("Test { x: Int8, y: Int16 }");
            ShouldPass("Test1 { x: Test2 } Test2 { x: Int8 }");
            ShouldPass("Test1 { x: Int8 } Test2 { x: Test1 }");

            ShouldFail("Test { x: UnknownType }");
            ShouldFail("DuplicateMembers { x: Int8, x: Int16 }");
            ShouldFail("DuplicateMembers { x: Int8, y: Int16, x: Int32 }");
            ShouldFail("Recursive { x: Recursive }");
            ShouldFail("Recursive1 { x: Recursive2 } Recursive2 { x: Recursive1 }");
            ShouldFail("Recursive1 { x: Recursive2 } Recursive2 { x: Recursive3 } Recursive3 { x: Recursive1 }");
            ShouldFail("Recursive1 { x: Recursive3 } Recursive2 { x: Recursive1 } Recursive3 { x: Recursive2 }");
            ShouldFail("VoidMember { x: Void }");
            ShouldFail("VoidMember { x: Int32, y: Void }");
            //ShouldFail("SameName { } SameName { }");
        }


        private void ShouldPass(string sourceStr)
        {
            Assert.IsTrue(CompileAndTest(sourceStr));
        }


        private void ShouldFail(string sourceStr)
        {
            Assert.IsFalse(CompileAndTest(sourceStr));
        }


        private bool CompileAndTest(string sourceStr)
        {
            var session = new Trapl.Interface.Session();
            var src = Trapl.Interface.SourceCode.MakeFromString(sourceStr);

            var tokens = Trapl.Grammar.Tokenizer.Tokenize(session, src);
            var ast = Trapl.Grammar.ASTParser.Parse(session, tokens);

            Trapl.Semantics.CheckTopDecl.Check(session, ast, src);

            if (session.diagn.ContainsErrors())
                Assert.Inconclusive();

            var topDeclClones = new List<Trapl.Semantics.TopDecl>(session.topDecls);
            foreach (var topDecl in topDeclClones)
            {
                try { topDecl.Resolve(session); }
                catch (Trapl.Semantics.CheckException) { }
            }

            return session.diagn.ContainsNoError();
        }
    }
}
