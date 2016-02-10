using System;
using System.Collections.Generic;


namespace Trapl.Extraction
{
    public class Funct
    {
        public class Binding
        {
            public int nameIndex, registerIndex;
        }


        public Path path;
        public UseDirective[] useDirectives;

        public Dependencies dependencies = new Dependencies();

        public List<int> registerTypes = new List<int>();
        public List<Binding> bindings = new List<Binding>();


        public void PrintToConsole(string indentation = "")
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(indentation + path.GetString());
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(" funct");
            Console.ResetColor();
            dependencies.PrintToConsole("  ");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(indentation + "  " + "registers");
            Console.ResetColor();

            for (var i = 0; i < this.registerTypes.Count; i++)
            {
                Console.Write(indentation + "    " + "#r" + i + " ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("type" + this.registerTypes[i]);
                Console.ResetColor();
            }
        }
    }
}
