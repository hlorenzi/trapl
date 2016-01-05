using System;


namespace Trapl
{
    class Program
    {
        static void Main(string[] args)
        {
            var session = new Infrastructure.Session();
            var input = Infrastructure.TextInput.MakeFromFile("../../test.tr");
            var tokens = Grammar.Tokenizer.Tokenize(session, input);
            var astParser = new Grammar.ASTParser(session, tokens);

            try
            {
                var topLevelNode = astParser.ParseTopLevel();
                topLevelNode.PrintDebugRecursive("");
            }
            catch (Infrastructure.CheckException) { }

            session.PrintMessagesToConsole();
            Console.ReadKey();
        }
    }
}
