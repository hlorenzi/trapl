using System.Collections.Generic;
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
            analyzer.ParseBlock(astNode, ref entrySegment, new StorageAccess(0, new Span()));

            routine.AddInstruction(entrySegment, new InstructionEnd());
        }


        private Infrastructure.Session session;
        private Routine routine;


        private RoutineASTParser(Infrastructure.Session session, Routine routine)
        {
            this.session = session;
            this.routine = routine;
        }


        private void ParseExpression(Grammar.ASTNode astNode, ref int entrySegment, StorageAccess output)
        {
            if (astNode.kind == Grammar.ASTNodeKind.Block)
                this.ParseBlock(astNode, ref entrySegment, output);
            else if (astNode.kind == Grammar.ASTNodeKind.ControlIf)
                this.ParseControlIf(astNode, ref entrySegment, output);
            else if (astNode.kind == Grammar.ASTNodeKind.ControlLet)
                this.ParseControlLet(astNode, ref entrySegment, output);
            else if (astNode.kind == Grammar.ASTNodeKind.Call)
                this.ParseCall(astNode, ref entrySegment, output);
            else if (astNode.kind == Grammar.ASTNodeKind.BinaryOp)
                this.ParseBinaryOp(astNode, ref entrySegment, output);
            else if (astNode.kind == Grammar.ASTNodeKind.Name)
                this.ParseName(astNode, ref entrySegment, output);
            else if (astNode.kind == Grammar.ASTNodeKind.NumberLiteral)
                this.ParseNumberLiteral(astNode, ref entrySegment, output);
            else
                throw new InternalException("not implemented");
        }


        private void ParseBlock(Grammar.ASTNode astNode, ref int entrySegment, StorageAccess output)
        {
            // Generate a dummy store if there are no subexpressions.
            if (astNode.ChildNumber() == 0)
            {
                this.routine.AddInstruction(entrySegment, new InstructionCopyFromTupleLiteral(output));
                return;
            }

            for (int i = 0; i < astNode.ChildNumber(); i++)
            {
                try
                {
                    var subexprOutput = output;
                    if (i < astNode.ChildNumber() - 1)
                        subexprOutput = new StorageAccess(this.routine.CreateRegister(new TypePlaceholder()), astNode.Span());

                    this.ParseExpression(astNode.Child(i), ref entrySegment, subexprOutput);
                }
                catch (CheckException) { }
            }
        }


        private void ParseControlIf(Grammar.ASTNode astNode, ref int entrySegment, StorageAccess output)
        {
            // Parse condition.
            var conditionReg = new StorageAccess(
                this.routine.CreateRegister(new TypeStruct(this.session.primitiveBool)),
                astNode.Child(0).Span());

            this.ParseExpression(
                astNode.Child(0),
                ref entrySegment,
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
        }


        private void ParseControlLet(Grammar.ASTNode astNode, ref int entrySegment, StorageAccess output)
        {
            // Create a new storage location and name binding.
            var registerIndex = this.routine.CreateRegister(new TypePlaceholder());
            var register = this.routine.registers[registerIndex];

            var bindingIndex = this.routine.CreateBinding(registerIndex);
            var binding = this.routine.bindings[bindingIndex];

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

            // Parse init expression, if there is one.
            if (astNode.ChildNumber() > curChild)
            {
                this.ParseExpression(
                    astNode.Child(curChild),
                    ref entrySegment,
                    new StorageAccess(registerIndex, astNode.Child(0).Span()));
            }
        }

        private void ParseCall(Grammar.ASTNode astNode, ref int entrySegment, StorageAccess output)
        {
            // Parse called expression.
            var calledReg = new StorageAccess(
                this.routine.CreateRegister(new TypeFunct(
                    this.routine.registers[output.registerIndex].type,
                    astNode.ChildNumber() - 1)),
                astNode.Child(0).Span());

            this.ParseExpression(
                astNode.Child(0),
                ref entrySegment,
                calledReg);

            // Parse argument expressions.
            var argumentRegs = new List<StorageAccess>();

            for (var i = 1; i < astNode.ChildNumber(); i++)
            {
                var argumentReg = new StorageAccess(
                    this.routine.CreateRegister(new TypePlaceholder()),
                    astNode.Child(i).Span());

                this.ParseExpression(
                    astNode.Child(i),
                    ref entrySegment,
                    argumentReg);

                argumentRegs.Add(argumentReg);
            }

            // Generate call instruction.
            this.routine.AddInstruction(entrySegment,
                new InstructionCopyFromCall(output, calledReg, argumentRegs));
        }


        private void ParseBinaryOp(Grammar.ASTNode astNode, ref int entrySegment, StorageAccess output)
        {
            if (astNode.Child(0).GetExcerpt() == "=")
            {
                if (astNode.ChildIs(1, Grammar.ASTNodeKind.Name))
                {
                    var bindingIndex = GetBindingIndex(astNode.Child(1), true);
                    var registerIndex = this.routine.bindings[bindingIndex].registerIndex;

                    this.ParseExpression(
                        astNode.Child(2),
                        ref entrySegment,
                        new StorageAccess(registerIndex, astNode.Child(1).Span()));
                }
            }

            throw new InternalException("not implemented");
        }


        private void ParseNumberLiteral(Grammar.ASTNode astNode, ref int entrySegment, StorageAccess output)
        {
            this.routine.AddInstruction(entrySegment,
                new InstructionCopyFromNumberLiteral(output, astNode.GetExcerpt()));
        }


        private void ParseName(Grammar.ASTNode astNode, ref int entrySegment, StorageAccess output)
        {
            var bindingIndex = GetBindingIndex(astNode, false);
            if (bindingIndex >= 0)
            {
                this.routine.AddInstruction(entrySegment,
                    new InstructionCopyFromStorage(output,
                    new StorageAccess(
                        this.routine.bindings[bindingIndex].registerIndex,
                        astNode.Span())));
                return;
            }

            var functList = session.functDecls.GetDeclsClone(astNode.Child(0));
            if (functList.Count > 0)
            {
                this.routine.AddInstruction(entrySegment,
                    new InstructionCopyFromFunct(output, functList));
                return;
            }

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
