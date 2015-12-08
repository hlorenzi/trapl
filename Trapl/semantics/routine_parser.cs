using Trapl.Diagnostics;
using Trapl.Infrastructure;


namespace Trapl.Semantics
{
    public class RoutineASTParser
    {
        public static void Parse(Infrastructure.Session session, Routine routine, Grammar.ASTNode astNode)
        {
            var analyzer = new RoutineASTParser(session, routine);

            var entrySegment = analyzer.routine.CreateSegment();
            entrySegment = analyzer.ParseBlock(astNode, new StorageAccess(0), entrySegment);
            routine.AddInstruction(entrySegment, new InstructionEnd());
        }


        private Infrastructure.Session session;
        private Routine routine;


        private RoutineASTParser(Infrastructure.Session session, Routine routine)
        {
            this.session = session;
            this.routine = routine;
        }


        private int ParseExpression(Grammar.ASTNode astNode, StorageAccess outputReg, int entrySegment)
        {
            if (astNode.kind == Grammar.ASTNodeKind.Block)
                return this.ParseBlock(astNode, outputReg, entrySegment);
            else if (astNode.kind == Grammar.ASTNodeKind.ControlIf)
                return this.ParseControlIf(astNode, outputReg, entrySegment);
            else if (astNode.kind == Grammar.ASTNodeKind.ControlLet)
                return this.ParseControlLet(astNode, outputReg, entrySegment);
            else if (astNode.kind == Grammar.ASTNodeKind.NumberLiteral)
                return this.ParseNumberLiteral(astNode, outputReg, entrySegment);
            else if (astNode.kind == Grammar.ASTNodeKind.BinaryOp)
                return this.ParseBinaryOp(astNode, outputReg, entrySegment);
            else if (astNode.kind == Grammar.ASTNodeKind.Name)
                return this.ParseName(astNode, outputReg, entrySegment);

            throw new InternalException("not implemented");
        }


        private int ParseBlock(Grammar.ASTNode astNode, StorageAccess outputReg, int entrySegment)
        {
            for (int i = 0; i < astNode.ChildNumber(); i++)
            {
                try
                {
                    var exprOutputReg = outputReg;

                    if (i < astNode.ChildNumber() - 1)
                        exprOutputReg = new StorageAccess(this.routine.CreateRegister(new TypePlaceholder()));

                    entrySegment = this.ParseExpression(
                        astNode.Child(i), exprOutputReg, entrySegment);
                }
                catch (CheckException)
                {

                }
            }

            return entrySegment;
        }


        private int ParseControlIf(Grammar.ASTNode astNode, StorageAccess outputReg, int entrySegment)
        {
            var conditionRegIndex = this.routine.CreateRegister(new TypeStruct(this.session.primitiveBool));

            entrySegment = this.ParseExpression(
                astNode.Child(0),
                new StorageAccess(conditionRegIndex),
                entrySegment);

            var instBranch = new InstructionBranch(conditionRegIndex, -1, -1);
            this.routine.AddInstruction(entrySegment, instBranch);


            var trueSegment = this.routine.CreateSegment();
            instBranch.trueDestinationSegment = trueSegment;
                
            trueSegment = this.ParseBlock(
                astNode.Child(1),
                outputReg,
                trueSegment);


            if (astNode.ChildNumber() == 3)
            {
                var falseSegment = this.routine.CreateSegment();
                instBranch.falseDestinationSegment = falseSegment;

                falseSegment = this.ParseBlock(
                    astNode.Child(2),
                    outputReg,
                    falseSegment);

                var afterSegment = this.routine.CreateSegment();
                this.routine.AddInstruction(trueSegment, new InstructionGoto(afterSegment));
                this.routine.AddInstruction(falseSegment, new InstructionGoto(afterSegment));
                return afterSegment;
            }
            else
            {
                var afterSegment = this.routine.CreateSegment();
                this.routine.AddInstruction(trueSegment, new InstructionGoto(afterSegment));
                instBranch.falseDestinationSegment = afterSegment;
                return afterSegment;
            }
        }


        private int ParseControlLet(Grammar.ASTNode astNode, StorageAccess outputReg, int entrySegment)
        {
            var registerIndex = this.routine.CreateRegister(new TypePlaceholder());
            var register = this.routine.registers[registerIndex];

            var bindingIndex = this.routine.CreateBinding(registerIndex);
            var binding = this.routine.bindings[bindingIndex];

            binding.declSpan = astNode.Child(0).Span();

            binding.name = new Name(
                astNode.Child(0).Span(),
                astNode.Child(0).Child(0),
                TemplateUtil.ResolveFromNameAST(this.session, astNode.Child(0), true));

            var curChild = 1;

            // Parse type annotation, if there is one.
            if (astNode.ChildNumber() > curChild &&
                TypeUtil.IsTypeNode(astNode.Child(curChild).kind))
            {
                register.type = TypeUtil.ResolveFromAST(this.session, astNode.Child(curChild), false);
                curChild++;
            }

            this.routine.AddInstruction(entrySegment,
                new InstructionCopy(
                    outputReg,
                    new SourceOperandTupleLiteral()));

            return entrySegment;
        }


        private int ParseBinaryOp(Grammar.ASTNode astNode, StorageAccess outputReg, int entrySegment)
        {
            if (astNode.Child(0).GetExcerpt() == "=")
            {
                if (astNode.ChildIs(1, Grammar.ASTNodeKind.Name))
                {
                    var bindingIndex = GetBindingIndex(astNode.Child(1), true);

                    entrySegment = ParseExpression(astNode.Child(2),
                        new StorageAccess(this.routine.bindings[bindingIndex].registerIndex),
                        entrySegment);

                    this.routine.AddInstruction(entrySegment,
                        new InstructionCopy(
                            outputReg,
                            new SourceOperandTupleLiteral()));

                    return entrySegment;
                }
            }

            throw new InternalException("not implemented");
        }


        private int ParseNumberLiteral(Grammar.ASTNode astNode, StorageAccess outputReg, int entrySegment)
        {
            this.routine.AddInstruction(entrySegment,
                new InstructionCopy(
                    outputReg,
                    new SourceOperandNumberLiteral(astNode.GetExcerpt())));

            return entrySegment;
        }


        private int ParseName(Grammar.ASTNode astNode, StorageAccess outputReg, int entrySegment)
        {
            var bindingIndex = GetBindingIndex(astNode, false);
            if (bindingIndex >= 0)
            {
                this.routine.AddInstruction(entrySegment,
                    new InstructionCopy(
                        outputReg,
                        new SourceOperandRegister(
                            new StorageAccess(this.routine.bindings[bindingIndex].registerIndex))));

                return entrySegment;
            }

            /*var functList = session.functDecls.GetDeclsClone(astNode.Child(0));
            if (functList.Count > 0)
            {
                var code = new CodeNodeFunct();
                code.span = astNode.Span();
                code.nameInference.pathASTNode = astNode.Child(0);
                code.nameInference.template = templ;
                code.potentialFuncts = functList;
                code.outputType = new TypePlaceholder();

                return code;
            }*/

            session.diagn.Add(MessageKind.Error, MessageCode.UndeclaredIdentifier,
                "'" + PathUtil.GetDisplayString(astNode.Child(0)) + "' is not declared",
                astNode.Span());
            throw new CheckException();
        }


        private int GetBindingIndex(Grammar.ASTNode astNode, bool throwErrorOnUndeclared)
        {
            var templ = TemplateUtil.ResolveFromNameAST(this.session, astNode, false);

            for (int i = this.routine.bindings.Count - 1; i >= 0; i--)
            {
                if (!this.routine.bindings[i].outOfScope &&
                    this.routine.bindings[i].name.Compare(astNode.Child(0), templ))
                {
                    return i;
                }
            }

            if (throwErrorOnUndeclared)
            {
                session.diagn.Add(MessageKind.Error, MessageCode.UndeclaredIdentifier,
                    "'" + PathUtil.GetDisplayString(astNode.Child(0)) + "' is not declared",
                    astNode.Span());
                throw new CheckException();
            }

            return -1;
        }
    }
}
