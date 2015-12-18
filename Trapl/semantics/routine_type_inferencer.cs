using System.Collections.Generic;
using Trapl.Diagnostics;
using Trapl.Infrastructure;


namespace Trapl.Semantics
{
    public class RoutineTypeInferencer
    {
        public static void DoInference(Infrastructure.Session session, Routine routine)
        {
            var inferencer = new RoutineTypeInferencer(session, routine);
            inferencer.ApplyRules();
        }


        private Infrastructure.Session session;
        private Routine routine;

        private bool appliedAnyRule = false;


        private RoutineTypeInferencer(Infrastructure.Session session, Routine body)
        {
            this.session = session;
            this.routine = body;
        }


        private void ApplyRules()
        {
            this.appliedAnyRule = true;
            while (this.appliedAnyRule)
            {
                this.appliedAnyRule = false;

                foreach (var segment in this.routine.segments)
                {
                    foreach (var inst in segment.instructions)
                    {
                        var instFromStorage = (inst as InstructionCopyFromStorage);
                        if (instFromStorage != null)
                            ApplyRuleForInstructionCopyFromStorage(instFromStorage);

                        var instFromTuple = (inst as InstructionCopyFromTupleLiteral);
                        if (instFromTuple != null)
                            ApplyRuleForInstructionCopyFromTupleLiteral(instFromTuple);

                        var instFromFunct = (inst as InstructionCopyFromFunct);
                        if (instFromFunct != null)
                            ApplyRuleForInstructionCopyFromFunct(instFromFunct);

                        var instFromCall = (inst as InstructionCopyFromCall);
                        if (instFromCall != null)
                            ApplyRuleForInstructionCopyFromCall(instFromCall);
                    }
                }
            }
        }


        private void ApplyRuleForInstructionCopyFromStorage(InstructionCopyFromStorage inst)
        {
            if (inst.destination.fieldAccesses.Count > 0 ||
                inst.source.fieldAccesses.Count > 0)
                throw new InternalException("not implemented");

            var destType = routine.registers[inst.destination.registerIndex].type;
            var srcType = routine.registers[inst.source.registerIndex].type;

            this.appliedAnyRule |=
                TryInference(session, srcType, ref destType) |
                TryInference(session, destType, ref srcType);

            routine.registers[inst.destination.registerIndex].type = destType;
            routine.registers[inst.source.registerIndex].type = srcType;
        }


        private void ApplyRuleForInstructionCopyFromTupleLiteral(InstructionCopyFromTupleLiteral inst)
        {
            if (inst.destination.fieldAccesses.Count > 0)
                throw new InternalException("not implemented");

            var destType = routine.registers[inst.destination.registerIndex].type;
            var srcTuple = new TypeTuple();
            foreach (var elem in inst.elementSources)
                srcTuple.elementTypes.Add(this.routine.registers[elem.registerIndex].type);

            var srcType = (Type)srcTuple;

            var result =
                TryInference(session, srcType, ref destType) |
                TryInference(session, destType, ref srcType);

            if (result)
            {
                this.appliedAnyRule = true;
                routine.registers[inst.destination.registerIndex].type = destType;
                for (var i = 0; i < inst.elementSources.Count; i++)
                {
                    this.routine.registers[inst.elementSources[i].registerIndex].type =
                        srcTuple.elementTypes[i];
                }
            }
        }


        private void ApplyRuleForInstructionCopyFromCall(InstructionCopyFromCall inst)
        {
            if (inst.destination.fieldAccesses.Count > 0)
                throw new InternalException("not implemented");

            var destType = routine.registers[inst.destination.registerIndex].type;
            var callType = routine.registers[inst.calledSource.registerIndex].type;
            var callFunct = callType as TypeFunct;
            if (callFunct == null)
                return;

            var srcFunct = new TypeFunct(destType, 0);
            foreach (var arg in inst.argumentSources)
                srcFunct.argumentTypes.Add(this.routine.registers[arg.registerIndex].type);

            var srcType = (Type)srcFunct;

            var result =
                TryInference(session, callType, ref srcType) |
                TryInference(session, srcType, ref callType) |
                TryInference(session, callFunct.returnType, ref destType) |
                TryInference(session, destType, ref callFunct.returnType);

            if (result)
            {
                this.appliedAnyRule = true;

                routine.registers[inst.calledSource.registerIndex].type = callType;
                routine.registers[inst.destination.registerIndex].type = callFunct.returnType;

                for (var i = 0; i < inst.argumentSources.Count; i++)
                {
                    this.routine.registers[inst.argumentSources[i].registerIndex].type =
                        srcFunct.argumentTypes[i];
                }
            }
        }


        private void ApplyRuleForInstructionCopyFromFunct(InstructionCopyFromFunct inst)
        {
            if (inst.destination.fieldAccesses.Count > 0)
                throw new InternalException("not implemented");

            if (inst.potentialFuncts.Count > 0)
            {
                for (var i = inst.potentialFuncts.Count - 1; i >= 0; i--)
                {
                    var functType = new TypeFunct(inst.potentialFuncts[i]);
                    if (!functType.IsMatch(this.routine.registers[inst.destination.registerIndex].type))
                    {
                        inst.potentialFuncts.RemoveAt(i);
                        this.appliedAnyRule = true;
                    }
                }
            }

            if (inst.potentialFuncts.Count == 1)
            {
                if (TryInference(this.session,
                    new TypeFunct(inst.potentialFuncts[0]),
                    ref routine.registers[inst.destination.registerIndex].type))
                    this.appliedAnyRule = true;
            }
        }


        public static bool TryInference(Session session, Type typeFrom, ref Type typeTo)
        {
            if (typeTo is TypePlaceholder && !(typeFrom is TypePlaceholder))
            {
                typeTo = typeFrom;
                return true;
            }

            else if (typeTo is TypeReference && typeFrom is TypeReference)
            {
                var refTo = (TypeReference)typeTo;
                var refFrom = (TypeReference)typeFrom;
                var result = TryInference(session, refFrom.referencedType, ref refTo.referencedType);
                result |= TryInference(session, refTo.referencedType, ref refFrom.referencedType);
                return result;
            }

            else if (typeTo is TypeStruct && typeFrom is TypeStruct)
            {
                var structTo = (TypeStruct)typeTo;
                var structFrom = (TypeStruct)typeFrom;

                /*if (structTo.potentialStructs.Count > 1 && structFrom.potentialStructs.Count == 1)
                {
                    TryInference(session, structFrom.nameInference.template, ref structTo.nameInference.template);
                    typeTo = typeFrom;
                }*/

                return false;
            }

            else if (typeTo is TypeFunct && typeFrom is TypeFunct)
            {
                var functTo = (TypeFunct)typeTo;
                var functFrom = (TypeFunct)typeFrom;
                var result = TryInference(session, functFrom.returnType, ref functTo.returnType);

                if (functTo.argumentTypes == null && functFrom.argumentTypes != null)
                {
                    functTo.argumentTypes = new List<Type>(functFrom.argumentTypes);
                    result = true;
                }

                if (functTo.argumentTypes != null &&
                    functFrom.argumentTypes != null &&
                    functTo.argumentTypes.Count == functFrom.argumentTypes.Count)
                {
                    for (var i = 0; i < functTo.argumentTypes.Count; i++)
                    {
                        var argTo = functTo.argumentTypes[i];
                        result |= TryInference(session, functFrom.argumentTypes[i], ref argTo);
                        functTo.argumentTypes[i] = argTo;
                    }
                }

                typeTo = functTo;
                return result;
            }

            else if (typeTo is TypeTuple && typeFrom is TypeTuple)
            {
                var tupleTo = (TypeTuple)typeTo;
                var tupleFrom = (TypeTuple)typeFrom;
                var result = false;

                if (tupleTo.elementTypes.Count == tupleFrom.elementTypes.Count)
                {
                    for (var i = 0; i < tupleTo.elementTypes.Count; i++)
                    {
                        var elemTo = tupleTo.elementTypes[i];
                        result |= TryInference(session, tupleFrom.elementTypes[i], ref elemTo);
                        tupleTo.elementTypes[i] = elemTo;
                    }
                }

                typeTo = tupleTo;
                return result;
            }

            return false;
        }


        private void TryTemplateInference(Template fromTemplate, ref Template toTemplate)
        {
            if (toTemplate.unconstrained)
            {
                if (!fromTemplate.unconstrained)
                {
                    toTemplate = fromTemplate;
                    this.appliedAnyRule = true;
                }
            }
            else if (toTemplate.parameters.Count == fromTemplate.parameters.Count)
            {
                for (int i = 0; i < toTemplate.parameters.Count; i++)
                {
                    if (toTemplate.parameters[i] is Template.ParameterType &&
                        fromTemplate.parameters[i] is Template.ParameterType)
                    {
                        var param = (Template.ParameterType)toTemplate.parameters[i];
                        TryInference(session, ((Template.ParameterType)fromTemplate.parameters[i]).type, ref param.type);
                        toTemplate.parameters[i] = param;
                    }
                }
            }
        }
    }
}