using System;
using Trapl.Core;
using Trapl.Grammar;


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

                var converter = new CoreConverter(session);
                converter.ConvertTopLevelDeclGroup(topLevelNode);
                converter.ConvertStructFields();
                session.PrintDeclsToConsole(true);
            }

            session.PrintMessagesToConsole();
            Console.ReadKey();
        }
    }
}
