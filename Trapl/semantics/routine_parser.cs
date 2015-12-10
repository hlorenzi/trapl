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
            var retSource = analyzer.ParseBlock(astNode, ref entrySegment);

            routine.AddInstruction(entrySegment,
                new InstructionCopy(
                    new StorageAccess(0, new Span()),
                    retSource));

            routine.AddInstruction(entrySegment, new InstructionEnd());
        }


        private Infrastructure.Session session;
        private Routine routine;


        private RoutineASTParser(Infrastructure.Session session, Routine routine)
        {
            this.session = session;
            this.routine = routine;
        }


        private SourceOperand ParseExpression(Grammar.ASTNode astNode, ref int entrySegment)
        {
            if (astNode.kind == Grammar.ASTNodeKind.Block)
                return this.ParseBlock(astNode, ref entrySegment);
            else if (astNode.kind == Grammar.ASTNodeKind.ControlIf)
                return this.ParseControlIf(astNode, ref entrySegment);
            else if (astNode.kind == Grammar.ASTNodeKind.ControlLet)
                return this.ParseControlLet(astNode, ref entrySegment);
            else if (astNode.kind == Grammar.ASTNodeKind.Call)
                return this.ParseCall(astNode, ref entrySegment);
            else if (astNode.kind == Grammar.ASTNodeKind.BinaryOp)
                return this.ParseBinaryOp(astNode, ref entrySegment);
            else if (astNode.kind == Grammar.ASTNodeKind.Name)
                return this.ParseName(astNode, ref entrySegment);
            else if (astNode.kind == Grammar.ASTNodeKind.NumberLiteral)
                return this.ParseNumberLiteral(astNode, ref entrySegment);

            throw new InternalException("not implemented");
        }


        private SourceOperand ParseBlock(Grammar.ASTNode astNode, ref int entrySegment)
        {
            for (int i = 0; i < astNode.ChildNumber(); i++)
            {
                try
                {
                    var exprOperand = this.ParseExpression(
                        astNode.Child(i), ref entrySegment);

                    if (i < astNode.ChildNumber() - 1)
                    {
                        this.routine.AddInstruction(entrySegment,
                            new InstructionExec(exprOperand));
                    }
                    else
                        return exprOperand;
                }
                catch (CheckException)
                {

                }
            }

            // This will be reached when the block has no subexpressions,
            // or when the last subexpression throws an exception.
            return new SourceOperandTupleLiteral(astNode.Span());
        }


        private SourceOperand ParseControlIf(Grammar.ASTNode astNode, ref int entrySegment)
        {
            var conditionSource = this.ParseExpression(astNode.Child(0), ref entrySegment);

            var instBranch = new InstructionBranch(conditionSource, -1, -1);
            this.routine.AddInstruction(entrySegment, instBranch);


            var trueSegment = this.routine.CreateSegment();
            instBranch.trueDestinationSegment = trueSegment;
                
            this.ParseBlock(astNode.Child(1), ref trueSegment);


            if (astNode.ChildNumber() == 3)
            {
                var falseSegment = this.routine.CreateSegment();
                instBranch.falseDestinationSegment = falseSegment;

                this.ParseBlock(astNode.Child(2), ref falseSegment);

                var afterSegment = this.routine.CreateSegment();
                this.routine.AddInstruction(trueSegment, new InstructionGoto(afterSegment));
                this.routine.AddInstruction(falseSegment, new InstructionGoto(afterSegment));
                entrySegment = afterSegment;
            }
            else
            {
                var afterSegment = this.routine.CreateSegment();
                this.routine.AddInstruction(trueSegment, new InstructionGoto(afterSegment));
                instBranch.falseDestinationSegment = afterSegment;
                entrySegment = afterSegment;
            }

            return new SourceOperandTupleLiteral(astNode.Span());
        }


        private SourceOperand ParseControlLet(Grammar.ASTNode astNode, ref int entrySegment)
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
                var initSource = this.ParseExpression(
                    astNode.Child(curChild),
                    ref entrySegment);

                this.routine.AddInstruction(entrySegment,
                    new InstructionCopy(
                        new StorageAccess(registerIndex, astNode.Child(0).Span()),
                        initSource));
            }

            return new SourceOperandTupleLiteral(astNode.Span());
        }

        private SourceOperand ParseCall(Grammar.ASTNode astNode, ref int entrySegment)
        {
            // Parse called expression.
            var calledSource = this.ParseExpression(
                astNode.Child(0),
                ref entrySegment);

            // Parse argument expressions.
            var argumentSources = new List<SourceOperand>();

            for (var i = 1; i < astNode.ChildNumber(); i++)
            {
                var argumentSource = this.ParseExpression(
                    astNode.Child(i),
                    ref entrySegment);

                argumentSources.Add(argumentSource);
            }

            // Generate call operand.
            var callSource = new SourceOperandCall(calledSource, astNode.Span());
            callSource.argumentSources = argumentSources;

            return callSource;
        }


        private SourceOperand ParseBinaryOp(Grammar.ASTNode astNode, ref int entrySegment)
        {
            if (astNode.Child(0).GetExcerpt() == "=")
            {
                if (astNode.ChildIs(1, Grammar.ASTNodeKind.Name))
                {
                    var bindingIndex = GetBindingIndex(astNode.Child(1), true);

                    var source = ParseExpression(astNode.Child(2),
                        ref entrySegment);

                    this.routine.AddInstruction(entrySegment,
                        new InstructionCopy(
                            new StorageAccess(
                                this.routine.bindings[bindingIndex].registerIndex,
                                astNode.Child(1).Span()),
                            source));

                    return new SourceOperandTupleLiteral(astNode.Span());
                }
            }

            throw new InternalException("not implemented");
        }


        private SourceOperand ParseNumberLiteral(Grammar.ASTNode astNode, ref int entrySegment)
        {
            return new SourceOperandNumberLiteral(astNode.GetExcerpt(), astNode.Span());
        }


        private SourceOperand ParseName(Grammar.ASTNode astNode, ref int entrySegment)
        {
            var bindingIndex = GetBindingIndex(astNode, false);
            if (bindingIndex >= 0)
            {
                return new SourceOperandRegister(
                    new StorageAccess(
                        this.routine.bindings[bindingIndex].registerIndex,
                        astNode.Span()),
                    astNode.Span());
            }

            var functList = session.functDecls.GetDeclsClone(astNode.Child(0));
            if (functList.Count > 0)
            {
                return new SourceOperandFunct(functList, astNode.Span());
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
