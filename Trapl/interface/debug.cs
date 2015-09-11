using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trapl.Interface
{
    public static class Debug
    {
        private static int indentationLevel = 0;


        public static void BeginSection(string str)
        {
            Console.WriteLine();
            Console.Write(new string(' ', indentationLevel * 2));
            Console.WriteLine(str);
            indentationLevel++;
        }

    
        public static void Note(string str)
        {
            Console.Write(new string(' ', indentationLevel * 2));
            Console.WriteLine(str);
        }


        public static void PrintAST(Interface.SourceCode src, Grammar.ASTNode astNode)
        {
            astNode.PrintDebugRecursive(src, indentationLevel, indentationLevel);
        }


        public static void EndSection()
        {
            indentationLevel--;
        }
    }
}
