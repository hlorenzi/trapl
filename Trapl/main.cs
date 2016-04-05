using System;
using Trapl.Core;
using Trapl.Grammar;
using Trapl.Semantics;


namespace Trapl
{
    class Program
    {
        static void Main(string[] args)
        {
            var session = new Session();
            session.PrimitiveBool = session.CreateStruct(Name.FromPath("Bool"), null);
            session.PrimitiveInt = session.CreateStruct(Name.FromPath("Int"), null);
            session.PrimitiveUInt = session.CreateStruct(Name.FromPath("UInt"), null);

            var input = TextInput.MakeFromFile("../../test.tr");
            var tokens = Tokenizer.Tokenize(session, input);
            var topLevelNode = ASTParser.Parse(session, tokens);

            if (topLevelNode != null)
            {
                //topLevelNode.PrintDebugRecursive("");

                var resolver = new DeclResolver(session);
                resolver.ResolveTopLevelDeclGroup(topLevelNode);
                resolver.ResolveStructFields();
                if (!StructRecursionChecker.Check(session))
                {
                    resolver.ResolveFunctHeaders();
                    resolver.ResolveFunctBodies();
                    session.PrintDeclsToConsole(true);
                }
            }

            session.PrintMessagesToConsole();
            Console.ReadKey();
        }
    }
}
