using System;
using System.Collections.Generic;


namespace Trapl.Extraction
{
    public class Struct
    {
        public class Field
        {
            public int nameIndex, typeIndex;
        }


        public Path path;
        public UseDirective[] useDirectives;

        public Dependencies dependencies = new Dependencies();

        public List<Field> fields = new List<Field>();


        public void PrintToConsole(string indentation = "")
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(indentation + path.GetString());
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(" struct");
            Console.ResetColor();
            dependencies.PrintToConsole("  ");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(indentation + "  " + "fields");
            Console.ResetColor();

            for (var i = 0; i < this.fields.Count; i++)
            {
                Console.Write(indentation + "    " + "field" + i + " ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("name" + this.fields[i].nameIndex);
                Console.Write(": ");
                Console.WriteLine("type" + this.fields[i].typeIndex);
                Console.ResetColor();
            }
        }
    }
}
