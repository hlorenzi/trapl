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
                    statusList.Add(new InitStatus(false));
            }

            checker.CheckSegment(0, statusList);
            checker.CheckUnusedMutabilities(statusList);
            return checker.foundErrors;
        }


        private Core.Session session;
        private Core.DeclFunct funct;
        private bool foundErrors;
        private HashSet<Diagnostics.Span> alreadyReportedSet = new HashSet<Diagnostics.Span>();


        private class InitStatus
        {
            public int initCounter;
            public bool fullyInitialized;


            public InitStatus(bool fullyInitialized)
            {
                this.fullyInitialized = fullyInitialized;
            }


            public InitStatus Clone()
            {
                return new InitStatus(this.fullyInitialized);
            }


            public void SetStatus(bool initialized)
            {
                this.fullyInitialized = initialized;
            }


            public bool IsInitialized()
            {
                return this.fullyInitialized;
            }

            
            public void IncrementCounter()
            {
                this.initCounter++;
            }
        }


        private FunctInitChecker(Core.Session session, Core.DeclFunct funct)
        {
            this.session = session;
            this.funct = funct;
            this.foundErrors = false;
        }


        private void CheckUnusedMutabilities(List<InitStatus> statusList)
        {
            foreach (var binding in this.funct.localBindings)
            {
                var status = statusList[binding.registerIndex];
                var regMutability = this.funct.registerMutabilities[binding.registerIndex];

                if (regMutability && status.initCounter == 1)
                {
                    this.session.AddMessage(
                        Diagnostics.MessageKind.Warning,
                        Diagnostics.MessageCode.UnusedMutability,
                        "'" + binding.name.GetString() + "' does not need to be mutable",
                        binding.declSpan);
                    this.foundErrors = true;
                }
            }
        }


        private void CheckSegment(int segmentIndex, List<InitStatus> statusList)
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

                var instMoveStruct = (inst as Core.InstructionMoveLiteralStruct);
                if (instMoveStruct != null)
                    CheckMoveStructLiteral(statusList, instMoveStruct);

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
                this.CheckSegment(flowBranch.destinationSegmentIfTaken, CloneStatuses(statusList));
                this.CheckSegment(flowBranch.destinationSegmentIfNotTaken, CloneStatuses(statusList));
                return;
            }

            var flowGoto = (flow as Core.SegmentFlowGoto);
            if (flowGoto != null)
            {
                this.CheckSegment(flowGoto.destinationSegment, statusList);
                return;
            }

            var flowEnd = (flow as Core.SegmentFlowEnd);
            if (flowEnd != null)
            {
                // Generate a void return.
                if (funct.GetReturnType().IsEmptyTuple() &&
                    !CheckSourceRecursive(statusList, Core.DataAccessRegister.ForRegister(flowEnd.span, 0)))
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


        private bool ValidateSource(List<InitStatus> statusList, Core.DataAccess source)
        {
            var initialized = CheckSourceRecursive(statusList, source);
            if (!initialized &&
                !this.alreadyReportedSet.Contains(source.span))
            {
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

            return initialized;
        }


        private bool CheckSourceRecursive(List<InitStatus> statusList, Core.DataAccess access)
        {
            var accessField = access as Core.DataAccessField;
            if (accessField != null)
            {
                return CheckSourceRecursive(statusList, accessField.baseAccess);
            }

            var accessReg = access as Core.DataAccessRegister;
            if (accessReg != null)
            {
                return statusList[accessReg.registerIndex].IsInitialized();
            }

            var accessDeref = access as Core.DataAccessDereference;
            if (accessDeref != null)
            {
                return CheckSourceRecursive(statusList, accessDeref.innerAccess);
            }

            return false;
        }


        private void CheckAndInitDestination(List<InitStatus> statusList, Core.DataAccess destination)
        {
            CheckAndInitDestinationRecursive(statusList, destination);
        }


        private void CheckAndInitDestinationRecursive(List<InitStatus> statusList, Core.DataAccess access)
        {
            var accessField = access as Core.DataAccessField;
            if (accessField != null)
            {
                if (ValidateSource(statusList, accessField.baseAccess))
                    CheckAndInitDestinationRecursive(statusList, accessField.baseAccess);

                return;
            }

            var accessReg = access as Core.DataAccessRegister;
            if (accessReg != null)
            {
                var status = statusList[accessReg.registerIndex];

                if (status.IsInitialized())
                {
                    var destMut = TypeResolver.GetDataAccessMutability(this.session, this.funct, accessReg);
                    if (!destMut)
                    {
                        this.foundErrors = true;
                        this.session.AddMessage(
                            Diagnostics.MessageKind.Error,
                            Diagnostics.MessageCode.IncompatibleMutability,
                            "value is not mutable",
                            accessReg.span);
                    }
                }

                status.SetStatus(true);
                status.IncrementCounter();
                return;
            }

            var accessDeref = access as Core.DataAccessDereference;
            if (accessDeref != null)
            {
                if (ValidateSource(statusList, accessDeref.innerAccess))
                {
                    var destType = TypeResolver.GetDataAccessType(this.session, this.funct, accessDeref.innerAccess);
                    var destPtr = destType as Core.TypePointer;
                    if (destPtr != null && !destPtr.mutable)
                    {
                        this.foundErrors = true;
                        this.session.AddMessage(
                            Diagnostics.MessageKind.Error,
                            Diagnostics.MessageCode.IncompatibleMutability,
                            "value through pointer is not mutable",
                            accessDeref.innerAccess.span);
                    }
                }

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
            CheckAndInitDestination(statusList, inst.destination);
        }


        private void CheckMoveBoolLiteral(List<InitStatus> statusList, Core.InstructionMoveLiteralBool inst)
        {
            CheckAndInitDestination(statusList, inst.destination);
        }


        private void CheckMoveIntLiteral(List<InitStatus> statusList, Core.InstructionMoveLiteralInt inst)
        {
            CheckAndInitDestination(statusList, inst.destination);
        }


        private void CheckMoveTupleLiteral(List<InitStatus> statusList, Core.InstructionMoveLiteralTuple inst)
        {
            for (var i = 0; i < inst.sourceElements.Length; i++)
                ValidateSource(statusList, inst.sourceElements[i]);

            CheckAndInitDestination(statusList, inst.destination);
        }


        private void CheckMoveStructLiteral(List<InitStatus> statusList, Core.InstructionMoveLiteralStruct inst)
        {
            for (var i = 0; i < inst.fieldSources.Length; i++)
                ValidateSource(statusList, inst.fieldSources[i]);

            CheckAndInitDestination(statusList, inst.destination);
        }


        private void CheckMoveAddr(List<InitStatus> statusList, Core.InstructionMoveAddr inst)
        {
            ValidateSource(statusList, inst.source);
            CheckAndInitDestination(statusList, inst.destination);
        }


        private void CheckMoveFunctLiteral(List<InitStatus> statusList, Core.InstructionMoveLiteralFunct inst)
        {
            CheckAndInitDestination(statusList, inst.destination);
        }


        private void CheckMoveCallResult(List<InitStatus> statusList, Core.InstructionMoveCallResult inst)
        {
            ValidateSource(statusList, inst.callTargetSource);

            for (var i = 0; i < inst.argumentSources.Length; i++)
                ValidateSource(statusList, inst.argumentSources[i]);

            CheckAndInitDestination(statusList, inst.destination);
        }
    }
}
