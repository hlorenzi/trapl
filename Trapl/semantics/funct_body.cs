﻿namespace Trapl.Semantics
{
    public class FunctBodyConverter
    {
        public FunctBodyConverter(Core.Session session, Core.DeclFunct funct, Core.UseDirective[] useDirectives)
        {
            this.session = session;
            this.funct = funct;
            this.useDirectives = useDirectives;
            this.typeInferencer = new TypeInferencer();
        }


        public void Convert(Grammar.ASTNodeExpr topExpr)
        {
            var segment = funct.CreateSegment();
            ConvertExpr(topExpr, ref segment, Core.DataAccessRegister.ForRegister(0));
        }


        private Core.Session session;
        private Core.DeclFunct funct;
        private Core.UseDirective[] useDirectives;
        private TypeInferencer typeInferencer;


        private void ConvertExpr(Grammar.ASTNodeExpr expr, ref int curSegment, Core.DataAccess outputReg)
        {
            if (expr is Grammar.ASTNodeExprBlock)
                this.ConvertExprBlock((Grammar.ASTNodeExprBlock)expr, ref curSegment, outputReg);
            else if (expr is Grammar.ASTNodeExprIf)
                this.ConvertExprIf((Grammar.ASTNodeExprIf)expr, ref curSegment, outputReg);
            else if (expr is Grammar.ASTNodeExprLet)
                this.ConvertExprLet((Grammar.ASTNodeExprLet)expr, ref curSegment, outputReg);
            else if (expr is Grammar.ASTNodeExprCall)
                this.ConvertExprCall((Grammar.ASTNodeExprCall)expr, ref curSegment, outputReg);
            else if (expr is Grammar.ASTNodeExprName)
                this.ConvertExprName((Grammar.ASTNodeExprName)expr, ref curSegment, outputReg);
            else if (expr is Grammar.ASTNodeExprLiteralInt)
                this.ConvertExprLiteralInt((Grammar.ASTNodeExprLiteralInt)expr, ref curSegment, outputReg);
            else
                throw new System.NotImplementedException();
        }


        private void ConvertExprBlock(Grammar.ASTNodeExprBlock exprBlock, ref int curSegment, Core.DataAccess outputReg)
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

                    this.ConvertExpr(exprBlock.subexprs[i], ref curSegment, subexprOutput);
                }
                catch (Core.CheckException) { }
            }
        }


        private void ConvertExprIf(Grammar.ASTNodeExprIf exprIf, ref int curSegment, Core.DataAccess output)
        {
            // Parse condition.
            var conditionReg = Core.DataAccessRegister.ForRegister(
                funct.CreateRegister(
                    Core.TypePlaceholder.Of(typeInferencer.AddSlot())));

            this.ConvertExpr(
                exprIf.conditionExpr,
                ref curSegment,
                conditionReg);

            var instBranch = new Core.InstructionBranch { conditionReg = conditionReg };
            funct.AddInstruction(curSegment, instBranch);

            // Parse true branch.
            var trueSegment = funct.CreateSegment();
            instBranch.destinationSegmentIfTaken = trueSegment;

            ConvertExpr(exprIf.trueBranchExpr, ref trueSegment, output);

            // Parse false branch, if there is one.
            if (exprIf.falseBranchExpr != null)
            {
                var falseSegment = funct.CreateSegment();
                instBranch.destinationSegmentIfNotTaken = falseSegment;

                ConvertExpr(exprIf.falseBranchExpr, ref falseSegment, output);

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


        private void ConvertExprLet(Grammar.ASTNodeExprLet exprLet, ref int curSegment, Core.DataAccess output)
        {
            // Create a new storage location and name binding.
            var inferSlot = typeInferencer.AddSlot();
            var registerIndex = funct.CreateRegister(Core.TypePlaceholder.Of(inferSlot));

            funct.CreateBinding(
                NameResolver.Resolve(((Grammar.ASTNodeExprNameConcrete)exprLet.name).name),
                registerIndex);

            // Parse type annotation, if there is one.
            if (exprLet.type != null)
            {
                typeInferencer.InferType(inferSlot,
                    TypeResolver.Resolve(session, exprLet.type, useDirectives));
            }

            // Parse init expression, if there is one.
            if (exprLet.initExpr != null)
            {
                ConvertExpr(exprLet.initExpr, ref curSegment,
                    Core.DataAccessRegister.ForRegister(registerIndex));
            }

            // Generate a void store.
            funct.AddInstruction(curSegment,
                Core.InstructionMoveLiteralTuple.Empty(exprLet.GetSpan(), output));
        }


        private void ConvertExprCall(Grammar.ASTNodeExprCall exprCall, ref int curSegment, Core.DataAccess output)
        {
            // Parse called expression.
            var callTargetReg = Core.DataAccessRegister.ForRegister(
                funct.CreateRegister(Core.TypePlaceholder.Of(typeInferencer.AddSlot())));

            ConvertExpr(exprCall.calledExpr, ref curSegment, callTargetReg);

            // Parse argument expressions.
            var argumentRegs = new Core.DataAccess[exprCall.argumentExprs.Count];

            for (var i = 0; i < exprCall.argumentExprs.Count; i++)
            {
                argumentRegs[i] = Core.DataAccessRegister.ForRegister(
                    funct.CreateRegister(Core.TypePlaceholder.Of(typeInferencer.AddSlot())));

                ConvertExpr(exprCall.argumentExprs[i], ref curSegment, argumentRegs[i]);
            }

            // Generate call instruction.
            funct.AddInstruction(curSegment,
                Core.InstructionMoveCallResult.For(exprCall.GetSpan(), output, callTargetReg, argumentRegs));
        }


        private void ConvertExprLiteralInt(Grammar.ASTNodeExprLiteralInt exprLiteralInt, ref int curSegment, Core.DataAccess output)
        {
            funct.AddInstruction(curSegment,
                Core.InstructionMoveLiteralInt.Of(exprLiteralInt.GetSpan(), output, System.Convert.ToInt64(exprLiteralInt.GetExcerpt())));
        }


        private void ConvertExprName(Grammar.ASTNodeExprName exprName, ref int curSegment, Core.DataAccess output)
        {
            var name = NameResolver.Resolve(((Grammar.ASTNodeExprNameConcrete)exprName).name);

            // Try to find a local with the same name.
            var bindingIndex = GetBindingIndex(name, false);
            if (bindingIndex >= 0)
            {
                funct.AddInstruction(curSegment,
                    Core.InstructionMoveData.Of(
                        exprName.GetSpan(),
                        output,
                        Core.DataAccessRegister.ForRegister(
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
                Diagnostics.MessageCode.Undeclared,
                "undeclared '" + name.GetString() + "'",
                exprName.GetSpan());
            throw new Core.CheckException();
        }


        private int GetBindingIndex(Core.Name name, bool throwErrorOnUndeclared)
        {
            for (int i = funct.localBindings.Count - 1; i >= 0; i--)
            {
                if (funct.localBindings[i].name.Compare(name))
                {
                    return i;
                }
            }

            if (throwErrorOnUndeclared)
            {
                /*session.AddMessage(
                    Diagnostics.MessageKind.Error, 
                    Diagnostics.MessageCode.Undeclared,
                    "undeclared '" + name.GetString() + "'",
                    name.GetSpan());*/
                throw new Core.CheckException();
            }

            return -1;
        }
    }
}
