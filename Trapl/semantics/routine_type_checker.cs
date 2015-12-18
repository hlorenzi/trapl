using Trapl.Diagnostics;
using Trapl.Infrastructure;


namespace Trapl.Semantics
{
    public class RoutineTypeChecker
    {
        public static void Check(Infrastructure.Session session, Routine body)
        {
            var checker = new RoutineTypeChecker(session, body);
            checker.Check();
        }


        private Infrastructure.Session session;
        private Routine routine;


        private RoutineTypeChecker(Infrastructure.Session session, Routine routine)
        {
            this.session = session;
            this.routine = routine;
        }


        private void Check()
        {
            this.CheckUnresolvedRegisters();

            foreach (var segment in this.routine.segments)
            {
                foreach (var inst in segment.instructions)
                {
                    var instFromStorage = (inst as InstructionCopyFromStorage);
                    if (instFromStorage != null)
                        CheckInstructionCopyFromStorage(instFromStorage);

                    var instFromTuple = (inst as InstructionCopyFromTupleLiteral);
                    if (instFromTuple != null)
                        ;// CheckInstructionCopyFromTupleLiteral(instFromTuple);

                    var instFromFunct = (inst as InstructionCopyFromFunct);
                    if (instFromFunct != null)
                        CheckInstructionCopyFromFunct(instFromFunct);

                    var instFromCall = (inst as InstructionCopyFromCall);
                    if (instFromCall != null)
                        ;// CheckInstructionCopyFromCall(instFromCall);
                }
            }
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


        private void CheckInstructionCopyFromFunct(InstructionCopyFromFunct inst)
        {
            var count = inst.potentialFuncts.Count;

            if (count > 1)
            {
                session.diagn.Add(MessageKind.Error, MessageCode.InferenceFailed,
                    "cannot infer which funct to use", inst.span);

                session.diagn.AddInnerToLast(MessageKind.Info, MessageCode.Info,
                    "ambiguous between the following" +
                    (count > 2 ? " and other " + (count - 2) : "") + ":",
                    inst.potentialFuncts[0].name.span,
                    inst.potentialFuncts[1].name.span);
            }
            else if (count == 0)
            {
                session.diagn.Add(MessageKind.Error, MessageCode.InferenceFailed,
                    "no matching funct", inst.span);
            }
        }


        private void CheckInstructionCopyFromStorage(InstructionCopyFromStorage inst)
        {
            var typeDest = this.routine.registers[inst.destination.registerIndex].type;
            var typeSrc = this.routine.registers[inst.source.registerIndex].type;

            if (DoesMismatch(typeDest, typeSrc))
            {
                if (inst.destination.registerIndex == 0)
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTypes,
                        "returning '" +
                        typeSrc.GetString(session) + "' " +
                        "in funct returning '" +
                        typeDest.GetString(session) + "'",
                        inst.source.span);
                }
                else
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTypes,
                        "assigning '" +
                        typeSrc.GetString(session) + "' " +
                        "to '" +
                        typeDest.GetString(session) + "'",
                        inst.destination.span,
                        inst.source.span);
                }
            }
        }
    }
}