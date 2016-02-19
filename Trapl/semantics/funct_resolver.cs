﻿using System.Collections.Generic;


namespace Trapl.Semantics
{
    public class FunctBodyResolver
    {
        public FunctBodyResolver(Core.Session session, Core.DeclFunct funct, Core.UseDirective[] useDirectives)
        {
            this.session = session;
            this.funct = funct;
            this.useDirectives = useDirectives;
        }


        public void Resolve(Grammar.ASTNodeExpr topExpr)
        {
            var curSegment = funct.CreateSegment();
            ResolveExpr(topExpr, ref curSegment, Core.DataAccessRegister.ForRegister(topExpr.GetSpan(), 0));
            funct.AddInstruction(curSegment, new Core.InstructionEnd());
            FunctInferencer.DoInference(this.session, this.funct);
            FunctChecker.Check(this.session, this.funct);
        }


        private Core.Session session;
        private Core.DeclFunct funct;
        private Core.UseDirective[] useDirectives;


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
            else if (expr is Grammar.ASTNodeExprCall)
                this.ResolveExprCall((Grammar.ASTNodeExprCall)expr, ref curSegment, outputReg);
            else if (expr is Grammar.ASTNodeExprBinaryOp)
                this.ResolveExprBinaryOp((Grammar.ASTNodeExprBinaryOp)expr, ref curSegment, outputReg);
            else if (expr is Grammar.ASTNodeExprName)
                this.ResolveExprName((Grammar.ASTNodeExprName)expr, ref curSegment, outputReg);
            else if (expr is Grammar.ASTNodeExprLiteralInt)
                this.ResolveExprLiteralInt((Grammar.ASTNodeExprLiteralInt)expr, ref curSegment, outputReg);
            else
                throw new System.NotImplementedException();
        }


        private void ResolveExprBlock(Grammar.ASTNodeExprBlock exprBlock, ref int curSegment, Core.DataAccess outputReg)
        {
            // Generate an empty tuple store if there are no subexpressions.
            if (exprBlock.subexprs.Count == 0)
            {
                funct.AddInstruction(curSegment,
                    Core.InstructionMoveLiteralTuple.Empty(exprBlock.GetSpan(), outputReg));
                return;
            }

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
        }


        private void ResolveExprIf(Grammar.ASTNodeExprIf exprIf, ref int curSegment, Core.DataAccess output)
        {
            // Parse condition.
            var conditionReg = Core.DataAccessRegister.ForRegister(
                exprIf.conditionExpr.GetSpan(),
                funct.CreateRegister(new Core.TypePlaceholder()));

            this.ResolveExpr(
                exprIf.conditionExpr,
                ref curSegment,
                conditionReg);

            var instBranch = new Core.InstructionBranch { conditionReg = conditionReg };
            funct.AddInstruction(curSegment, instBranch);

            // Parse true branch.
            var trueSegment = funct.CreateSegment();
            instBranch.destinationSegmentIfTaken = trueSegment;

            ResolveExpr(exprIf.trueBranchExpr, ref trueSegment, output);

            // Parse false branch, if there is one.
            if (exprIf.falseBranchExpr != null)
            {
                var falseSegment = funct.CreateSegment();
                instBranch.destinationSegmentIfNotTaken = falseSegment;

                ResolveExpr(exprIf.falseBranchExpr, ref falseSegment, output);

                var afterSegment = funct.CreateSegment();
                funct.AddInstruction(trueSegment, Core.InstructionGoto.To(afterSegment));
                funct.AddInstruction(falseSegment, Core.InstructionGoto.To(afterSegment));
                curSegment = afterSegment;
            }
            // Or else, just route the false segment path to the next segment.
            else
            {
                var afterSegment = funct.CreateSegment();
                funct.AddInstruction(trueSegment, Core.InstructionGoto.To(afterSegment));
                instBranch.destinationSegmentIfNotTaken = afterSegment;
                curSegment = afterSegment;
            }
        }


        private void ResolveExprLet(Grammar.ASTNodeExprLet exprLet, ref int curSegment, Core.DataAccess output)
        {
            // Create a new storage location and name binding.
            var registerIndex = funct.CreateRegister(new Core.TypePlaceholder());

            funct.CreateBinding(
                NameResolver.Resolve(((Grammar.ASTNodeExprNameConcrete)exprLet.name).name),
                registerIndex,
                exprLet.name.GetSpan());

            // Parse type annotation, if there is one.
            if (exprLet.type != null)
            {
                funct.registerTypes[registerIndex] =
                    TypeResolver.Resolve(session, exprLet.type, useDirectives);
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


        private void ResolveExprCall(Grammar.ASTNodeExprCall exprCall, ref int curSegment, Core.DataAccess output)
        {
            // Parse called expression.
            var callTargetReg = Core.DataAccessRegister.ForRegister(
                exprCall.calledExpr.GetSpan(),
                funct.CreateRegister(new Core.TypePlaceholder()));

            ResolveExpr(exprCall.calledExpr, ref curSegment, callTargetReg);

            // Parse argument expressions.
            var argumentRegs = new Core.DataAccess[exprCall.argumentExprs.Count];

            for (var i = 0; i < exprCall.argumentExprs.Count; i++)
            {
                argumentRegs[i] = Core.DataAccessRegister.ForRegister(
                    exprCall.argumentExprs[i].GetSpan(),
                    funct.CreateRegister(new Core.TypePlaceholder()));

                ResolveExpr(exprCall.argumentExprs[i], ref curSegment, argumentRegs[i]);
            }

            // Generate call instruction.
            funct.AddInstruction(curSegment,
                Core.InstructionMoveCallResult.For(exprCall.GetSpan(), output, callTargetReg, argumentRegs));
        }


        private void ResolveExprBinaryOp(Grammar.ASTNodeExprBinaryOp exprBinOp, ref int curSegment, Core.DataAccess output)
        {
            if (exprBinOp.oper == Grammar.ASTNodeExprBinaryOp.Operator.Equal)
            {
                var access = ResolveDataAccess(exprBinOp.lhsOperand, ref curSegment, false);
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
                var access = ResolveDataAccess(exprBinOp, ref curSegment, false);
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


        private void ResolveExprLiteralInt(Grammar.ASTNodeExprLiteralInt exprLiteralInt, ref int curSegment, Core.DataAccess output)
        {
            funct.AddInstruction(curSegment,
                Core.InstructionMoveLiteralInt.Of(exprLiteralInt.GetSpan(), output, System.Convert.ToInt64(exprLiteralInt.GetExcerpt())));
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
                if (funct.localBindings[i].name.Compare(name))
                    return i;
            }

            return -1;
        }


        private Core.DataAccess ResolveDataAccess(Grammar.ASTNodeExpr expr, ref int curSegment, bool allowInnerExpr)
        {
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

            var exprDotOp = expr as Grammar.ASTNodeExprBinaryOp;
            if (exprDotOp != null && exprDotOp.oper == Grammar.ASTNodeExprBinaryOp.Operator.Dot)
            {
                var rhsFieldName = exprDotOp.rhsOperand as Grammar.ASTNodeExprNameConcrete;
                if (rhsFieldName == null)
                {
                    session.AddMessage(
                        Diagnostics.MessageKind.Error,
                        Diagnostics.MessageCode.Expected,
                        "expected field name",
                        exprDotOp.rhsOperand.GetSpan());
                    return null;
                }

                var innerAccess = ResolveDataAccess(exprDotOp.lhsOperand, ref curSegment, true);
                var innerRegAccess = innerAccess as Core.DataAccessRegister;
                if (innerRegAccess == null)
                    return null;

                // Left-hand side expr type must be resolved for field access.
                FunctInferencer.DoInference(this.session, this.funct);

                var lhsType = TypeResolver.GetDataAccessType(this.session, this.funct, innerAccess);
                if (!lhsType.IsResolved() || lhsType.IsError())
                {
                    session.AddMessage(
                        Diagnostics.MessageKind.Error,
                        Diagnostics.MessageCode.EarlyInferenceFailed,
                        "type must be known but inference up to this point failed",
                        exprDotOp.lhsOperand.GetSpan());
                    return null;
                }

                var lhsStruct = lhsType as Core.TypeStruct;
                if (lhsStruct == null)
                {
                    session.AddMessage(
                        Diagnostics.MessageKind.Error,
                        Diagnostics.MessageCode.WrongFieldAccess,
                        "field access on '" + lhsType.GetString(this.session) + "'",
                        exprDotOp.lhsOperand.GetSpan());
                    return null;
                }

                var name = NameResolver.Resolve(rhsFieldName.name);

                int fieldIndex;
                if (!this.session.GetStruct(lhsStruct.structIndex).fieldNames.FindByName(
                    name, out fieldIndex))
                {
                    session.AddMessage(
                        Diagnostics.MessageKind.Error,
                        Diagnostics.MessageCode.Unknown,
                        "unknown field '" + name.GetString() + "' in '" +
                        lhsType.GetString(this.session) + "'",
                        exprDotOp.rhsOperand.GetSpan(),
                        exprDotOp.lhsOperand.GetSpan());
                    return null;
                }

                innerRegAccess.AddFieldAccess(fieldIndex);
                innerRegAccess.span = innerRegAccess.span.Merge(exprDotOp.rhsOperand.GetSpan());
                return innerRegAccess;
            }

            if (allowInnerExpr)
            {
                // Generate a new register for inner expression.
                var registerIndex = this.funct.CreateRegister(new Core.TypePlaceholder());
                var access = Core.DataAccessRegister.ForRegister(expr.GetSpan(), registerIndex);
                ResolveExpr(expr, ref curSegment, access);

                // Create a new access for the same register, in case there's an outer
                // access modifier (dot operator).
                access = Core.DataAccessRegister.ForRegister(expr.GetSpan(), registerIndex);
                return access;
            }

            session.AddMessage(
                Diagnostics.MessageKind.Error,
                Diagnostics.MessageCode.InvalidAccess,
                "invalid assignment destination",
                expr.GetSpan());
            return null;
        }
    }
}