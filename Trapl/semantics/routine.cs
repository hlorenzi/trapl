﻿using System;
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


    public abstract class SourceOperand
    {
        public Diagnostics.Span span;


        public abstract string GetString(Infrastructure.Session session);


        public virtual Infrastructure.Type GetTypeForInference(Session session, Routine routine)
        {
            return new TypePlaceholder();
        }
        

        public virtual void SetTypeForInference(Session session, Routine routine, Infrastructure.Type type)
        {

        }
    }


    public class SourceOperandRegister : SourceOperand
    {
        public StorageAccess access;


        public SourceOperandRegister(StorageAccess access, Diagnostics.Span span)
        {
            this.access = access;
            this.span = span;
        }


        public override string GetString(Infrastructure.Session session)
        {
            return this.access.GetString(session);
        }


        public override Infrastructure.Type GetTypeForInference(Session session, Routine routine)
        {
            return routine.registers[access.registerIndex].type;
        }


        public override void SetTypeForInference(Session session, Routine routine, Infrastructure.Type type)
        {
            routine.registers[access.registerIndex].type = type;
        }
    }


    public class SourceOperandFunct : SourceOperand
    {
        public List<DeclFunct> potentialFuncts = new List<DeclFunct>();


        public SourceOperandFunct(List<DeclFunct> potentialFuncts, Diagnostics.Span span)
        {
            this.potentialFuncts = potentialFuncts;
            this.span = span;
        }


        public SourceOperandFunct(DeclFunct funct, Diagnostics.Span span)
        {
            this.potentialFuncts.Add(funct);
            this.span = span;
        }


        public override string GetString(Infrastructure.Session session)
        {
            if (this.potentialFuncts.Count > 1)
                return "ambiguous funct";
            else if (this.potentialFuncts.Count == 0)
                return "no funct";
            else
                return this.potentialFuncts[0].GetString(session);
        }


        public override Infrastructure.Type GetTypeForInference(Session session, Routine routine)
        {
            if (this.potentialFuncts.Count != 1)
                return new TypeError();

            return new TypeFunct(this.potentialFuncts[0]);
        }


        public override void SetTypeForInference(Session session, Routine routine, Infrastructure.Type type)
        {
            
        }
    }



    public class SourceOperandNumberLiteral : SourceOperand
    {
        public string value;


        public SourceOperandNumberLiteral(string value, Diagnostics.Span span)
        {
            this.value = value;
            this.span = span;
        }


        public override string GetString(Infrastructure.Session session)
        {
            return this.value;
        }


        public override Infrastructure.Type GetTypeForInference(Session session, Routine routine)
        {
            return new TypeStruct(session.primitiveInt);
        }


        public override void SetTypeForInference(Session session, Routine routine, Infrastructure.Type type)
        {

        }
    }



    public class SourceOperandTupleLiteral : SourceOperand
    {
        public List<SourceOperand> elementSources = new List<SourceOperand>();


        public SourceOperandTupleLiteral(Diagnostics.Span span)
        {
            this.span = span;
        }


        public override string GetString(Infrastructure.Session session)
        {
            var result = "(";
            for (int i = 0; i < elementSources.Count; i++)
            {
                result += elementSources[i].GetString(session);
                if (i < elementSources.Count - 1)
                    result += ", ";
            }
            return result + ")";
        }


        public override Infrastructure.Type GetTypeForInference(Session session, Routine routine)
        {
            if (elementSources.Count > 0)
                throw new InternalException("not implemented");

            return new TypeTuple();
        }


        public override void SetTypeForInference(Session session, Routine routine, Infrastructure.Type type)
        {

        }
    }


    public class SourceOperandCall : SourceOperand
    {
        public SourceOperand calledSource;
        public List<SourceOperand> argumentSources = new List<SourceOperand>();


        public SourceOperandCall(SourceOperand calledSource, Diagnostics.Span span)
        {
            this.calledSource = calledSource;
            this.span = span;
        }


        public override string GetString(Infrastructure.Session session)
        {
            var result =
                "call " + this.calledSource.GetString(session) + " (";

            for (var i = 0; i < this.argumentSources.Count; i++)
            {
                result += this.argumentSources[i].GetString(session);
                if (i < this.argumentSources.Count - 1)
                    result += ", ";
            }

            return result + ")";
        }


        public override Infrastructure.Type GetTypeForInference(Session session, Routine routine)
        {
            var functType = calledSource.GetTypeForInference(session, routine) as TypeFunct;
            if (functType == null)
                return new TypePlaceholder();

            return functType.returnType;
        }


        public override void SetTypeForInference(Session session, Routine routine, Infrastructure.Type type)
        {
            calledSource.SetTypeForInference(session, routine, type);
        }
    }


    public abstract class Instruction
    {
        public Diagnostics.Span span;


        public abstract string GetString(Infrastructure.Session session);
    }


    public class InstructionExec : Instruction
    {
        public SourceOperand source;


        public InstructionExec(SourceOperand source)
        {
            this.source = source;
        }


        public override string GetString(Infrastructure.Session session)
        {
            return
                "exec " + this.source.GetString(session);
        }
    }


    public class InstructionCopy : Instruction
    {
        public StorageAccess destination;
        public SourceOperand source;


        public InstructionCopy(StorageAccess destination, SourceOperand source)
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
        public SourceOperand conditionSource;
        public int trueDestinationSegment;
        public int falseDestinationSegment;


        public InstructionBranch(SourceOperand conditionSource, int trueDestinationSegment, int falseDestinationSegment)
        {
            this.conditionSource = conditionSource;
            this.trueDestinationSegment = trueDestinationSegment;
            this.falseDestinationSegment = falseDestinationSegment;
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
