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
            ShouldPass("Test: struct { }");
            ShouldPass("Test: struct { x: Int8 }");
            ShouldPass("Test: struct { x: Int8, y: Int8 }");
            ShouldPass("Test: struct { x: Int8, y: Int16, }");
            ShouldPass("Test1: struct { x: Test2 } Test2: struct { x: Int8 }");
            ShouldPass("Test1: struct { x: Int8 } Test2: struct { x: Test1 }");

            ShouldFail("Test: struct { x: UnknownType }");
            //ShouldFail("RepeatedMembers: struct { x: Int8, x: Int16 }");
            ShouldFail("Recursive: struct { x: Recursive }");
            ShouldFail("Recursive1: struct { x: Recursive2 } Recursive2: struct { x: Recursive1 }");
            ShouldFail("Recursive1: struct { x: Recursive2 } Recursive2: struct { x: Recursive3 } Recursive3: struct { x: Recursive1 }");
            ShouldFail("Recursive1: struct { x: Recursive3 } Recursive2: struct { x: Recursive1 } Recursive3: struct { x: Recursive2 }");
            //ShouldFail("VoidMember: struct { x: Void }");
            //ShouldFail("SameName: struct { } SameName: struct { }");
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

            Assert.IsTrue(session.diagn.ContainsNoError());

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
