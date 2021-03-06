﻿namespace Trapl.Semantics
{
    public class FunctTypeChecker
    {
        public static bool Check(Core.Session session, Core.DeclFunct funct)
        {
            var checker = new FunctTypeChecker(session, funct);
            checker.Check();
            return checker.foundErrors;
        }


        private Core.Session session;
        private Core.DeclFunct funct;
        private bool foundErrors;


        private FunctTypeChecker(Core.Session session, Core.DeclFunct funct)
        {
            this.session = session;
            this.funct = funct;
            this.foundErrors = false;
        }


        private void Check()
        {
            foreach (var segment in this.funct.segments)
            {
                foreach (var inst in segment.instructions)
                {
                    var instMoveData = (inst as Core.InstructionMoveData);
                    if (instMoveData != null)
                        CheckMoveData(instMoveData);

                    var instMoveLitBool = (inst as Core.InstructionMoveLiteralBool);
                    if (instMoveLitBool != null)
                        CheckMoveBoolLiteral(instMoveLitBool);

                    var instMoveLitInt = (inst as Core.InstructionMoveLiteralInt);
                    if (instMoveLitInt != null)
                        CheckMoveIntLiteral(instMoveLitInt);

                    var instMoveTuple = (inst as Core.InstructionMoveLiteralTuple);
                    if (instMoveTuple != null)
                        CheckMoveTupleLiteral(instMoveTuple);

                    var instMoveStruct = (inst as Core.InstructionMoveLiteralStruct);
                    if (instMoveStruct != null)
                        CheckMoveStructLiteral(instMoveStruct);

                    var instMoveAddr = (inst as Core.InstructionMoveAddr);
                    if (instMoveAddr != null)
                        CheckMoveAddr(instMoveAddr);

                    var instMoveFunct = (inst as Core.InstructionMoveLiteralFunct);
                    if (instMoveFunct != null)
                        CheckMoveFunctLiteral(instMoveFunct);

                    var instMoveCallResult = (inst as Core.InstructionMoveCallResult);
                    if (instMoveCallResult != null)
                        CheckMoveCallResult(instMoveCallResult);
                }

                var flowBranch = (segment.outFlow as Core.SegmentFlowBranch);
                if (flowBranch != null)
                    CheckBranch(flowBranch);
            }

            foreach (var binding in this.funct.localBindings)
            {
                var bindingType = this.funct.registerTypes[binding.registerIndex];
                if (!bindingType.IsResolved())
                {
                    this.session.AddMessage(
                        Diagnostics.MessageKind.Error,
                        Diagnostics.MessageCode.InferenceFailed,
                        "cannot infer type of '" + binding.name.GetString() + "'",
                        binding.declSpan);
                    this.foundErrors = true;
                }
            }

            if (!this.foundErrors)
            {
                for (var i = 0; i < this.funct.registerTypes.Count; i++)
                {
                    var regType = this.funct.registerTypes[i];
                    if (!regType.IsResolved())
                    {
                        this.session.AddMessage(
                            Diagnostics.MessageKind.Internal,
                            Diagnostics.MessageCode.InferenceFailed,
                            "cannot infer type of #r" + i);
                    }
                }
            }
        }


        private bool ShouldDiagnose(Core.Type type)
        {
            return type.IsResolved() && !type.IsError();
        }


        private void CheckMove(Core.DataAccess destination, Core.Type srcType, Diagnostics.Span srcSpan)
        {
            if (!TypeResolver.ValidateDataAccess(this.session, this.funct, destination))
            {
                this.foundErrors = true;
                return;
            }

            var destType = TypeResolver.GetDataAccessType(this.session, this.funct, destination);
            if (destType == null)
                return;

            if (!srcType.IsConvertibleTo(destType) &&
                ShouldDiagnose(srcType) &&
                ShouldDiagnose(destType))
            {
                this.foundErrors = true;

                var destReg = destination as Core.DataAccessRegister;
                if (destReg != null && destReg.registerIndex == 0)
                {
                    this.session.AddMessage(
                        Diagnostics.MessageKind.Error,
                        Diagnostics.MessageCode.IncompatibleTypes,
                        "returning '" + srcType.GetString(this.session) + "' " +
                        "but expecting '" + destType.GetString(this.session) + "'",
                        srcSpan,
                        destination.span);
                }
                else
                {
                    this.session.AddMessage(
                        Diagnostics.MessageKind.Error,
                        Diagnostics.MessageCode.IncompatibleTypes,
                        "moving '" + srcType.GetString(this.session) + "' " +
                        "into '" + destType.GetString(this.session) + "'",
                        srcSpan,
                        destination.span);
                }
            }
        }


        private void CheckBranch(Core.SegmentFlowBranch flow)
        {
            if (!TypeResolver.ValidateDataAccess(this.session, this.funct, flow.conditionReg))
            {
                this.foundErrors = true;
                return;
            }

            var destType = Core.TypeStruct.Of(session.PrimitiveBool);
            var srcType = TypeResolver.GetDataAccessType(this.session, this.funct, flow.conditionReg);

            if (!srcType.IsConvertibleTo(destType) &&
                ShouldDiagnose(srcType))
            {
                this.foundErrors = true;
                this.session.AddMessage(
                    Diagnostics.MessageKind.Error,
                    Diagnostics.MessageCode.IncompatibleTypes,
                    "using '" + srcType.GetString(this.session) + "' as condition",
                    flow.conditionReg.span);
            }
        }


        private void CheckMoveData(Core.InstructionMoveData inst)
        {
            if (!TypeResolver.ValidateDataAccess(this.session, this.funct, inst.source))
            {
                this.foundErrors = true;
                return;
            }

            CheckMove(
                inst.destination,
                TypeResolver.GetDataAccessType(this.session, this.funct, inst.source),
                inst.source.span);
        }


        private void CheckMoveBoolLiteral(Core.InstructionMoveLiteralBool inst)
        {
            var destType = TypeResolver.GetDataAccessType(this.session, this.funct, inst.destination);
            CheckMove(inst.destination, Core.TypeStruct.Of(session.PrimitiveBool), inst.span);
        }


        private void CheckMoveIntLiteral(Core.InstructionMoveLiteralInt inst)
        {
            var destType = TypeResolver.GetDataAccessType(this.session, this.funct, inst.destination);
            CheckMove(inst.destination, inst.type, inst.span);
        }


        private void CheckMoveTupleLiteral(Core.InstructionMoveLiteralTuple inst)
        {
            var destType = TypeResolver.GetDataAccessType(this.session, this.funct, inst.destination);

            var tupleElements = new Core.Type[inst.sourceElements.Length];
            for (var i = 0; i < inst.sourceElements.Length; i++)
            {
                if (!TypeResolver.ValidateDataAccess(this.session, this.funct, inst.sourceElements[i]))
                {
                    this.foundErrors = true;
                    return;
                }

                tupleElements[i] = TypeResolver.GetDataAccessType(this.session, this.funct, inst.sourceElements[i]);
            }

            var srcTuple = Core.TypeTuple.Of(tupleElements);

            CheckMove(inst.destination, srcTuple, inst.span);
        }


        private void CheckMoveStructLiteral(Core.InstructionMoveLiteralStruct inst)
        {
            for (var i = 0; i < inst.fieldSources.Length; i++)
            {
                if (!TypeResolver.ValidateDataAccess(this.session, this.funct, inst.fieldSources[i]))
                {
                    this.foundErrors = true;
                    return;
                }

                var fieldDestType = TypeResolver.GetFieldType(this.session, Core.TypeStruct.Of(inst.structIndex), i);
                var fieldSrcType = TypeResolver.GetDataAccessType(this.session, this.funct, inst.fieldSources[i]);

                if (!fieldSrcType.IsConvertibleTo(fieldDestType) &&
                    ShouldDiagnose(fieldSrcType))
                {
                    this.foundErrors = true;
                    this.session.AddMessage(
                        Diagnostics.MessageKind.Error,
                        Diagnostics.MessageCode.IncompatibleTypes,
                        "moving '" + fieldSrcType.GetString(this.session) + "' " +
                        "into '" + fieldDestType.GetString(this.session) + "' field",
                        inst.fieldSources[i].span,
                        inst.fieldDestSpans[i]);
                }
            }

            var destType = TypeResolver.GetDataAccessType(this.session, this.funct, inst.destination);
            CheckMove(inst.destination, Core.TypeStruct.Of(inst.structIndex), inst.span);
        }


        private void CheckMoveAddr(Core.InstructionMoveAddr inst)
        {
            if (!TypeResolver.ValidateDataAccess(this.session, this.funct, inst.source))
            {
                this.foundErrors = true;
                return;
            }

            var srcMut = TypeResolver.GetDataAccessMutability(this.session, this.funct, inst.source);
            if (inst.mutable && !srcMut)
            {
                this.foundErrors = true;
                this.session.AddMessage(
                    Diagnostics.MessageKind.Error,
                    Diagnostics.MessageCode.IncompatibleMutability,
                    "value is not mutable",
                    inst.source.span);
            }

            var destType = TypeResolver.GetDataAccessType(this.session, this.funct, inst.destination);
            var srcType = TypeResolver.GetDataAccessType(this.session, this.funct, inst.source);
            var srcPtr = Core.TypePointer.Of(inst.mutable, srcType);

            CheckMove(inst.destination, srcPtr, inst.span);
        }


        private void CheckMoveFunctLiteral(Core.InstructionMoveLiteralFunct inst)
        {
            var destType = TypeResolver.GetDataAccessType(this.session, this.funct, inst.destination);
            var srcType = this.session.GetFunct(inst.functIndex).MakeFunctType();

            CheckMove(inst.destination, srcType, inst.span);
        }


        private void CheckMoveCallResult(Core.InstructionMoveCallResult inst)
        { 
            if (!TypeResolver.ValidateDataAccess(this.session, this.funct, inst.callTargetSource))
            {
                this.foundErrors = true;
                return;
            }

            var destType = TypeResolver.GetDataAccessType(this.session, this.funct, inst.destination);
            var srcType = TypeResolver.GetDataAccessType(this.session, this.funct, inst.callTargetSource);
            var srcFunct = srcType as Core.TypeFunct;

            if (srcFunct == null)
            {
                this.foundErrors = true;
                this.session.AddMessage(
                    Diagnostics.MessageKind.Error,
                    Diagnostics.MessageCode.UncallableType,
                    "calling '" + srcType.GetString(this.session) + "', which is not a funct",
                    inst.callTargetSource.span);
                return;
            }

            if (inst.argumentSources.Length != srcFunct.parameterTypes.Length)
            {
                this.foundErrors = true;
                this.session.AddMessage(
                    Diagnostics.MessageKind.Error,
                    Diagnostics.MessageCode.WrongNumberOfArguments,
                    "passing the wrong number of arguments to '" + srcType.GetString(this.session) + "'",
                    inst.span);
                return;
            }

            for (var i = 0; i < inst.argumentSources.Length; i++)
            {
                if (!TypeResolver.ValidateDataAccess(this.session, this.funct, inst.argumentSources[i]))
                {
                    this.foundErrors = true;
                    return;
                }

                var argType = TypeResolver.GetDataAccessType(this.session, this.funct, inst.argumentSources[i]);
                var paramType = srcFunct.parameterTypes[i];

                if (!paramType.IsConvertibleTo(argType) &&
                    ShouldDiagnose(paramType) &&
                    ShouldDiagnose(argType))
                {
                    this.foundErrors = true;
                    this.session.AddMessage(
                        Diagnostics.MessageKind.Error,
                        Diagnostics.MessageCode.IncompatibleTypes,
                        "passing '" + argType.GetString(this.session) + "' " +
                        "into '" + paramType.GetString(this.session) + "' parameter",
                        inst.argumentSources[i].span);
                }
            }
                    
            CheckMove(inst.destination, srcFunct.returnType, inst.span);
        }
    }
}
