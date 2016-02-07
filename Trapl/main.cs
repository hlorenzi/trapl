﻿using System;
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
            var input = TextInput.MakeFromFile("../../test.tr");
            var tokens = Tokenizer.Tokenize(session, input);
            var topLevelNode = ASTParser.Parse(session, tokens);

            if (topLevelNode != null)
            {
                //topLevelNode.PrintDebugRecursive("");

                var resolver = new DeclResolver(session);
                resolver.ResolveTopLevelDeclGroup(topLevelNode);
                resolver.ResolveStructFields();
                resolver.ResolveFunctHeaders();
                resolver.ResolveFunctBodies();
                session.PrintDeclsToConsole(true);
            }

            session.PrintMessagesToConsole();
            Console.ReadKey();
        }
    }
}
