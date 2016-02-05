using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trapl.Grammar
{
    public class FunctBodyConverter
    {
        public FunctBodyConverter(Core.Session session, int functIndex, Core.UseDirective[] useDirectives)
        {
            this.session = session;
            this.functIndex = functIndex;
            this.useDirectives = useDirectives;
            this.typeInferencer = new TypeInferencer();
        }


        public void Convert(ASTNodeExpr topExpr)
        {
            var segment = session.CreateFunctSegment(functIndex);
            ConvertExpr(topExpr, ref segment, Core.DataAccessRegister.ForRegister(0));
        }


        private Core.Session session;
        private int functIndex;
        private Core.UseDirective[] useDirectives;
        private TypeInferencer typeInferencer;


        private void ConvertExpr(Grammar.ASTNodeExpr expr, ref int curSegment, Core.DataAccess outputReg)
        {
            if (expr is ASTNodeExprBlock)
                this.ConvertExprBlock((ASTNodeExprBlock)expr, ref curSegment, outputReg);
            /*else if (expr is ASTNodeExprIf)
                this.ConvertExprIf((ASTNodeExprIf)expr, ref curSegment, outputReg);
            else if (expr is ASTNodeExprLet)
                this.ConvertExprLet((ASTNodeExprLet)expr, ref curSegment, outputReg);
            else if (expr is ASTNodeExprName)
                this.ConvertExprName((ASTNodeExprName)expr, ref curSegment, outputReg);*/
            else if (expr is ASTNodeExprLiteralInt)
                this.ConvertExprLiteralInt((ASTNodeExprLiteralInt)expr, ref curSegment, outputReg);
            else
                throw new System.NotImplementedException();
        }


        private void ConvertExprBlock(ASTNodeExprBlock exprBlock, ref int curSegment, Core.DataAccess outputReg)
        {
            // Generate an empty tuple store if there are no subexpressions.
            if (exprBlock.subexprs.Count == 0)
            {
                session.AddFunctInstruction(functIndex, curSegment,
                    Core.InstructionMoveLiteralTuple.Empty(exprBlock.GetSpan(), outputReg));
                return;
            }

            for (int i = 0; i < exprBlock.subexprs.Count; i++)
            {
                try
                {
                    var subexprOutput = outputReg;
                    if (i < exprBlock.subexprs.Count - 1)
                        subexprOutput = Core.DataAccessRegister.ForRegister(
                            session.CreateFunctRegister(functIndex,
                                Core.TypePlaceholder.Of(typeInferencer.AddSlot())));

                    this.ConvertExpr(exprBlock.subexprs[i], ref curSegment, subexprOutput);
                }
                catch (Core.CheckException) { }
            }
        }


        /*private void ConvertExprIf(ASTNodeExprIf exprIf, ref int curSegment, Core.DataAccess output)
        {
            // Parse condition.
            var conditionReg = Core.DataAccessRegister.ForRegister(
                session.CreateFunctRegister(functIndex,
                    Core.TypePlaceholder.Of(typeInferencer.AddSlot())));

            this.ConvertExpr(
                exprIf.conditionExpr,
                ref curSegment,
                conditionReg);

            var instBranch = new InstructionBranch(conditionReg);
            this.routine.AddInstruction(entrySegment, instBranch);

            // Parse true branch.
            var trueSegment = this.routine.CreateSegment();
            instBranch.trueDestinationSegment = trueSegment;

            this.ParseBlock(astNode.Child(1), ref trueSegment, output);

            // Parse false branch, if there is one.
            if (astNode.ChildNumber() == 3)
            {
                var falseSegment = this.routine.CreateSegment();
                instBranch.falseDestinationSegment = falseSegment;

                this.ParseBlock(astNode.Child(2), ref falseSegment, output);

                var afterSegment = this.routine.CreateSegment();
                this.routine.AddInstruction(trueSegment, new InstructionGoto(afterSegment));
                this.routine.AddInstruction(falseSegment, new InstructionGoto(afterSegment));
                entrySegment = afterSegment;
            }
            // Or else, just route the false segment path to the next segment.
            else
            {
                var afterSegment = this.routine.CreateSegment();
                this.routine.AddInstruction(trueSegment, new InstructionGoto(afterSegment));
                instBranch.falseDestinationSegment = afterSegment;
                entrySegment = afterSegment;
            }
        }*/


        private void ConvertExprLiteralInt(ASTNodeExprLiteralInt exprLiteralInt, ref int curSegment, Core.DataAccess output)
        {
            session.AddFunctInstruction(functIndex, curSegment,
                Core.InstructionMoveLiteralInt.Of(exprLiteralInt.GetSpan(), output, System.Convert.ToInt64(exprLiteralInt.GetExcerpt())));
        }
    }
}
