using System;


namespace Trapl.Core
{
    public class InstructionDeinit : Instruction
    {
        public int registerIndex;


        public static InstructionDeinit ForRegister(int registerIndex)
        {
            return new InstructionDeinit { registerIndex = registerIndex };
        }


        public override void PrintToConsole(string indentation = "")
        {
            Console.Write(indentation);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("deinit ");
            Console.ResetColor();
            Console.WriteLine("#r" + registerIndex);
        }
    }
}
