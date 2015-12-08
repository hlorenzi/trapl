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
            this.CheckUnresolvedLocals();

            foreach (var segment in this.routine.segments)
            {
                foreach (var inst in segment.instructions)
                {
                    var instCopy = (inst as InstructionCopy);
                    if (instCopy != null)
                        CheckInstructionCopy(instCopy);
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


        private void CheckUnresolvedLocals()
        {
            foreach (var binding in this.routine.bindings)
            {
                var register = this.routine.registers[binding.registerIndex];

                if (!register.type.IsResolved())
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.InferenceFailed,
                        "cannot infer type for '" +
                        binding.name.GetString(this.session) + "'",
                        binding.name.span);

                    register.type = new TypeError();
                }
            }
        }


        private void CheckInstructionCopy(InstructionCopy code)
        {
            var typeDest = this.routine.registers[code.destination.registerIndex].type;
            var typeSrc = code.source.GetTypeForInference(this.session, this.routine);

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
                        "passing '" +
                        typeSrc.GetString(session) + "' " +
                        "to '" +
                        typeDest.GetString(session) + "'",
                        code.destination.span,
                        code.source.span);
                }
            }
        }
    }
}