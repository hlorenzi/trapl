using System;
using Trapl.Core;
using Trapl.Grammar;
using Trapl.Extraction;


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

                var extracted = Extractor.Extract(topLevelNode);

                foreach (var st in extracted.structs)
                    st.PrintToConsole();

                foreach (var fn in extracted.functs)
                    fn.PrintToConsole();
            }

            session.PrintMessagesToConsole();
            Console.ReadKey();
        }
    }
}
