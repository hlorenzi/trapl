﻿using System.Collections.Generic;


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
            if (!this.foundErrors)
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
                InitStatus baseStatus;
                Core.Type baseType;
                GetStatusRecursive(statusList, accessField.baseAccess, out baseStatus, out baseType);

                if (baseStatus == null || !baseStatus.IsInitialized())
                    return false;

                if (!baseStatus.hasFields)
                    return false;

                return baseStatus.fieldStatuses[accessField.fieldIndex].IsInitialized();
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


        private void InitDestination(List<InitStatus> statusList, Core.DataAccess destination)
        {
            InitStatus baseStatus;
            Core.Type baseType;
            InitDestinationRecursive(statusList, destination, out baseStatus, out baseType);

            if (baseStatus != null)
            {
                if (baseStatus.IsInitialized())
                {
                    var destMut = TypeResolver.GetDataAccessMutability(this.session, this.funct, destination);
                    if (!destMut)
                    {
                        this.foundErrors = true;
                        this.session.AddMessage(
                            Diagnostics.MessageKind.Error,
                            Diagnostics.MessageCode.IncompatibleMutability,
                            "value is not mutable",
                            destination.span);
                    }
                }

                baseStatus.SetStatus(true);
                baseStatus.IncrementCounter();
            }
        }


        private void InitDestinationRecursive(List<InitStatus> statusList, Core.DataAccess access, out InitStatus baseStatus, out Core.Type baseType)
        {
            baseStatus = null;
            baseType = null;

            var accessField = access as Core.DataAccessField;
            if (accessField != null)
            {
                if (!ValidateSource(statusList, accessField.baseAccess))
                    return;

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


        private void CheckMoveStructLiteral(List<InitStatus> statusList, Core.InstructionMoveLiteralStruct inst)
        {
            for (var i = 0; i < inst.fieldSources.Length; i++)
                ValidateSource(statusList, inst.fieldSources[i]);

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
