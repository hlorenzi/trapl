using System;


namespace Trapl
{
    class Program
    {
        static void Main(string[] args)
        {
            var src = SourceCode.MakeFromFile("../../test.tr");
            var diagn = new Diagnostics.Collection();

            var lex = Grammar.Tokenizer.Tokenize(src, diagn);
            //lex.PrintDebug(src);

            var syn = Grammar.ASTParser.Parse(lex, src, diagn);
            syn.PrintDebug(src);

            var struc = Structure.Analyzer.Pass(syn, src, diagn);
            //struc.PrintDebug(src);

            var semantics = Semantics.Analyzer.Pass(struc, diagn);
            semantics.PrintFunctsDebug();

            diagn.PrintToConsole();

            Console.ReadKey();
        }
    }
}
