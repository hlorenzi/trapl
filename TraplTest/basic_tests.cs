using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trapl.Diagnostics;


namespace TraplTest
{
    [TestClass]
    public class BasicTests
    {
        [TestMethod]
        public void StructuralTests()
        {
            ShouldSucceed("");

            ShouldSucceed("Test: struct { }");
            ShouldSucceed("Test: struct { x: Int8 }");
            ShouldSucceed("Test: struct { x: Int8, y: Int8 }");
            ShouldSucceed("Test: struct { x: Int8, y: Int16, }");
            ShouldSucceed("Test1: struct { x: Test2 } Test2: struct { x: Int8 }");
            ShouldSucceed("Test1: struct { x: Int8 } Test2: struct { x: Test1 }");

            GrammarShouldFail("{ }");
            GrammarShouldFail("test");
            GrammarShouldFail("test { }");
            GrammarShouldFail("test:");
            GrammarShouldFail("test: { }");
            GrammarShouldFail("test: test { }");
            GrammarShouldFail("test: 123 { }");
            GrammarShouldFail("123: struct { }");
            GrammarShouldFail(": struct { }");
            GrammarShouldFail("test: funct() { }; Test2: struct { }");

            GrammarShouldFail("struct { }");
            GrammarShouldFail("Test struct { }");
            GrammarShouldFail("Test: struct");
            GrammarShouldFail("Test: struct {");
            GrammarShouldFail("Test: struct { x: Int8 y: Int8 }");

            GrammarShouldFail("funct() { }");
            GrammarShouldFail("test funct() { }");
            GrammarShouldFail("test: funct");
            GrammarShouldFail("test: funct(");
            GrammarShouldFail("test: funct { }");
            GrammarShouldFail("test: funct()");
            GrammarShouldFail("test: funct() {");
            GrammarShouldFail("test: funct( -> { }");
            GrammarShouldFail("test: funct( -> ) { }");
            GrammarShouldFail("test: funct() { }; Test2: struct { }");
            GrammarShouldFail("test: funct(x: Int8 y: Int8) { }");
            GrammarShouldFail("test: funct(x: Int8, y: Int8 -> ) { }");
            GrammarShouldFail("test: funct(x: Int8, y: Int8 - > Int8) { return 0; }");

            SemanticsShouldFail("Test: struct { x: UnknownType }");
            //SemanticsShouldFail("RepeatedMembers: struct { x: Int8, x: Int16 }");
            SemanticsShouldFail("Recursive: struct { x: Recursive }");
            SemanticsShouldFail("Recursive1: struct { x: Recursive2 } Recursive2: struct { x: Recursive1 }");
            SemanticsShouldFail("Recursive1: struct { x: Recursive2 } Recursive2: struct { x: Recursive3 } Recursive3: struct { x: Recursive1 }");
            SemanticsShouldFail("Recursive1: struct { x: Recursive3 } Recursive2: struct { x: Recursive1 } Recursive3: struct { x: Recursive2 }");
            //SemanticsShouldFail("VoidMember: struct { x: Void }");
            //SemanticsShouldFail("SameName: struct { } SameName: struct { }");
        }


        delegate string EmbedDelegate(string str);


        private void ShouldSucceed(string str)
        {
            try
            {
                var src = Trapl.Interface.SourceCode.MakeFromString(str);
                var session = new Trapl.Interface.Session();
                session.diagn = new Trapl.Diagnostics.Collection();

                var tokenCollection = Trapl.Grammar.Tokenizer.Tokenize(session, src);
                var ast = Trapl.Grammar.ASTParser.Parse(session, tokenCollection, src);

                Trapl.Semantics.CheckTopDecl.Check(session, ast, src);

                if (session.diagn.HasNoError())
                {
                    var topDeclClones = new List<Trapl.Semantics.TopDecl>(session.topDecls);
                    foreach (var topDecl in topDeclClones)
                    {
                        try { topDecl.Resolve(session); }
                        catch (Trapl.Semantics.CheckException) { }
                    }
                }

                Assert.IsTrue(session.diagn.HasNoError());
            }
            catch (Exception)
            {
                Assert.Inconclusive();
            }
        }


        private void GrammarShouldFail(string str)
        {
            try
            {
                var src = Trapl.Interface.SourceCode.MakeFromString(str);
                var session = new Trapl.Interface.Session();
                session.diagn = new Trapl.Diagnostics.Collection();

                var tokenCollection = Trapl.Grammar.Tokenizer.Tokenize(session, src);
                var ast = Trapl.Grammar.ASTParser.Parse(session, tokenCollection, src);

                Assert.IsTrue(session.diagn.HasErrors());
            }
            catch (Exception)
            {
                Assert.Inconclusive();
            }
        }


        private void SemanticsShouldFail(string str)
        {
            try
            {
                var src = Trapl.Interface.SourceCode.MakeFromString(str);
                var session = new Trapl.Interface.Session();
                session.diagn = new Trapl.Diagnostics.Collection();

                var tokenCollection = Trapl.Grammar.Tokenizer.Tokenize(session, src);
                var ast = Trapl.Grammar.ASTParser.Parse(session, tokenCollection, src);

                Trapl.Semantics.CheckTopDecl.Check(session, ast, src);

                if (session.diagn.HasNoError())
                {
                    var topDeclClones = new List<Trapl.Semantics.TopDecl>(session.topDecls);
                    foreach (var topDecl in topDeclClones)
                    {
                        try { topDecl.Resolve(session); }
                        catch (Trapl.Semantics.CheckException) { }
                    }
                }

                Assert.IsTrue(session.diagn.HasErrors());
            }
            catch (Exception)
            {
                Assert.Inconclusive();
            }
        }
    }
}
