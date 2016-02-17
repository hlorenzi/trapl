using System.Collections.Generic;


namespace Trapl.Semantics
{
    public class FunctChecker
    {
        public static void Check(Core.Session session, Core.DeclFunct funct)
        {
            var checker = new FunctChecker(session, funct);
            checker.Check();
        }


        private Core.Session session;
        private Core.DeclFunct funct;


        private FunctChecker(Core.Session session, Core.DeclFunct funct)
        {
            this.session = session;
            this.funct = funct;
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
        }


        private Core.Type GetDataAccessType(Core.DataAccess access)
        {
            var regAccess = access as Core.DataAccessRegister;
            if (regAccess != null)
                return this.funct.registerTypes[regAccess.registerIndex];

            return null;
        }


        private bool ShouldDiagnose(Core.Type type)
        {
            return type.IsResolved() && !type.IsError();
        }


        private void CheckMove(Core.DataAccess destination, Core.Type srcType, Diagnostics.Span srcSpan)
        {
            var destType = GetDataAccessType(destination);
            if (destType == null)
                return;

            if (!srcType.IsSame(destType) &&
                ShouldDiagnose(srcType) &&
                ShouldDiagnose(destType))
            {
                var destReg = destination as Core.DataAccessRegister;
                if (destReg != null && destReg.registerIndex == 0)
                {
                    this.session.AddMessage(
                        Diagnostics.MessageKind.Error,
                        Diagnostics.MessageCode.IncompatibleTypes,
                        "returning '" + srcType.GetString(this.session) + "' " +
                        "but expecting '" + destType.GetString(this.session) + "'",
                        srcSpan);
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


        private void CheckMoveData(Core.InstructionMoveData inst)
        {
            CheckMove(inst.destination, GetDataAccessType(inst.source), inst.source.span);
        }


        private void CheckMoveTupleLiteral(Core.InstructionMoveLiteralTuple inst)
        {
            var destType = GetDataAccessType(inst.destination);

            var tupleElements = new Core.Type[inst.sourceElements.Count];
            for (var i = 0; i < inst.sourceElements.Count; i++)
                tupleElements[i] = GetDataAccessType(inst.sourceElements[i]);

            var srcTuple = Core.TypeTuple.Of(tupleElements);

            CheckMove(inst.destination, srcTuple, inst.span);
        }


        private void CheckMoveFunctLiteral(Core.InstructionMoveLiteralFunct inst)
        {
            var destType = GetDataAccessType(inst.destination);
            var srcType = this.session.GetFunct(inst.functIndex).MakeFunctType();

            CheckMove(inst.destination, srcType, inst.span);
        }


        private void CheckMoveCallResult(Core.InstructionMoveCallResult inst)
        {
            var destType = GetDataAccessType(inst.destination);
            var srcType = GetDataAccessType(inst.callTargetSource);
            var srcFunct = srcType as Core.TypeFunct;

            if (srcFunct == null)
            {
                this.session.AddMessage(
                    Diagnostics.MessageKind.Error,
                    Diagnostics.MessageCode.UncallableType,
                    "calling '" + srcType.GetString(this.session) + "'",
                    inst.callTargetSource.span);
                return;
            }

            if (inst.argumentSources.Length != srcFunct.parameterTypes.Length)
            {
                this.session.AddMessage(
                    Diagnostics.MessageKind.Error,
                    Diagnostics.MessageCode.WrongNumberOfArguments,
                    "passing the wrong number of arguments to '" + srcType.GetString(this.session) + "'",
                    inst.span);
                return;
            }

            for (var i = 0; i < inst.argumentSources.Length; i++)
            {
                var argType = GetDataAccessType(inst.argumentSources[i]);
                var paramType = srcFunct.parameterTypes[i];

                if (!paramType.IsSame(argType) &&
                    ShouldDiagnose(paramType) &&
                    ShouldDiagnose(argType))
                {
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
