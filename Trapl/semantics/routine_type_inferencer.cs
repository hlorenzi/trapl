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
                        var instCopy = (inst as InstructionCopy);
                        if (instCopy != null)
                            ApplyRuleForInstructionCopy(instCopy);
                    }
                }
            }
        }


        private void ApplyRuleForInstructionCopy(InstructionCopy inst)
        {
            if (inst.destination.fieldAccesses.Count > 0)
                throw new InternalException("not implemented");

            var destType = this.routine.registers[inst.destination.registerIndex].type;
            var srcType = inst.source.GetTypeForInference(this.session, this.routine);

            TryInference(srcType, ref destType);
            TryInference(destType, ref srcType);

            this.routine.registers[inst.destination.registerIndex].type = destType;
            inst.source.SetTypeForInference(this.session, this.routine, srcType);
        }


        private void TryInference(Type typeFrom, ref Type typeTo)
        {
            if (typeTo is TypePlaceholder && !(typeFrom is TypePlaceholder))
            {
                this.appliedAnyRule = true;
                typeTo = typeFrom;
            }
            else if (typeTo is TypeReference && typeFrom is TypeReference)
            {
                var refTo = (TypeReference)typeTo;
                var refFrom = (TypeReference)typeFrom;
                TryInference(refFrom.referencedType, ref refTo.referencedType);
                TryInference(refTo.referencedType, ref refFrom.referencedType);
            }
            else if (typeTo is TypeStruct && typeFrom is TypeStruct)
            {
                var structTo = (TypeStruct)typeTo;
                var structFrom = (TypeStruct)typeFrom;

                if (structTo.potentialStructs.Count > 1 && structFrom.potentialStructs.Count == 1)
                {
                    TryTemplateInference(structFrom.nameInference.template, ref structTo.nameInference.template);
                    typeTo = typeFrom;
                }
            }
            else if (typeTo is TypeFunct && typeFrom is TypeFunct)
            {
                var functTo = (TypeFunct)typeTo;
                var functFrom = (TypeFunct)typeFrom;
                TryInference(functFrom.returnType, ref functTo.returnType);

                if (functTo.argumentTypes.Count == functFrom.argumentTypes.Count)
                {
                    for (var i = 0; i < functTo.argumentTypes.Count; i++)
                    {
                        var argTo = functTo.argumentTypes[i];
                        TryInference(functFrom.argumentTypes[i], ref argTo);
                        functTo.argumentTypes[i] = argTo;
                    }
                }

                typeTo = functTo;
            }
            else if (typeTo is TypeTuple && typeFrom is TypeTuple)
            {
                var tupleTo = (TypeTuple)typeTo;
                var tupleFrom = (TypeTuple)typeFrom;

                if (tupleTo.elementTypes.Count == tupleFrom.elementTypes.Count)
                {
                    for (var i = 0; i < tupleTo.elementTypes.Count; i++)
                    {
                        var elemTo = tupleTo.elementTypes[i];
                        TryInference(tupleFrom.elementTypes[i], ref elemTo);
                        tupleTo.elementTypes[i] = elemTo;
                    }
                }

                typeTo = tupleTo;
            }
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
                        TryInference(((Template.ParameterType)fromTemplate.parameters[i]).type, ref param.type);
                        toTemplate.parameters[i] = param;
                    }
                }
            }
        }
    }
}