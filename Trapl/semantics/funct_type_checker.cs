namespace Trapl.Semantics
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
                    var instBranch = (inst as Core.InstructionBranch);
                    if (instBranch != null)
                        CheckBranch(instBranch);

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

                    var instMoveFunct = (inst as Core.InstructionMoveLiteralFunct);
                    if (instMoveFunct != null)
                        CheckMoveFunctLiteral(instMoveFunct);

                    var instMoveCallResult = (inst as Core.InstructionMoveCallResult);
                    if (instMoveCallResult != null)
                        CheckMoveCallResult(instMoveCallResult);
                }
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
            var destType = TypeResolver.GetDataAccessType(this.session, this.funct, destination);
            if (destType == null)
                return;

            if (!srcType.IsSame(destType) &&
                ShouldDiagnose(srcType) &&
                ShouldDiagnose(destType))
            {
                var destReg = destination as Core.DataAccessRegister;
                if (destReg != null && destReg.registerIndex == 0)
                {
                    this.foundErrors = true;
                    this.session.AddMessage(
                        Diagnostics.MessageKind.Error,
                        Diagnostics.MessageCode.IncompatibleTypes,
                        "returning '" + srcType.GetString(this.session) + "' " +
                        "but expecting '" + destType.GetString(this.session) + "'",
                        srcSpan);
                }
                else
                {
                    this.foundErrors = true;
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


        private void CheckBranch(Core.InstructionBranch inst)
        {
            var destType = Core.TypeStruct.Of(session.PrimitiveBool);
            var srcType = TypeResolver.GetDataAccessType(this.session, this.funct, inst.conditionReg);

            if (!srcType.IsSame(destType) &&
                ShouldDiagnose(srcType))
            {
                this.foundErrors = true;
                this.session.AddMessage(
                    Diagnostics.MessageKind.Error,
                    Diagnostics.MessageCode.IncompatibleTypes,
                    "branching on '" + srcType.GetString(this.session) + "'",
                    inst.conditionReg.span);
            }
        }


        private void CheckMoveData(Core.InstructionMoveData inst)
        {
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

            var tupleElements = new Core.Type[inst.sourceElements.Count];
            for (var i = 0; i < inst.sourceElements.Count; i++)
                tupleElements[i] = TypeResolver.GetDataAccessType(this.session, this.funct, inst.sourceElements[i]);

            var srcTuple = Core.TypeTuple.Of(tupleElements);

            CheckMove(inst.destination, srcTuple, inst.span);
        }


        private void CheckMoveFunctLiteral(Core.InstructionMoveLiteralFunct inst)
        {
            var destType = TypeResolver.GetDataAccessType(this.session, this.funct, inst.destination);
            var srcType = this.session.GetFunct(inst.functIndex).MakeFunctType();

            CheckMove(inst.destination, srcType, inst.span);
        }


        private void CheckMoveCallResult(Core.InstructionMoveCallResult inst)
        {
            var destType = TypeResolver.GetDataAccessType(this.session, this.funct, inst.destination);
            var srcType = TypeResolver.GetDataAccessType(this.session, this.funct, inst.callTargetSource);
            var srcFunct = srcType as Core.TypeFunct;

            if (srcFunct == null)
            {
                this.foundErrors = true;
                this.session.AddMessage(
                    Diagnostics.MessageKind.Error,
                    Diagnostics.MessageCode.UncallableType,
                    "calling '" + srcType.GetString(this.session) + "'",
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
                var argType = TypeResolver.GetDataAccessType(this.session, this.funct, inst.argumentSources[i]);
                var paramType = srcFunct.parameterTypes[i];

                if (!paramType.IsSame(argType) &&
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
