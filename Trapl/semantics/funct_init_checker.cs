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
                statusList.Add(new InitStatus(i > 0 && i < funct.parameterNum));

            var segmentVisitCounts = new List<int>();
            for (var i = 0; i < funct.segments.Count; i++)
                segmentVisitCounts.Add(0);

            checker.CheckSegment(0, segmentVisitCounts, statusList);
            checker.CheckUnusedMutabilities();
            return checker.foundErrors;
        }


        private Core.Session session;
        private Core.DeclFunct funct;
        private bool foundErrors;
        private HashSet<Diagnostics.Span> alreadyReportedSet = new HashSet<Diagnostics.Span>();
        private List<List<InitStatus>> everyPathInitStatuses = new List<List<InitStatus>>();


        private class InitStatus
        {
            public int curInitCounter;
            public int maxInitCounter;
            public bool takenMutAddr;
            public bool fullyInitialized;


            public InitStatus()
            {

            }


            public InitStatus(bool fullyInitialized)
            {
                this.fullyInitialized = fullyInitialized;
            }


            public InitStatus Clone()
            {
                return new InitStatus
                {
                    fullyInitialized = fullyInitialized,
                    curInitCounter = curInitCounter,
                    maxInitCounter = maxInitCounter,
                    takenMutAddr = takenMutAddr
                };
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
                this.curInitCounter++;
                this.maxInitCounter = System.Math.Max(this.maxInitCounter, this.curInitCounter);
            }
        }


        private FunctInitChecker(Core.Session session, Core.DeclFunct funct)
        {
            this.session = session;
            this.funct = funct;
            this.foundErrors = false;
        }


        private void CheckUnusedMutabilities()
        {
            foreach (var binding in this.funct.localBindings)
            {
                var maxInitCounter = 0;
                var takenMutAddr = false;
                foreach (var statusList in this.everyPathInitStatuses)
                {
                    maxInitCounter = System.Math.Max(
                        maxInitCounter,
                        statusList[binding.registerIndex].maxInitCounter);

                    takenMutAddr |= statusList[binding.registerIndex].takenMutAddr;
                }

                var regMutability = this.funct.registerMutabilities[binding.registerIndex];

                if (regMutability && maxInitCounter == 1 && !takenMutAddr)
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


        private void CheckSegment(int segmentIndex, List<int> segmentVisitCounts, List<InitStatus> statusList)
        {
            if (segmentVisitCounts[segmentIndex] >= 2)
            {
                everyPathInitStatuses.Add(statusList);
                return;
            }

            segmentVisitCounts[segmentIndex]++;

            foreach (var inst in this.funct.segments[segmentIndex].instructions)
            {
                var instDeinit = (inst as Core.InstructionDeinit);
                if (instDeinit != null)
                    CheckDeinit(statusList, instDeinit);

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
                this.CheckSegment(flowBranch.destinationSegmentIfTaken, new List<int>(segmentVisitCounts), CloneStatuses(statusList));
                this.CheckSegment(flowBranch.destinationSegmentIfNotTaken, new List<int>(segmentVisitCounts), CloneStatuses(statusList));
                return;
            }

            var flowGoto = (flow as Core.SegmentFlowGoto);
            if (flowGoto != null)
            {
                this.CheckSegment(flowGoto.destinationSegment, new List<int>(segmentVisitCounts), statusList);
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
                everyPathInitStatuses.Add(statusList);
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


        private void MarkTakenMutAddr(List<InitStatus> statusList, Core.DataAccess destination)
        {
            MarkTakenMutAddrRecursive(statusList, destination);
        }


        private void MarkTakenMutAddrRecursive(List<InitStatus> statusList, Core.DataAccess access)
        {
            var accessField = access as Core.DataAccessField;
            if (accessField != null)
            {
                MarkTakenMutAddrRecursive(statusList, accessField.baseAccess);
                return;
            }

            var accessReg = access as Core.DataAccessRegister;
            if (accessReg != null)
            {
                statusList[accessReg.registerIndex].takenMutAddr = true;
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


        private void CheckDeinit(List<InitStatus> statusList, Core.InstructionDeinit inst)
        {
            statusList[inst.registerIndex].SetStatus(false);
            statusList[inst.registerIndex].curInitCounter = 0;
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

            if (inst.mutable)
                MarkTakenMutAddr(statusList, inst.source);

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
