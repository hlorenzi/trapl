using System;


namespace Trapl
{
    class Program
    {
        static void Main(string[] args)
        {
            var src = Source.FromFile("../../test.tr");
            var diagn = new Diagnostics.MessageList();

            var lex = Lexer.Analyzer.Pass(src, diagn);
            //lex.PrintDebug(src);

            var syn = Syntax.Analyzer.Pass(lex, src, diagn);
            syn.PrintDebug(src);

            var struc = Structure.Analyzer.Pass(syn, src, diagn);
            //struc.PrintDebug(src);

            diagn.Print();

            Console.ReadKey();
        }
    }
}
