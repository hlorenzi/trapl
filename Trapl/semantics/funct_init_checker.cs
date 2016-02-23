using System.Collections.Generic;


namespace Trapl.Semantics
{
    public class FunctInitChecker
    {
        public static bool Check(Core.Session session, Core.DeclFunct funct)
        {
            var checker = new FunctInitChecker(session, funct);

            var statusList = new List<InitStatus>();

            for (var i = 0; i < funct.registerTypes.Count; i++)
            {
                if (i > 0 && i < funct.parameterNum)
                    statusList.Add(new InitStatus(true));
                else
                    statusList.Add(new InitStatus(TypeResolver.GetFieldNum(session, funct, funct.registerTypes[i])));
            }

            checker.Check(0, statusList);
            return checker.foundErrors;
        }


        private Core.Session session;
        private Core.DeclFunct funct;
        private bool foundErrors;


        private class InitStatus
        {
            public bool hasFields;
            public bool fullyInitialized;
            public InitStatus[] fieldStatuses = new InitStatus[0];


            private InitStatus()
            {

            }


            public InitStatus(bool fullyInitialized)
            {
                this.hasFields = false;
                this.fullyInitialized = fullyInitialized;
            }


            public InitStatus(int fieldNum)
            {
                if (fieldNum == 0)
                {
                    this.hasFields = false;
                    this.fullyInitialized = false;
                }
                else
                {
                    this.hasFields = true;
                    this.fieldStatuses = new InitStatus[fieldNum];
                    for (var i = 0; i < fieldNum; i++)
                        this.fieldStatuses[i] = new InitStatus(false);
                }
            }


            public InitStatus Clone()
            {
                var other = new InitStatus
                {
                    hasFields = this.hasFields,
                    fullyInitialized = this.fullyInitialized,
                    fieldStatuses = new InitStatus[this.fieldStatuses.Length]
                };

                for (var i = 0; i < this.fieldStatuses.Length; i++)
                    other.fieldStatuses[i] = this.fieldStatuses[i].Clone();

                return other;
            }


            public void ConvertToFields(int fieldNum)
            {
                if (fieldNum > 0)
                {
                    this.hasFields = true;
                    this.fieldStatuses = new InitStatus[fieldNum];
                    for (var i = 0; i < fieldNum; i++)
                        this.fieldStatuses[i] = new InitStatus(this.fullyInitialized);
                }
            }


            public void SetStatus(bool initialized)
            {
                if (!this.hasFields)
                    this.fullyInitialized = initialized;
                else
                {
                    foreach (var field in this.fieldStatuses)
                        field.SetStatus(initialized);
                }
            }


            public bool IsInitialized()
            {
                if (!this.hasFields)
                    return this.fullyInitialized;
                else
                {
                    foreach (var field in this.fieldStatuses)
                    {
                        if (!field.IsInitialized())
                            return false;
                    }
                    return true;
                }
            }
        }


        private FunctInitChecker(Core.Session session, Core.DeclFunct funct)
        {
            this.session = session;
            this.funct = funct;
            this.foundErrors = false;
        }


        private void Check(int segmentIndex, List<InitStatus> statusList)
        {
            foreach (var inst in this.funct.segments[segmentIndex].instructions)
            {
                var instMoveData = (inst as Core.InstructionMoveData);
                if (instMoveData != null)
                    CheckMoveData(statusList, instMoveData);

                var instMoveLitBool = (inst as Core.InstructionMoveLiteralBool);
                if (instMoveLitBool != null)
                    CheckMoveBoolLiteral(statusList, instMoveLitBool);

                var instMoveLitInt = (inst as Core.InstructionMoveLiteralInt);
                if (instMoveLitInt != null)
                    CheckMoveIntLiteral(statusList, instMoveLitInt);

                var instMoveTuple = (inst as Core.InstructionMoveLiteralTuple);
                if (instMoveTuple != null)
                    CheckMoveTupleLiteral(statusList, instMoveTuple);

                var instMoveFunct = (inst as Core.InstructionMoveLiteralFunct);
                if (instMoveFunct != null)
                    CheckMoveFunctLiteral(statusList, instMoveFunct);

                var instMoveCallResult = (inst as Core.InstructionMoveCallResult);
                if (instMoveCallResult != null)
                    CheckMoveCallResult(statusList, instMoveCallResult);

                var instBranch = (inst as Core.InstructionBranch);
                if (instBranch != null)
                {
                    CheckBranch(statusList, instBranch);
                    this.Check(instBranch.destinationSegmentIfTaken, CloneStatuses(statusList));
                    this.Check(instBranch.destinationSegmentIfNotTaken, CloneStatuses(statusList));
                    return;
                }

                var instGoto = (inst as Core.InstructionGoto);
                if (instGoto != null)
                {
                    this.Check(instGoto.destinationSegment, statusList);
                    return;
                }

                var instEnd = (inst as Core.InstructionEnd);
                if (instEnd != null)
                {
                    CheckEnd(statusList, instEnd);
                    return;
                }
            }
        }


        private List<InitStatus> CloneStatuses(List<InitStatus> orig)
        {
            var other = new List<InitStatus>(orig.Count);
            for (var i = 0; i < orig.Count; i++)
                other.Add(orig[i].Clone());
            return other;
        }


        private void CheckSource(List<InitStatus> statusList, Core.DataAccess source)
        {
            var srcReg = source as Core.DataAccessRegister;
            if (srcReg == null)
                return;

            var srcType = this.funct.registerTypes[srcReg.registerIndex];
            var srcInit = statusList[srcReg.registerIndex];

            var isInitialized = srcInit.IsInitialized();

            if (!isInitialized)
            {
                for (var i = 0; i < srcReg.fieldAccesses.indices.Count; i++)
                {
                    srcType = TypeResolver.GetFieldType(
                        this.session, this.funct, srcType, srcReg.fieldAccesses.indices[i]);

                    if (!srcInit.hasFields)
                    {
                        isInitialized = srcInit.IsInitialized();
                        break;
                    }

                    srcInit = srcInit.fieldStatuses[srcReg.fieldAccesses.indices[i]];

                    isInitialized = srcInit.IsInitialized();
                    if (isInitialized)
                        break;
                }
            }

            if (!isInitialized)
            {
                this.foundErrors = true;
                this.session.AddMessage(
                    Diagnostics.MessageKind.Error,
                    Diagnostics.MessageCode.UninitializedUse,
                    "use of possibly uninitialized value",
                    source.span);
            }
        }


        private void InitDestination(List<InitStatus> statusList, Core.DataAccess destination)
        {
            var destReg = destination as Core.DataAccessRegister;
            if (destReg == null)
                return;

            var destType = this.funct.registerTypes[destReg.registerIndex];
            var destInit = statusList[destReg.registerIndex];

            if (destInit.IsInitialized())
                return;

            for (var i = 0; i < destReg.fieldAccesses.indices.Count; i++)
            {
                if (!destInit.hasFields)
                    destInit.ConvertToFields(TypeResolver.GetFieldNum(this.session, this.funct, destType));

                destType = TypeResolver.GetFieldType(
                    this.session, this.funct, destType, destReg.fieldAccesses.indices[i]);

                destInit = destInit.fieldStatuses[destReg.fieldAccesses.indices[i]];

                if (destInit.IsInitialized())
                    return;
            }

            destInit.SetStatus(true);
        }


        private void CheckBranch(List<InitStatus> statusList, Core.InstructionBranch inst)
        {
            CheckSource(statusList, inst.conditionReg);
        }


        private void CheckEnd(List<InitStatus> statusList, Core.InstructionEnd inst)
        {
            if (!statusList[0].IsInitialized())
            {
                this.foundErrors = true;
                this.session.AddMessage(
                    Diagnostics.MessageKind.Error,
                    Diagnostics.MessageCode.UninitializedUse,
                    "ret possibly uninitialized",
                    inst.span);
            }
        }


        private void CheckMoveData(List<InitStatus> statusList, Core.InstructionMoveData inst)
        {
            CheckSource(statusList, inst.source);
            InitDestination(statusList, inst.destination);
        }


        private void CheckMoveBoolLiteral(List<InitStatus> statusList, Core.InstructionMoveLiteralBool inst)
        {
            InitDestination(statusList, inst.destination);
        }


        private void CheckMoveIntLiteral(List<InitStatus> statusList, Core.InstructionMoveLiteralInt inst)
        {
            InitDestination(statusList, inst.destination);
        }


        private void CheckMoveTupleLiteral(List<InitStatus> statusList, Core.InstructionMoveLiteralTuple inst)
        {
            for (var i = 0; i < inst.sourceElements.Count; i++)
                CheckSource(statusList, inst.sourceElements[i]);

            InitDestination(statusList, inst.destination);
        }


        private void CheckMoveFunctLiteral(List<InitStatus> statusList, Core.InstructionMoveLiteralFunct inst)
        {
            InitDestination(statusList, inst.destination);
        }


        private void CheckMoveCallResult(List<InitStatus> statusList, Core.InstructionMoveCallResult inst)
        {
            CheckSource(statusList, inst.callTargetSource);

            for (var i = 0; i < inst.argumentSources.Length; i++)
                CheckSource(statusList, inst.argumentSources[i]);

            InitDestination(statusList, inst.destination);
        }
    }
}
