using System;
using Trapl.Infrastructure;


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

            var primitiveInt = session.CreateStruct(Name.FromPath("Int"));
            session.AddStructField(primitiveInt, Name.FromPath("x"), new TypeStruct(primitiveInt));
            session.CreateStruct(Name.FromPath("Int", "One", "More"));
            session.CreateStruct(Name.FromPath("Float", "ThirtyTwo"));
            session.CreateStruct(Name.FromPath("Float"));
            session.CreateStruct(Name.FromPath("Int", "Two"));
            session.CreateStruct(Name.FromPath("Int", "One"));
            session.CreateStruct(Name.FromPath("Float", "Double"));

            session.PrintDeclsToConsole(true);
            session.PrintMessagesToConsole();
            Console.ReadKey();
        }
    }
}
