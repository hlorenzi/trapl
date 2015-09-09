using System;


namespace Trapl
{
    class Program
    {
        static void Main(string[] args)
        {
            Interface.Session.Compile(Interface.SourceCode.MakeFromFile("../../test.tr"));
            Console.ReadKey();
        }
    }
}
