using System.Collections.Generic;


namespace Trapl.Semantics
{
    public class FunctBodyResolver
    {
        public static bool Resolve(
            Core.Session session,
            Core.DeclFunct funct,
            Core.UseDirective[] useDirectives,
            Grammar.ASTNodeExpr bodyExpr)
        {
            var resolver = new FunctBodyResolver(session, funct, useDirectives);
            resolver.Resolve(bodyExpr);
            return resolver.foundErrors;
        }


        private Core.Session session;
        private Core.DeclFunct funct;
        private Core.UseDirective[] useDirectives;
        private bool foundErrors;
        private List<bool> localScopeLivenesses;


        private FunctBodyResolver(Core.Session session, Core.DeclFunct funct, Core.UseDirective[] useDirectives)
        {
            this.session = session;
            this.funct = funct;
            this.useDirectives = useDirectives;
            this.foundErrors = false;
            this.localScopeLivenesses = new List<bool>();
        }


        private void Resolve(Grammar.ASTNodeExpr bodyExpr)
        {
            var curSegment = funct.CreateSegment(bodyExpr.GetSpan().JustBefore());

            ResolveExpr(bodyExpr, ref curSegment, new Core.DataAccessDiscard());

            if (funct.segments[curSegment].outFlow == null)
                funct.SetSegmentFlow(curSegment,
                    new Core.SegmentFlowEnd { span = funct.segments[curSegment].GetSpan().JustAfter() });
        }


        private void ResolveExpr(Grammar.ASTNodeExpr expr, ref int curSegment, Core.DataAccess outputReg)
        {
            if (expr is Grammar.ASTNodeExprParenthesized)
                this.ResolveExpr(((Grammar.ASTNodeExprParenthesized)expr).innerExpr, ref curSegment, outputReg);
            else if (expr is Grammar.ASTNodeExprBlock)
                this.ResolveExprBlock((Grammar.ASTNodeExprBlock)expr, ref curSegment, outputReg);
            else if (expr is Grammar.ASTNodeExprIf)
                this.ResolveExprIf((Grammar.ASTNodeExprIf)expr, ref curSegment, outputReg);
            else if (expr is Grammar.ASTNodeExprLet)
                this.ResolveExprLet((Grammar.ASTNodeExprLet)expr, ref curSegment, outputReg);
            else if (expr is Grammar.ASTNodeExprReturn)
                this.ResolveExprReturn((Grammar.ASTNodeExprReturn)expr, ref curSegment, outputReg);
            else if (expr is Grammar.ASTNodeExprCall)
                this.ResolveExprCall((Grammar.ASTNodeExprCall)expr, ref curSegment, outputReg);
            else if (expr is Grammar.ASTNodeExprUnaryOp)
                this.ResolveExprUnaryOp((Grammar.ASTNodeExprUnaryOp)expr, ref curSegment, outputReg);
            else if (expr is Grammar.ASTNodeExprBinaryOp)
                this.ResolveExprBinaryOp((Grammar.ASTNodeExprBinaryOp)expr, ref curSegment, outputReg);
            else if (expr is Grammar.ASTNodeExprName)
                this.ResolveExprName((Grammar.ASTNodeExprName)expr, ref curSegment, outputReg);
            else if (expr is Grammar.ASTNodeExprLiteralBool)
                this.ResolveExprLiteralBool((Grammar.ASTNodeExprLiteralBool)expr, ref curSegment, outputReg);
            else if (expr is Grammar.ASTNodeExprLiteralInt)
                this.ResolveExprLiteralInt((Grammar.ASTNodeExprLiteralInt)expr, ref curSegment, outputReg);
            else if (expr is Grammar.ASTNodeExprLiteralStruct)
                this.ResolveExprLiteralStruct((Grammar.ASTNodeExprLiteralStruct)expr, ref curSegment, outputReg);
            else
                throw new System.NotImplementedException();
        }


        private void ResolveExprBlock(Grammar.ASTNodeExprBlock exprBlock, ref int curSegment, Core.DataAccess outputReg)
        {
            // Generate an empty tuple store if there are no subexpressions.
            if (exprBlock.subexprs.Count == 0)
            {
                funct.AddInstruction(curSegment,
                    Core.InstructionMoveLiteralTuple.Empty(exprBlock.GetSpan().Displace(1, -1), outputReg));
                return;
            }

            // Store local scope index before block.
            var localScopeIndexBefore = localScopeLivenesses.Count;

            for (int i = 0; i < exprBlock.subexprs.Count; i++)
            {
                try
                {
                    var subexprOutput = outputReg;
                    if (i < exprBlock.subexprs.Count - 1)
                        subexprOutput = new Core.DataAccessDiscard();

                    this.ResolveExpr(exprBlock.subexprs[i], ref curSegment, subexprOutput);
                }
                catch (Core.CheckException) { }
            }

            // Mark locals inside block as out-of-scope.
            for (var i = localScopeIndexBefore; i < localScopeLivenesses.Count; i++)
                localScopeLivenesses[i] = false;
        }


        private void ResolveExprLet(Grammar.ASTNodeExprLet exprLet, ref int curSegment, Core.DataAccess output)
        {
            // Create a new storage location and name binding.
            var registerIndex = funct.CreateRegister(new Core.TypePlaceholder(), exprLet.mutable);

            funct.CreateBinding(
                NameResolver.Resolve(((Grammar.ASTNodeExprNameConcrete)exprLet.name).name),
                registerIndex,
                exprLet.name.GetSpan());

            localScopeLivenesses.Add(true);

            // Parse type annotation, if there is one.
            if (exprLet.type != null)
            {
                funct.registerTypes[registerIndex] =
                    TypeResolver.Resolve(session, exprLet.type, useDirectives, false);
            }

            // Parse init expression, if there is one.
            if (exprLet.initExpr != null)
            {
                ResolveExpr(exprLet.initExpr, ref curSegment,
                    Core.DataAccessRegister.ForRegister(exprLet.name.GetSpan(), registerIndex));
            }

            // Generate a void store.
            funct.AddInstruction(curSegment,
                Core.InstructionMoveLiteralTuple.Empty(exprLet.GetSpan(), output));
        }


        private void ResolveExprIf(Grammar.ASTNodeExprIf exprIf, ref int curSegment, Core.DataAccess output)
        {
            // Parse condition.
            var conditionReg = Core.DataAccessRegister.ForRegister(
                exprIf.conditionExpr.GetSpan(),
                funct.CreateRegister(new Core.TypePlaceholder(), false));

            this.ResolveExpr(
                exprIf.conditionExpr,
                ref curSegment,
                conditionReg);

            var flowBranch = new Core.SegmentFlowBranch { conditionReg = conditionReg };
            funct.SetSegmentFlow(curSegment, flowBranch);

            // Parse true branch.
            var trueSegment = funct.CreateSegment(exprIf.trueBranchExpr.GetSpan().JustBefore());
            flowBranch.destinationSegmentIfTaken = trueSegment;

            ResolveExpr(exprIf.trueBranchExpr, ref trueSegment, output);

            // Parse false branch, if there is one.
            if (exprIf.falseBranchExpr != null)
            {
                var falseSegment = funct.CreateSegment(exprIf.falseBranchExpr.GetSpan().JustBefore());
                flowBranch.destinationSegmentIfNotTaken = falseSegment;

                ResolveExpr(exprIf.falseBranchExpr, ref falseSegment, output);

                var afterSegment = funct.CreateSegment(exprIf.GetSpan().JustAfter());
                funct.SetSegmentFlow(trueSegment, Core.SegmentFlowGoto.To(afterSegment));
                funct.SetSegmentFlow(falseSegment, Core.SegmentFlowGoto.To(afterSegment));
                curSegment = afterSegment;
            }
            // Or else, just route the false segment path to the next segment.
            else
            {
                var afterSegment = funct.CreateSegment(exprIf.GetSpan().JustAfter());
                funct.SetSegmentFlow(trueSegment, Core.SegmentFlowGoto.To(afterSegment));
                flowBranch.destinationSegmentIfNotTaken = afterSegment;
                curSegment = afterSegment;
            }
        }


        private void ResolveExprReturn(Grammar.ASTNodeExprReturn exprRet, ref int curSegment, Core.DataAccess output)
        {
            // Generate a void store.
            funct.AddInstruction(curSegment,
                Core.InstructionMoveLiteralTuple.Empty(exprRet.GetSpan(), output));

            // Parse returned expr, if there is one.
            if (exprRet.expr != null)
            {
                ResolveExpr(exprRet.expr, ref curSegment,
                    Core.DataAccessRegister.ForRegister(exprRet.expr.GetSpan(), 0));

                funct.SetSegmentFlow(curSegment, new Core.SegmentFlowEnd());
            }
            // Else, return a void.
            else
            {
                funct.AddInstruction(curSegment,
                    Core.InstructionMoveLiteralTuple.Empty(
                        exprRet.GetSpan(), Core.DataAccessRegister.ForRegister(exprRet.expr.GetSpan(), 0)));

                funct.SetSegmentFlow(curSegment, new Core.SegmentFlowEnd());
            }

            // Create next unlinked segment.
            curSegment = funct.CreateSegment(exprRet.GetSpan().JustAfter());
        }


        private void ResolveExprCall(Grammar.ASTNodeExprCall exprCall, ref int curSegment, Core.DataAccess output)
        {
            // Parse called expression.
            var callTargetReg = Core.DataAccessRegister.ForRegister(
                exprCall.calledExpr.GetSpan(),
                funct.CreateRegister(new Core.TypePlaceholder(), false));

            ResolveExpr(exprCall.calledExpr, ref curSegment, callTargetReg);

            // Parse argument expressions.
            var argumentRegs = new Core.DataAccess[exprCall.argumentExprs.Count];

            for (var i = 0; i < exprCall.argumentExprs.Count; i++)
            {
                argumentRegs[i] = Core.DataAccessRegister.ForRegister(
                    exprCall.argumentExprs[i].GetSpan(),
                    funct.CreateRegister(new Core.TypePlaceholder(), false));

                ResolveExpr(exprCall.argumentExprs[i], ref curSegment, argumentRegs[i]);
            }

            // Generate call instruction.
            funct.AddInstruction(curSegment,
                Core.InstructionMoveCallResult.For(exprCall.GetSpan(), output, callTargetReg, argumentRegs));
        }


        private void ResolveExprUnaryOp(Grammar.ASTNodeExprUnaryOp exprUnOp, ref int curSegment, Core.DataAccess output)
        {
            if (exprUnOp.oper == Grammar.ASTNodeExprUnaryOp.Operator.Asterisk ||
                exprUnOp.oper == Grammar.ASTNodeExprUnaryOp.Operator.AsteriskMut)
            {
                var mutable = (exprUnOp.oper == Grammar.ASTNodeExprUnaryOp.Operator.AsteriskMut);

                // Parse addressed expression.
                var access = ResolveDataAccess(exprUnOp.operand, ref curSegment, true, mutable);
                if (access == null)
                    return;

                // Store pointer.
                funct.AddInstruction(
                    curSegment,
                    Core.InstructionMoveAddr.Of(exprUnOp.GetSpan(), output, access, mutable));

                return;
            }
            else if (exprUnOp.oper == Grammar.ASTNodeExprUnaryOp.Operator.At)
            {
                // Parse addressed expression.
                var access = ResolveDataAccess(exprUnOp, ref curSegment, true, false);
                if (access == null)
                    return;

                // Store pointer.
                funct.AddInstruction(
                    curSegment,
                    Core.InstructionMoveData.Of(exprUnOp.GetSpan(), output, access));

                return;
            }

            throw new System.NotImplementedException();
        }


        private void ResolveExprBinaryOp(Grammar.ASTNodeExprBinaryOp exprBinOp, ref int curSegment, Core.DataAccess output)
        {
            if (exprBinOp.oper == Grammar.ASTNodeExprBinaryOp.Operator.Equal)
            {
                var access = ResolveDataAccess(exprBinOp.lhsOperand, ref curSegment, false, false);
                if (access == null)
                    return;

                // Parse right-hand side expression.
                ResolveExpr(
                    exprBinOp.rhsOperand,
                    ref curSegment,
                    access);

                // Generate a void store.
                funct.AddInstruction(
                    curSegment,
                    Core.InstructionMoveLiteralTuple.Empty(exprBinOp.GetSpan(), output));

                return;
            }
            else if (exprBinOp.oper == Grammar.ASTNodeExprBinaryOp.Operator.Dot)
            {
                var access = ResolveDataAccess(exprBinOp, ref curSegment, false, false);
                if (access == null)
                    return;

                funct.AddInstruction(curSegment,
                    Core.InstructionMoveData.Of(
                        exprBinOp.GetSpan(),
                        output,
                        access));
                return;
            }

            throw new System.NotImplementedException();
        }


        private void ResolveExprLiteralBool(Grammar.ASTNodeExprLiteralBool exprLiteralBool, ref int curSegment, Core.DataAccess output)
        {
            funct.AddInstruction(curSegment,
                Core.InstructionMoveLiteralBool.Of(
                    exprLiteralBool.GetSpan(),
                    output,
                    exprLiteralBool.value));
        }


        private void ResolveExprLiteralInt(Grammar.ASTNodeExprLiteralInt exprLiteralInt, ref int curSegment, Core.DataAccess output)
        {
            funct.AddInstruction(curSegment,
                Core.InstructionMoveLiteralInt.Of(
                    exprLiteralInt.GetSpan(),
                    output,
                    Core.TypeStruct.Of(session.PrimitiveInt),
                    System.Convert.ToInt64(exprLiteralInt.GetExcerpt())));
        }


        private void ResolveExprLiteralStruct(Grammar.ASTNodeExprLiteralStruct exprLiteralStruct, ref int curSegment, Core.DataAccess output)
        {
            var type = TypeResolver.ResolveStruct(session, exprLiteralStruct.name, useDirectives, true);
            if (!type.IsResolved())
                return;

            var typeStruct = type as Core.TypeStruct;
            var fieldNum = TypeResolver.GetFieldNum(this.session, this.funct, typeStruct);
            var fieldDestSpans = new Diagnostics.Span?[fieldNum];

            var fieldRegs = new int[fieldNum];
            for (var i = 0; i < fieldNum; i++)
            {
                fieldRegs[i] = funct.CreateRegister(
                    TypeResolver.GetFieldType(this.session, this.funct, typeStruct, i), false);
            }

            var fieldRegAccesses = new Core.DataAccess[fieldNum];
            for (var i = 0; i < fieldNum; i++)
            {
                fieldRegAccesses[i] =
                    Core.DataAccessRegister.ForRegister(exprLiteralStruct.name.GetSpan(), fieldRegs[i]);
            }

            foreach (var fieldInit in exprLiteralStruct.fields)
            {
                var name = NameResolver.Resolve(fieldInit.name);

                int fieldIndex;
                if (!this.session.GetStruct(typeStruct.structIndex).fieldNames.FindByName(name, out fieldIndex))
                {
                    this.foundErrors = true;
                    session.AddMessage(
                        Diagnostics.MessageKind.Error,
                        Diagnostics.MessageCode.Unknown,
                        "unknown field '" + name.GetString() + "' in '" +
                        type.GetString(this.session) + "'",
                        fieldInit.name.GetSpan());
                    continue;
                }

                if (fieldDestSpans[fieldIndex].HasValue)
                {
                    this.foundErrors = true;
                    session.AddMessage(
                        Diagnostics.MessageKind.Error,
                        Diagnostics.MessageCode.Unknown,
                        "duplicate field '" + name.GetString() + "' initialization",
                        fieldInit.name.GetSpan(),
                        fieldDestSpans[fieldIndex].Value);
                    continue;
                }

                fieldRegAccesses[fieldIndex].span = fieldInit.name.GetSpan();
                fieldDestSpans[fieldIndex] = fieldInit.name.GetSpan();
                this.ResolveExpr(fieldInit.expr, ref curSegment, fieldRegAccesses[fieldIndex]);
            }

            var missingFields = new List<int>();
            for (var i = 0; i < fieldNum; i++)
            {
                if (!fieldDestSpans[i].HasValue)
                    missingFields.Add(i);
            }

            if (missingFields.Count > 0)
            {
                Core.Name fieldName;
                session.GetStruct(typeStruct.structIndex).fieldNames.FindByValue(missingFields[0], out fieldName);

                this.foundErrors = true;
                session.AddMessage(
                    Diagnostics.MessageKind.Error,
                    Diagnostics.MessageCode.Unknown,
                    "missing initializer" + (missingFields.Count > 1 ? "s" : "") +
                    " for field '" + fieldName.GetString() + "'" +
                    (missingFields.Count > 1 ? " and other " + (missingFields.Count - 1) : ""),
                    exprLiteralStruct.GetSpan());
            }
            else
            {
                var finalFieldDestSpans = new Diagnostics.Span[fieldNum];
                for (var i = 0; i < fieldNum; i++)
                {
                    finalFieldDestSpans[i] = fieldDestSpans[i].Value;
                }

                funct.AddInstruction(curSegment, Core.InstructionMoveLiteralStruct.Of(
                    exprLiteralStruct.GetSpan(), output,
                    typeStruct.structIndex, fieldRegAccesses, finalFieldDestSpans));
            }
        }


        private void ResolveExprName(Grammar.ASTNodeExprName exprName, ref int curSegment, Core.DataAccess output)
        {
            var name = NameResolver.Resolve(((Grammar.ASTNodeExprNameConcrete)exprName).name);

            // Try to find a local with the same name.
            var bindingIndex = FindLocalBinding(name);
            if (bindingIndex >= 0)
            {
                funct.AddInstruction(curSegment,
                    Core.InstructionMoveData.Of(
                        exprName.GetSpan(),
                        output,
                        Core.DataAccessRegister.ForRegister(
                            exprName.GetSpan(),
                            funct.localBindings[bindingIndex].registerIndex)));
                return;
            }

            // Try to find a group of functs with the same name.
            var functList = session.GetDeclsWithUseDirectives(name, false, useDirectives);
            if (session.ValidateSingleDecl(functList, name, exprName.GetSpan()) &&
                session.ValidateAsFunct(functList[0], name, exprName.GetSpan()))
            { 
                funct.AddInstruction(curSegment,
                    Core.InstructionMoveLiteralFunct.With(exprName.GetSpan(), output, functList[0].index));
                return;
            }

            this.foundErrors = true;
            session.AddMessage(
                Diagnostics.MessageKind.Error,
                Diagnostics.MessageCode.Unknown,
                "unknown '" + name.GetString() + "'",
                exprName.GetSpan());
            throw new Core.CheckException();
        }


        private int FindLocalBinding(Core.Name name)
        {
            for (int i = funct.localBindings.Count - 1; i >= 0; i--)
            {
                if (funct.localBindings[i].name.Compare(name) &&
                    localScopeLivenesses[i])
                    return i;
            }

            return -1;
        }


        private Core.DataAccess ResolveDataAccess(Grammar.ASTNodeExpr expr, ref int curSegment, bool allowInnerExpr, bool mutInnerExpr)
        {
            while (true)
            {
                var exprParenthesized = expr as Grammar.ASTNodeExprParenthesized;
                if (exprParenthesized != null)
                    expr = exprParenthesized.innerExpr;
                else
                    break;
            }

            var exprConcreteName = expr as Grammar.ASTNodeExprNameConcrete;
            if (exprConcreteName != null)
            {
                var name = NameResolver.Resolve(exprConcreteName.name);
                var bindingIndex = FindLocalBinding(name);

                if (bindingIndex >= 0)
                {
                    var localRegisterIndex = funct.localBindings[bindingIndex].registerIndex;
                    return Core.DataAccessRegister.ForRegister(expr.GetSpan(), localRegisterIndex);
                }
            }

            var exprDereference = expr as Grammar.ASTNodeExprUnaryOp;
            if (exprDereference != null && exprDereference.oper == Grammar.ASTNodeExprUnaryOp.Operator.At)
            {
                var innerAccess = ResolveDataAccess(exprDereference.operand, ref curSegment, true, false);
                return Core.DataAccessDereference.Of(exprDereference.GetSpan(), innerAccess);
            }

            var exprDotOp = expr as Grammar.ASTNodeExprBinaryOp;
            if (exprDotOp != null && exprDotOp.oper == Grammar.ASTNodeExprBinaryOp.Operator.Dot)
            {
                var baseAccess = ResolveDataAccess(exprDotOp.lhsOperand, ref curSegment, true, false);

                // Left-hand side expr type must be resolved for field access.
                FunctTypeInferencer.DoInference(this.session, this.funct);

                if (!TypeResolver.ValidateDataAccess(this.session, this.funct, baseAccess))
                {
                    this.foundErrors = true;
                    return null;
                }

                var lhsType = TypeResolver.GetDataAccessType(this.session, this.funct, baseAccess);

                var lhsStruct = lhsType as Core.TypeStruct;
                if (lhsStruct != null && lhsType.IsResolved())
                {
                    var rhsFieldName = exprDotOp.rhsOperand as Grammar.ASTNodeExprNameConcrete;
                    if (rhsFieldName == null)
                    {
                        this.foundErrors = true;
                        session.AddMessage(
                            Diagnostics.MessageKind.Error,
                            Diagnostics.MessageCode.Expected,
                            "expected field name",
                            exprDotOp.rhsOperand.GetSpan());
                        return null;
                    }

                    var name = NameResolver.Resolve(rhsFieldName.name);

                    int fieldIndex;
                    if (!this.session.GetStruct(lhsStruct.structIndex).fieldNames.FindByName(
                        name, out fieldIndex))
                    {
                        this.foundErrors = true;
                        session.AddMessage(
                            Diagnostics.MessageKind.Error,
                            Diagnostics.MessageCode.Unknown,
                            "unknown field '" + name.GetString() + "' in '" +
                            lhsType.GetString(this.session) + "'",
                            exprDotOp.rhsOperand.GetSpan(),
                            exprDotOp.lhsOperand.GetSpan());
                        return null;
                    }

                    return Core.DataAccessField.Of(exprDotOp.GetSpan(), baseAccess, fieldIndex);
                }

                var lhsTuple = lhsType as Core.TypeTuple;
                if (lhsTuple != null)
                {
                    var rhsFieldIndex = exprDotOp.rhsOperand as Grammar.ASTNodeExprLiteralInt;
                    if (rhsFieldIndex == null)
                    {
                        this.foundErrors = true;
                        session.AddMessage(
                            Diagnostics.MessageKind.Error,
                            Diagnostics.MessageCode.Expected,
                            "expected field index",
                            exprDotOp.rhsOperand.GetSpan());
                        return null;
                    }

                    var fieldIndex = System.Convert.ToInt32(rhsFieldIndex.value);

                    if (fieldIndex < 0 || fieldIndex >= lhsTuple.elementTypes.Length)
                    {
                        this.foundErrors = true;
                        session.AddMessage(
                            Diagnostics.MessageKind.Error,
                            Diagnostics.MessageCode.Unknown,
                            "invalid field index for '" +
                            lhsType.GetString(this.session) + "'",
                            exprDotOp.rhsOperand.GetSpan(),
                            exprDotOp.lhsOperand.GetSpan());
                        return null;
                    }

                    return Core.DataAccessField.Of(exprDotOp.GetSpan(), baseAccess, fieldIndex);
                }

                if (!lhsType.IsError() && !lhsType.IsResolved())
                {
                    this.foundErrors = true;
                    session.AddMessage(
                        Diagnostics.MessageKind.Error,
                        Diagnostics.MessageCode.EarlyInferenceFailed,
                        "type must be known but inference up to this point failed",
                        exprDotOp.lhsOperand.GetSpan());
                    return null;
                }

                this.foundErrors = true;
                session.AddMessage(
                    Diagnostics.MessageKind.Error,
                    Diagnostics.MessageCode.WrongFieldAccess,
                    "attempted field access on '" + lhsType.GetString(this.session) + "'",
                    exprDotOp.lhsOperand.GetSpan());
                return null;
            }

            if (allowInnerExpr)
            {
                // Generate a new register for inner expression.
                var registerIndex = this.funct.CreateRegister(new Core.TypePlaceholder(), mutInnerExpr);
                var access = Core.DataAccessRegister.ForRegister(expr.GetSpan(), registerIndex);
                ResolveExpr(expr, ref curSegment, access);

                // Create a new access for the same register, in case there's an outer
                // access modifier.
                access = Core.DataAccessRegister.ForRegister(expr.GetSpan(), registerIndex);
                return access;
            }

            this.foundErrors = true;
            session.AddMessage(
                Diagnostics.MessageKind.Error,
                Diagnostics.MessageCode.InvalidAccess,
                "expression has no address",
                expr.GetSpan());
            return null;
        }
    }
}
