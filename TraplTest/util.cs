using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TraplTest
{
    public static class Util
    {
        public static Trapl.Core.Session Compile(string src)
        {
            var session = new Trapl.Core.Session();
            session.PrimitiveBool = session.CreateStruct(Trapl.Core.Name.FromPath("Bool"));
            session.PrimitiveInt = session.CreateStruct(Trapl.Core.Name.FromPath("Int"));
            session.PrimitiveUInt = session.CreateStruct(Trapl.Core.Name.FromPath("UInt"));

            var input = Trapl.Core.TextInput.MakeFromString(src);
            var tokens = Trapl.Grammar.Tokenizer.Tokenize(session, input);
            var topLevelNode = Trapl.Grammar.ASTParser.Parse(session, tokens);

            if (session.HasInternalErrors())
                Assert.Inconclusive("Internal compiler error.");

            if (session.HasErrors() || topLevelNode == null)
                Assert.Inconclusive("Syntax error.");

            var resolver = new Trapl.Semantics.DeclResolver(session);
            resolver.ResolveTopLevelDeclGroup(topLevelNode);
            resolver.ResolveStructFields();
            resolver.ResolveFunctHeaders();
            resolver.ResolveFunctBodies();

            return session;
        }


        public static Trapl.Core.Session Ok(this Trapl.Core.Session session)
        {
            if (session.HasInternalErrors())
                Assert.Inconclusive("Internal compiler error.");

            Assert.IsFalse(session.HasErrors(), "Compilation encountered errors, but none were expected.");
            return session;
        }


        public static Trapl.Core.Session Fail(this Trapl.Core.Session session)
        {
            if (session.HasInternalErrors())
                Assert.Inconclusive("Internal compiler error.");

            Assert.IsTrue(session.HasErrors(), "Compilation encountered no errors, but some were expected.");
            return session;
        }


        public static Trapl.Core.Session LocalTypeOk(this Trapl.Core.Session session, string fnName, string localName, string typeName)
        {
            var name = Trapl.Core.Name.FromPath(localName);
            var type = Util.ResolveType(session, typeName);
            var funct = session.GetFunct(
                session.GetDeclsWithUseDirectives(
                    Trapl.Core.Name.FromPath(fnName),
                    true,
                    null)[0].index);
            var binding = funct.localBindings.Find(b => b.name.Compare(name));
            Assert.IsTrue(funct.registerTypes[binding.registerIndex].IsSame(type),
                "Local type mismatch. Expecting '" +
                type.GetString(session) + "', got '" +
                funct.registerTypes[binding.registerIndex].GetString(session) + "'.");
            return session;
        }


        public static Trapl.Core.Type ResolveType(Trapl.Core.Session session, string typeName)
        {
            var input = Trapl.Core.TextInput.MakeFromString(typeName);
            var tokens = Trapl.Grammar.Tokenizer.Tokenize(session, input);

            var astParser = new Trapl.Grammar.ASTParser(session, tokens);
            var typeNode = astParser.ParseType();

            var type = Trapl.Semantics.TypeResolver.Resolve(session, typeNode, null, false);
            if (type.IsError())
                Assert.Inconclusive("Type resolve error.");

            return type;
        }
    }
}
