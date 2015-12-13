using Trapl.Diagnostics;
using Trapl.Infrastructure;


namespace Trapl.Semantics
{
    public class RoutineTypeChecker
    {
        public static void Check(Infrastructure.Session session, Routine body)
        {
            var checker = new RoutineTypeChecker(session, body);
            //checker.Check();
        }


        private Infrastructure.Session session;
        private Routine routine;


        private RoutineTypeChecker(Infrastructure.Session session, Routine routine)
        {
            this.session = session;
            this.routine = routine;
        }


        /*private void Check()
        {
            this.CheckUnresolvedRegisters();

            foreach (var segment in this.routine.segments)
            {
                foreach (var inst in segment.instructions)
                {
                    foreach (var operand in inst.EnumerateOperands())
                        CheckOperand(operand);

                    var instCopy = (inst as InstructionCopy);
                    if (instCopy != null)
                        CheckInstructionCopy(instCopy);
                }
            }
        }


        private void CheckOperand(SourceOperand operand)
        {
            foreach (var suboperand in operand.EnumerateSuboperands())
                CheckOperand(suboperand);

            var operandFunct = (operand as SourceOperandFunct);
            if (operandFunct != null)
                CheckOperandFunct(operandFunct);
        }


        private static bool DoesMismatch(Type type1, Type type2)
        {
            if (type1 is TypePlaceholder ||
                type2 is TypePlaceholder ||
                type1 is TypeError ||
                type2 is TypeError)
                return false;

            return !type1.IsSame(type2);
        }


        private void CheckUnresolvedRegisters()
        {
            for (var i = 0; i < this.routine.registers.Count; i++)
            {
                if (this.routine.registers[i].type.IsResolved())
                    continue;

                var binding = this.routine.bindings.Find(b => b.registerIndex == i);
                if (binding != null)
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.InferenceFailed,
                        "cannot infer type for '" +
                        binding.name.GetString(this.session) + "'",
                        binding.name.span);
                }
                else
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.Internal,
                        "cannot infer type for register #r" + i);
                }

                this.routine.registers[i].type = new TypeError();
            }
        }


        private void CheckOperandFunct(SourceOperandFunct operand)
        {
            var count = operand.potentialFuncts.Count;

            if (count > 1)
            {
                session.diagn.Add(MessageKind.Error, MessageCode.InferenceFailed,
                    "cannot infer which funct to use", operand.span);

                session.diagn.AddInnerToLast(MessageKind.Info, MessageCode.Info,
                    "ambiguous between the following" +
                    (count > 2 ? " and other " + (count - 2) : "") + ":",
                    operand.potentialFuncts[0].name.span,
                    operand.potentialFuncts[1].name.span);
            }
            else if (count == 0)
            {
                session.diagn.Add(MessageKind.Error, MessageCode.InferenceFailed,
                    "no matching funct", operand.span);
            }
        }


        private void CheckInstructionCopy(InstructionCopy code)
        {
            var typeDest = this.routine.registers[code.destination.registerIndex].type;
            var typeSrc = code.source.GetOutputType(this.session, this.routine);

            if (DoesMismatch(typeDest, typeSrc))
            {
                if (code.destination.registerIndex == 0)
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTypes,
                        "returning '" +
                        typeSrc.GetString(session) + "' " +
                        "in funct returning '" +
                        typeDest.GetString(session) + "'",
                        code.source.span);
                }
                else
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTypes,
                        "assigning '" +
                        typeSrc.GetString(session) + "' " +
                        "to '" +
                        typeDest.GetString(session) + "'",
                        code.destination.span,
                        code.source.span);
                }
            }
        }*/
    }
}