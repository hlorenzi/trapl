using System;
using System.Collections.Generic;
using Trapl.Infrastructure;


namespace Trapl.Semantics
{
    public class Routine
    {
        public List<RoutineSegment> segments = new List<RoutineSegment>();
        public List<StorageLocation> registers = new List<StorageLocation>();
        public List<StorageBinding> bindings = new List<StorageBinding>();


        public int CreateSegment()
        {
            segments.Add(new RoutineSegment());
            return segments.Count - 1;
        }


        public void AddInstruction(int segmentIndex, Instruction inst)
        {
            segments[segmentIndex].instructions.Add(inst);
        }


        public int CreateRegister(Infrastructure.Type type)
        {
            registers.Add(new StorageLocation(type));
            return registers.Count - 1;
        }


        public int CreateBinding(int registerIndex)
        {
            var binding = new StorageBinding();
            binding.registerIndex = registerIndex;
            bindings.Add(binding);
            return bindings.Count - 1;
        }


        public void Print(Infrastructure.Session session)
        {
            for (int i = 0; i < registers.Count; i++)
            {
                Console.Out.WriteLine("#r" + i + ": " + registers[i].type.GetString(session));
            }

            Console.Out.WriteLine();

            for (int i = 0; i < bindings.Count; i++)
            {
                Console.Out.WriteLine(bindings[i].name.GetString(session) + " = #r" +
                    bindings[i].registerIndex + ": " +
                    registers[bindings[i].registerIndex].type.GetString(session));
            }

            Console.Out.WriteLine();

            for (int i = 0; i < segments.Count; i++)
            {
                Console.Out.WriteLine("#s" + i + ":");
                segments[i].Print(session);
            }
        }
    }


    public class RoutineSegment
    {
        public List<Instruction> instructions = new List<Instruction>();


        public void Print(Infrastructure.Session session)
        {
            for (int i = 0; i < instructions.Count; i++)
            {
                Console.Out.WriteLine("  " + instructions[i].GetString(session));
            }
        }
    }


    public class StorageBinding
    {
        public int registerIndex;
        public Infrastructure.Name name;
        public bool outOfScope;
    }


    public class StorageAccess
    {
        public int registerIndex;
        public List<int> fieldAccesses = new List<int>();
        public Diagnostics.Span span;


        public StorageAccess(int registerIndex, Diagnostics.Span span)
        {
            this.registerIndex = registerIndex;
            this.span = span;
        }


        public string GetString(Infrastructure.Session session)
        {
            return "#r" + this.registerIndex;
        }
    }


    public abstract class Instruction
    {
        public Diagnostics.Span span;


        public abstract string GetString(Infrastructure.Session session);
    }



    public class InstructionCopyFromStorage : Instruction
    {
        public StorageAccess destination;
        public StorageAccess source;


        public InstructionCopyFromStorage(StorageAccess destination, StorageAccess source)
        {
            this.destination = destination;
            this.source = source;
        }


        public override string GetString(Infrastructure.Session session)
        {
            return 
                "copy " + this.destination.GetString(session) +
                " <- " + this.source.GetString(session);
        }
    }


    public class InstructionCopyFromNumberLiteral : Instruction
    {
        public StorageAccess destination;
        public string value;


        public InstructionCopyFromNumberLiteral(StorageAccess destination, string value)
        {
            this.destination = destination;
            this.value = value;
        }


        public override string GetString(Infrastructure.Session session)
        {
            return
                "copy " + this.destination.GetString(session) +
                " <- " + this.value;
        }
    }


    public class InstructionCopyFromTupleLiteral : Instruction
    {
        public StorageAccess destination;
        public List<StorageAccess> elementSources = new List<StorageAccess>();


        public InstructionCopyFromTupleLiteral(StorageAccess destination)
        {
            this.destination = destination;
        }


        public override string GetString(Infrastructure.Session session)
        {
            var result =
                "copy " + this.destination.GetString(session) +
                " <- (";

            for (var i = 0; i < this.elementSources.Count; i++)
            {
                result += this.elementSources[i].GetString(session) +
                    (i < this.elementSources.Count - 1 ? ", " : "");
            }

            return result + ")";
        }
    }


    public class InstructionCopyFromFunct : Instruction
    {
        public StorageAccess destination;
        public List<DeclFunct> potentialFuncts = new List<DeclFunct>();


        public InstructionCopyFromFunct(StorageAccess destination, List<DeclFunct> potentialFuncts)
        {
            this.destination = destination;
            this.potentialFuncts = potentialFuncts;
        }


        public override string GetString(Infrastructure.Session session)
        {
            var result =
                "copy " + this.destination.GetString(session) +
                " <- ";

            if (this.potentialFuncts.Count > 1)
                return result + this.potentialFuncts.Count + " ambiguous functs";
            else if (this.potentialFuncts.Count == 0)
                return result + "no funct";
            else
                return result + this.potentialFuncts[0].GetString(session);
        }
    }


    public class InstructionCopyFromCall : Instruction
    {
        public StorageAccess destination;
        public StorageAccess calledSource;
        public List<StorageAccess> argumentSources = new List<StorageAccess>();


        public InstructionCopyFromCall(StorageAccess destination, StorageAccess called, List<StorageAccess> args)
        {
            this.destination = destination;
            this.calledSource = called;
            this.argumentSources = args;
        }


        public override string GetString(Infrastructure.Session session)
        {
            var result =
                "copy " + this.destination.GetString(session) +
                " <- call " + this.calledSource.GetString(session) + "(";

            for (var i = 0; i < this.argumentSources.Count; i++)
            {
                result += this.argumentSources[i].GetString(session) +
                    (i < this.argumentSources.Count - 1 ? ", " : "");
            }

            return result + ")";
        }
    }


    public class InstructionEnd : Instruction
    {
        public InstructionEnd()
        {

        }


        public override string GetString(Infrastructure.Session session)
        {
            return "end";
        }
    }


    public class InstructionGoto : Instruction
    {
        public int destinationSegment;


        public InstructionGoto(int destinationSegment)
        {
            this.destinationSegment = destinationSegment;
        }


        public override string GetString(Infrastructure.Session session)
        {
            return "goto #s" + this.destinationSegment;
        }
    }


    public class InstructionBranch : Instruction
    {
        public StorageAccess conditionSource;
        public int trueDestinationSegment = -1;
        public int falseDestinationSegment = -1;


        public InstructionBranch(StorageAccess conditionSource)
        {
            this.conditionSource = conditionSource;
        }


        public override string GetString(Infrastructure.Session session)
        {
            return
                "branch " + this.conditionSource.GetString(session) +
                " ? #s" + this.trueDestinationSegment +
                " : #s" + this.falseDestinationSegment;
        }
    }
}
