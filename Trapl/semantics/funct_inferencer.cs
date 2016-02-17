using System.Collections.Generic;


namespace Trapl.Semantics
{
    public class FunctInferencer
    {
        public static void DoInference(Core.Session session, Core.DeclFunct funct)
        {
            var inferencer = new FunctInferencer(session, funct);
            inferencer.ApplyRules();
        }


        private Core.Session session;
        private Core.DeclFunct funct;


        private FunctInferencer(Core.Session session, Core.DeclFunct funct)
        {
            this.session = session;
            this.funct = funct;
        }


        private void ApplyRules()
        {
            while (true)
            {
                var appliedSomeRule = false;

                foreach (var segment in this.funct.segments)
                {
                    foreach (var inst in segment.instructions)
                    {
                        var instMoveData = (inst as Core.InstructionMoveData);
                        if (instMoveData != null)
                            ApplyRuleForMoveData(ref appliedSomeRule, instMoveData);

                        var instMoveLitTuple = (inst as Core.InstructionMoveLiteralTuple);
                        if (instMoveLitTuple != null)
                            ApplyRuleForMoveTupleLiteral(ref appliedSomeRule, instMoveLitTuple);

                        var instMoveFunct = (inst as Core.InstructionMoveLiteralFunct);
                        if (instMoveFunct != null)
                            ApplyRuleForMoveFunctLiteral(ref appliedSomeRule, instMoveFunct);

                        var instMoveCallResult = (inst as Core.InstructionMoveCallResult);
                        if (instMoveCallResult != null)
                            ApplyRuleForMoveCallResult(ref appliedSomeRule, instMoveCallResult);
                    }
                }

                if (!appliedSomeRule)
                    break;
            }
        }


        private Core.Type GetDataAccessType(Core.DataAccess access)
        {
            var regAccess = access as Core.DataAccessRegister;
            if (regAccess != null)
                return this.funct.registerTypes[regAccess.registerIndex];

            return null;
        }


        private bool ApplyToDataAccess(Core.DataAccess access, Core.Type type)
        {
            var regAccess = access as Core.DataAccessRegister;
            if (regAccess != null)
            {
                this.funct.registerTypes[regAccess.registerIndex] = type;
                return true;
            }

            return false;
        }


        private void ApplyRuleForMoveData(ref bool appliedRule, Core.InstructionMoveData inst)
        {
            var destType = GetDataAccessType(inst.destination);
            var srcType = GetDataAccessType(inst.source);

            var inferredDest = TypeInferencer.Try(session, srcType, ref destType);
            var inferredSrc = TypeInferencer.Try(session, destType, ref srcType);

            if (inferredDest)
                appliedRule = ApplyToDataAccess(inst.destination, destType);

            if (inferredSrc)
                appliedRule = ApplyToDataAccess(inst.source, destType);
        }


        private void ApplyRuleForMoveTupleLiteral(ref bool appliedRule, Core.InstructionMoveLiteralTuple inst)
        {
            var destType = GetDataAccessType(inst.destination);

            var tupleElements = new Core.Type[inst.sourceElements.Count];
            for (var i = 0; i < inst.sourceElements.Count; i++)
                tupleElements[i] = GetDataAccessType(inst.sourceElements[i]);

            var srcTuple = Core.TypeTuple.Of(tupleElements);
            var srcType = (Core.Type)srcTuple;

            var inferredDest = TypeInferencer.Try(session, srcType, ref destType);
            var inferredSrc = TypeInferencer.Try(session, destType, ref srcType);

            if (inferredDest)
                appliedRule = ApplyToDataAccess(inst.destination, destType);

            if (inferredSrc)
            {
                for (var i = 0; i < inst.sourceElements.Count; i++)
                    appliedRule = ApplyToDataAccess(inst.sourceElements[i], srcTuple.elementTypes[i]);
            }
        }


        private void ApplyRuleForMoveCallResult(ref bool appliedRule, Core.InstructionMoveCallResult inst)
        {
            var destType = GetDataAccessType(inst.destination);
            var callType = GetDataAccessType(inst.callTargetSource);
            var callFunct = callType as Core.TypeFunct;
            if (callFunct == null)
                return;

            var inferredResult = TypeInferencer.Try(session, callFunct.returnType, ref destType);

            if (inferredResult)
                appliedRule = ApplyToDataAccess(inst.destination, destType);

            /*var srcArgumentTypes = new Core.Type[inst.argumentSources.Length];
            for (var i = 0; i < inst.argumentSources.Length; i++)
                srcArgumentTypes[i] = GetDataAccessType(inst.argumentSources[i]);

            var srcFunct = Core.TypeFunct.Of(destType, srcArgumentTypes);
            var srcType = (Core.Type)srcFunct;

            var inferredFunct = TypeInferencer.Try(session, callType, ref srcType);
            var inferredFunctArgs = TypeInferencer.Try(session, srcType, ref callType);
            var inferredResult = TypeInferencer.Try(session, callFunct.returnType, ref destType);
            var inferredFunctResult = TypeInferencer.Try(session, destType, ref callFunct.returnType);*/

            /*if (result)
            {
                this.appliedAnyRule = true;

                routine.registers[inst.calledSource.registerIndex].type = callType;
                routine.registers[inst.destination.registerIndex].type = callFunct.returnType;

                for (var i = 0; i < inst.argumentSources.Count; i++)
                {
                    this.routine.registers[inst.argumentSources[i].registerIndex].type =
                        srcFunct.argumentTypes[i];
                }
            }*/
        }


        private void ApplyRuleForMoveFunctLiteral(ref bool appliedRule, Core.InstructionMoveLiteralFunct inst)
        {
            var destType = GetDataAccessType(inst.destination);
            var srcType = (Core.Type)this.session.GetFunct(inst.functIndex).MakeFunctType();

            var inferredDest = TypeInferencer.Try(session, srcType, ref destType);

            if (inferredDest)
                appliedRule = ApplyToDataAccess(inst.destination, destType);

            /*if (inst.destination.fieldAccesses.Count > 0)
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
            }*/
        }
    }
}
