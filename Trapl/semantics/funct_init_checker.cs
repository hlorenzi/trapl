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
        private HashSet<Diagnostics.Span> alreadyReportedSet = new HashSet<Diagnostics.Span>();


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
                if (!this.hasFields && fieldNum > 0)
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

                var instMoveAddr = (inst as Core.InstructionMoveAddr);
                if (instMoveAddr != null)
                    CheckMoveAddr(statusList, instMoveAddr);

                var instMoveFunct = (inst as Core.InstructionMoveLiteralFunct);
                if (instMoveFunct != null)
                    CheckMoveFunctLiteral(statusList, instMoveFunct);

                var instMoveCallResult = (inst as Core.InstructionMoveCallResult);
                if (instMoveCallResult != null)
                    CheckMoveCallResult(statusList, instMoveCallResult);
            }

            var flow = this.funct.segments[segmentIndex].outFlow;
            var flowBranch = (flow as Core.SegmentFlowBranch);
            if (flowBranch != null)
            {
                CheckBranch(statusList, flowBranch);
                this.Check(flowBranch.destinationSegmentIfTaken, CloneStatuses(statusList));
                this.Check(flowBranch.destinationSegmentIfNotTaken, CloneStatuses(statusList));
                return;
            }

            var flowGoto = (flow as Core.SegmentFlowGoto);
            if (flowGoto != null)
            {
                this.Check(flowGoto.destinationSegment, statusList);
                return;
            }

            var flowEnd = (flow as Core.SegmentFlowEnd);
            if (flowEnd != null)
            {
                // Generate a void return.
                if (funct.GetReturnType().IsEmptyTuple() &&
                    !CheckSource(statusList, Core.DataAccessRegister.ForRegister(flowEnd.span, 0)))
                {
                    var retEmptyTuple = Core.InstructionMoveLiteralTuple.Empty(flowEnd.span,
                        Core.DataAccessRegister.ForRegister(flowEnd.span, 0));

                    this.funct.AddInstruction(segmentIndex, retEmptyTuple);
                    CheckMoveTupleLiteral(statusList, retEmptyTuple);
                }

                CheckEnd(statusList, flowEnd);
                return;
            }
        }


        private List<InitStatus> CloneStatuses(List<InitStatus> orig)
        {
            var other = new List<InitStatus>(orig.Count);
            for (var i = 0; i < orig.Count; i++)
                other.Add(orig[i].Clone());
            return other;
        }


        private bool CheckSource(List<InitStatus> statusList, Core.DataAccess source)
        {
            InitStatus baseStatus;
            Core.Type baseType;
            GetStatusRecursive(statusList, source, out baseStatus, out baseType);

            if (baseStatus != null)
                return baseStatus.IsInitialized();

            return true;
        }


        private void ValidateSource(List<InitStatus> statusList, Core.DataAccess source)
        {
            if (!CheckSource(statusList, source))
            {
                if (this.alreadyReportedSet.Contains(source.span))
                    return;

                this.foundErrors = true;
                this.alreadyReportedSet.Add(source.span);

                var srcReg = source as Core.DataAccessRegister;
                if (srcReg != null && srcReg.registerIndex == 0)
                {
                    this.session.AddMessage(
                        Diagnostics.MessageKind.Error,
                        Diagnostics.MessageCode.UninitializedUse,
                        "not returning a value but expecting '" +
                            funct.GetReturnType().GetString(session) + "'",
                        source.span);
                }
                else
                {
                    this.session.AddMessage(
                        Diagnostics.MessageKind.Error,
                        Diagnostics.MessageCode.UninitializedUse,
                        "use of possibly uninitialized value",
                        source.span);
                }
            }
        }


        private void InitDestination(List<InitStatus> statusList, Core.DataAccess destination)
        {
            InitStatus baseStatus;
            Core.Type baseType;
            GetStatusRecursive(statusList, destination, out baseStatus, out baseType);

            if (baseStatus != null)
                baseStatus.SetStatus(true);
        }


        private void GetStatusRecursive(List<InitStatus> statusList, Core.DataAccess access, out InitStatus baseStatus, out Core.Type baseType)
        {
            baseStatus = null;
            baseType = null;

            var accessField = access as Core.DataAccessField;
            if (accessField != null)
            {
                GetStatusRecursive(statusList, accessField.baseAccess, out baseStatus, out baseType);

                if (baseStatus == null)
                    return;

                var fieldNum = TypeResolver.GetFieldNum(session, funct, baseType);
                baseStatus.ConvertToFields(fieldNum);

                baseStatus = baseStatus.fieldStatuses[accessField.fieldIndex];
                baseType = TypeResolver.GetFieldType(session, funct, baseType, accessField.fieldIndex);
                return;
            }

            var accessReg = access as Core.DataAccessRegister;
            if (accessReg != null)
            {
                baseType = TypeResolver.GetDataAccessType(session, funct, accessReg);
                baseStatus = statusList[accessReg.registerIndex];
                return;
            }

            var accessDeref = access as Core.DataAccessDereference;
            if (accessDeref != null)
            {
                ValidateSource(statusList, accessDeref.innerAccess);
                baseStatus = null;
                return;
            }
        }


        private void CheckEnd(List<InitStatus> statusList, Core.SegmentFlowEnd flow)
        {
            ValidateSource(statusList, Core.DataAccessRegister.ForRegister(flow.span, 0));
        }


        private void CheckBranch(List<InitStatus> statusList, Core.SegmentFlowBranch flow)
        {
            ValidateSource(statusList, flow.conditionReg);
        }


        private void CheckMoveData(List<InitStatus> statusList, Core.InstructionMoveData inst)
        {
            ValidateSource(statusList, inst.source);
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
            for (var i = 0; i < inst.sourceElements.Length; i++)
                ValidateSource(statusList, inst.sourceElements[i]);

            InitDestination(statusList, inst.destination);
        }


        private void CheckMoveAddr(List<InitStatus> statusList, Core.InstructionMoveAddr inst)
        {
            ValidateSource(statusList, inst.source);
            InitDestination(statusList, inst.destination);
        }


        private void CheckMoveFunctLiteral(List<InitStatus> statusList, Core.InstructionMoveLiteralFunct inst)
        {
            InitDestination(statusList, inst.destination);
        }


        private void CheckMoveCallResult(List<InitStatus> statusList, Core.InstructionMoveCallResult inst)
        {
            ValidateSource(statusList, inst.callTargetSource);

            for (var i = 0; i < inst.argumentSources.Length; i++)
                ValidateSource(statusList, inst.argumentSources[i]);

            InitDestination(statusList, inst.destination);
        }
    }
}
