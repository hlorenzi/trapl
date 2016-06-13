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
            session.PrimitiveBool = session.CreatePrimitiveStruct(Name.FromPath("Bool"));
            session.PrimitiveInt = session.CreatePrimitiveStruct(Name.FromPath("Int"));
            session.PrimitiveUInt = session.CreatePrimitiveStruct(Name.FromPath("UInt"));

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

                    Console.WriteLine("==== CODEGEN ====");
                    Console.WriteLine(Codegen.CGenerator.Generate(session));
                }
            }

            session.PrintMessagesToConsole();
            Console.ReadKey();
        }
    }
}
