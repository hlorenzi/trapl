using System;


namespace Trapl
{
    class Program
    {
        static void Main(string[] args)
        {
            Infrastructure.Session.Compile(Infrastructure.Unit.MakeFromFile("../../test.tr"));
            Console.ReadKey();
        }
    }
}
