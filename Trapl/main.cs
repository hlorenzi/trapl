using System;


namespace Trapl
{
    class Program
    {
        static void Main(string[] args)
        {
            var session = new Infrastructure.Session();
            session.AddUnit(Infrastructure.Unit.MakeFromFile("../../test.tr"));
            session.Resolve();
            session.PrintDefs();
            session.diagn.PrintToConsole(session);
            Console.ReadKey();
        }
    }
}
