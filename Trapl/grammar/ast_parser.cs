using System;
using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Grammar
{
    public class ASTParser
    {
        private int readhead;
        private TokenCollection tokenColl;
        private Core.Session session;
        private Stack<bool> insideCondition;


        public ASTParser(Core.Session session, TokenCollection tokenColl, int startToken = 0)
        {
            this.readhead = startToken;
            this.tokenColl = tokenColl;
            this.session = session;
            this.insideCondition = new Stack<bool>();
            this.insideCondition.Push(false);
        }


        public static ASTNodeDeclGroup Parse(Core.Session session, TokenCollection tokenColl)
        {
            try
            {
                var astParser = new ASTParser(session, tokenColl);
                return astParser.ParseDeclGroup();
            }
            catch (Core.CheckException)
            {
                return null;
            }
        }


        #region Helper Methods


        private Token Current()
        {
            return this.tokenColl[this.readhead];
        }


        private bool CurrentIs(TokenKind kind)
        {
            return (this.Current().kind == kind);
        }


        private bool CurrentIsNot(params TokenKind[] kinds)
        {
            for (int i = 0; i < kinds.Length; i++)
                if (this.Current().kind == kinds[i])
                    return false;
            return (this.Current().kind != TokenKind.Error);
        }


        private Token Next()
        {
            return this.tokenColl[this.readhead + 1];
        }


        private bool NextIs(TokenKind kind)
        {
            return (this.Next().kind == kind);
        }


        private Token Advance()
        {
            var cur = this.Current();
            this.readhead++;
            return cur;
        }


        private Token Previous()
        {
            return this.tokenColl[this.readhead - 1];
        }


        private bool IsOver()
        {
            return (this.readhead >= this.tokenColl.tokens.Count);
        }


        private Token Match(TokenKind tokenKind, string errText)
        {
            if (this.Current().kind == tokenKind)
                return this.Advance();
            else
                throw this.FatalBefore(MessageCode.Expected, errText);
        }


        private bool MatchListSeparator(TokenKind separatorKind, TokenKind endingKind, MessageCode errCode, string errText)
        {
            if (this.Current().kind == separatorKind)
            {
                this.Advance();
                return false;
            }
            else if (this.Current().kind == endingKind)
                return true;
            else
                throw this.FatalAfterPrevious(errCode, errText);
        }


        private Core.CheckException FatalAt(Span span, MessageCode errCode, string errText)
        {
            this.session.AddMessage(MessageKind.Error, errCode, errText, span);
            return new Core.CheckException();
        }


        private Core.CheckException FatalBefore(MessageCode errCode, string errText)
        {
            return this.FatalAt(this.Current().span.JustBefore(), errCode, errText);
        }


        private Core.CheckException FatalCurrent(MessageCode errCode, string errText)
        {
            return this.FatalAt(this.Current().span, errCode, errText);
        }


        private Core.CheckException FatalAfterPrevious(MessageCode errCode, string errText)
        {
            return this.FatalAt(this.Previous().span.JustAfter(), errCode, errText);
        }


        #endregion


        #region Parser Methods


        public ASTNodeDeclGroup ParseDeclGroup()
        {
            var declGroupNode = new ASTNodeDeclGroup();
            declGroupNode.SetSpan(this.Current().span);

            // Parse use directives.
            while (!this.IsOver() && !this.CurrentIs(TokenKind.BraceClose) &&
                this.CurrentIs(TokenKind.KeywordUse))
            {
                var useNode = this.ParseUseDirective();
                this.Match(TokenKind.Semicolon, "expected ';'");
                declGroupNode.AddUseNode(useNode);
            }

            // Parse namespaces, functs, and structs.
            while (!this.IsOver() && !this.CurrentIs(TokenKind.BraceClose))
            {
                var declName = this.ParseName(false, false);
                if (this.CurrentIs(TokenKind.DoubleColon) && this.NextIs(TokenKind.BraceOpen))
                {
                    this.Advance();
                    var namespaceNode = new ASTNodeDeclNamespace();
                    namespaceNode.SetPathNode(declName.path);

                    this.Match(TokenKind.BraceOpen, "expected '{'");
                    namespaceNode.SetInnerGroupNode(ParseDeclGroup());
                    this.Match(TokenKind.BraceClose, "expected '}'");

                    declGroupNode.AddNamespaceDeclNode(namespaceNode);
                }
                else
                {
                    this.Match(TokenKind.Colon, "expected ':'");

                    if (this.CurrentIs(TokenKind.KeywordFn))
                    {
                        var fnNode = this.ParseDeclFunct(true);
                        fnNode.SetNameNode(declName);
                        declGroupNode.AddFunctDeclNode(fnNode);
                    }

                    else if (this.CurrentIs(TokenKind.KeywordStruct))
                    {
                        var stNode = this.ParseDeclStruct();
                        stNode.SetNameNode(declName);
                        declGroupNode.AddStructDeclNode(stNode);
                    }

                    else
                        throw this.FatalBefore(MessageCode.Expected, "expected 'struct' or 'fn'");
                }
            }

            declGroupNode.AddSpan(this.Current().span);
            return declGroupNode;
        }


        public ASTNodeUse ParseUseDirective()
        {
            var useSpan = this.Current().span;
            this.Match(TokenKind.KeywordUse, "expected 'use'");

            var pathNode = new ASTNodePath();
            pathNode.SetSpan(this.Current().span);
            pathNode.AddIdentifierNode(this.ParseIdentifier());
            while (this.CurrentIs(TokenKind.DoubleColon) && this.NextIs(TokenKind.Identifier))
            {
                this.Advance();
                pathNode.AddIdentifierNode(this.ParseIdentifier());
            }

            if (this.CurrentIs(TokenKind.DoubleColon) && this.NextIs(TokenKind.Placeholder))
            {
                this.Advance();
                this.Advance();

                var useNode = new ASTNodeUseAll();
                useNode.SetSpan(useSpan);
                useNode.SetPathNode(pathNode);
                return useNode;
            }
            else
                throw new NotImplementedException();
        }


        public ASTNodeDeclStruct ParseDeclStruct()
        {
            var structNode = new ASTNodeDeclStruct();
            structNode.SetSpan(this.Current().span);

            this.Match(TokenKind.KeywordStruct, "expected 'struct'");
            this.Match(TokenKind.BraceOpen, "expected '{'");

            // Parse use directives.
            while (!this.IsOver() && !this.CurrentIs(TokenKind.BraceClose) &&
                this.CurrentIs(TokenKind.KeywordUse))
            {
                var useNode = this.ParseUseDirective();
                this.Match(TokenKind.Semicolon, "expected ';'");
                structNode.AddUseNode(useNode);
            }

            // Parse fields.
            while (this.CurrentIsNot(TokenKind.BraceClose))
            {
                var fieldNode = new ASTNodeDeclStructField();
                fieldNode.SetSpan(this.Current().span);
                fieldNode.SetNameNode(this.ParseName(false, true));
                this.Match(TokenKind.Colon, "expected ':'");
                fieldNode.SetTypeNode(this.ParseType());
                structNode.AddFieldNode(fieldNode);

                this.MatchListSeparator(
                    TokenKind.Comma, TokenKind.BraceClose,
                    MessageCode.Expected, "expected ',' or '}'");
            }
            structNode.AddSpan(this.Current().span);
            this.Match(TokenKind.BraceClose, "expected '}'");

            return structNode;
        }


        public ASTNodeDeclFunct ParseDeclFunct(bool withBody)
        {
            var functNode = new ASTNodeDeclFunct();
            functNode.SetSpan(this.Current().span);

            this.Match(TokenKind.KeywordFn, "expected 'fn'");

            // Parse parameter list.
            this.Match(TokenKind.ParenOpen, "expected '('");
            while (this.CurrentIsNot(TokenKind.ParenClose))
            {
                var paramNode = new ASTNodeDeclFunctParam();
                paramNode.SetNameNode(this.ParseName(false, true));
                this.Match(TokenKind.Colon, "expected ':'");
                paramNode.SetTypeNode(this.ParseType());
                functNode.AddParameterNode(paramNode);
                if (this.Current().kind == TokenKind.Comma)
                    this.Advance();
                else if (this.Current().kind != TokenKind.ParenClose)
                    throw this.FatalAfterPrevious(MessageCode.Expected, "expected ',' or ')'");
            }
            this.Match(TokenKind.ParenClose, "expected ')'");

            // Parse return type.
            if (this.CurrentIs(TokenKind.Arrow))
            {
                this.Advance();
                functNode.SetReturnTypeNode(this.ParseType());
            }

            // Parse body.
            if (withBody)
            {
                functNode.SetBodyNode(this.ParseExprBlock());
            }

            return functNode;
        }


        public ASTNodeName ParseName(bool needsExplicitParameterSeparator, bool canBeRooted)
        {
            var nameNode = new ASTNodeName();
            nameNode.SetSpan(this.Current().span);

            var pathNode = new ASTNodePath();
            pathNode.SetSpan(this.Current().span);

            if (canBeRooted && this.CurrentIs(TokenKind.Placeholder))
            {
                this.Advance();
                pathNode.SetRooted(true);
                this.Match(TokenKind.DoubleColon, "expected '::'");
            }

            pathNode.AddIdentifierNode(this.ParseIdentifier());

            while (this.CurrentIs(TokenKind.DoubleColon) && this.NextIs(TokenKind.Identifier))
            {
                this.Advance();
                pathNode.AddIdentifierNode(this.ParseIdentifier());
            }
            nameNode.SetPathNode(pathNode);

            return nameNode;
        }


        public ASTNodeIdentifier ParseIdentifier()
        {
            var node = new ASTNodeIdentifier();
            node.SetSpan(this.Current().span);
            this.Match(TokenKind.Identifier, "expected identifier");
            return node;
        }


        public ASTNodeType ParseType()
        {
            // Parse a reference type.
            if (this.CurrentIs(TokenKind.Ampersand))
            {
                var refTypeNode = new ASTNodeTypeReference();
                refTypeNode.SetSpan(this.Current().span);
                this.Advance();
                if (this.CurrentIs(TokenKind.SingleQuote))
                    refTypeNode.SetLifetimeNode(this.ParseLifetime());
                refTypeNode.SetReferencedNode(this.ParseType());
                return refTypeNode;
            }
            // Parse a tuple type.
            else if (this.CurrentIs(TokenKind.ParenOpen))
            {
                var tupleTypeNode = new ASTNodeTypeTuple();
                tupleTypeNode.SetSpan(this.Current().span);
                this.Advance();
                while (this.CurrentIsNot(TokenKind.ParenClose))
                {
                    tupleTypeNode.AddElementNode(this.ParseType());
                    this.MatchListSeparator(
                        TokenKind.Comma, TokenKind.ParenClose,
                        MessageCode.Expected, "expected ',' or ')'");
                }
                tupleTypeNode.AddSpan(this.Current().span);
                this.Match(TokenKind.ParenClose, "expected ')'");
                return tupleTypeNode;
            }
            // Parse a placeholder type.
            else if (this.CurrentIs(TokenKind.Placeholder) && !this.NextIs(TokenKind.DoubleColon))
            {
                var placeholderTypeNode = new ASTNodeTypePlaceholder();
                placeholderTypeNode.SetSpan(this.Current().span);
                this.Advance();
                return placeholderTypeNode;
            }
            // Parse a struct type.
            else
            {
                var structTypeNode = new ASTNodeTypeStruct();
                structTypeNode.SetSpan(this.Current().span);
                structTypeNode.SetNameNode(this.ParseName(false, true));
                return structTypeNode;
            }
        }


        public ASTNodeLifetime ParseLifetime()
        {
            var span = this.Current().span;
            this.Match(TokenKind.SingleQuote, "expected single quote");

            if (this.CurrentIs(TokenKind.Placeholder))
            {
                var lifetimeNode = new ASTNodePlaceholderLifetime();
                lifetimeNode.SetSpan(span);
                lifetimeNode.AddSpan(this.Current().span);
                this.Advance();
                return lifetimeNode;
            }
            else
            {
                var lifetimeNode = new ASTNodeConcreteLifetime();
                lifetimeNode.SetSpan(span);
                lifetimeNode.SetIdentifierNode(this.ParseIdentifier());
                return lifetimeNode;
            }
        }


        public ASTNodeExpr ParseExpr()
        {
            if (this.CurrentIs(TokenKind.KeywordLet))
                return this.ParseExprLet();
            else if (this.CurrentIs(TokenKind.KeywordIf))
                return this.ParseExprIf();
            else if (this.CurrentIs(TokenKind.KeywordElse))
                throw this.FatalCurrent(MessageCode.UnmatchedElse, "unmatched 'else'");
            else if (this.CurrentIs(TokenKind.KeywordWhile))
                return this.ParseExprWhile();
            else if (this.CurrentIs(TokenKind.KeywordReturn))
                return this.ParseExprReturn();
            else
                return this.ParseBinaryOp(0);
        }


        public ASTNodeExprLet ParseExprLet()
        {
            var letNode = new ASTNodeExprLet();
            letNode.SetSpan(this.Current().span);
            this.Match(TokenKind.KeywordLet, "expected 'let'");

            if (this.CurrentIs(TokenKind.Identifier) || this.CurrentIs(TokenKind.Placeholder))
                letNode.SetDeclarationNode(this.ParseExprName());
            else
                throw this.FatalBefore(MessageCode.Expected, "expected declaration");

            if (this.CurrentIs(TokenKind.Colon))
            {
                this.Advance();
                letNode.SetTypeNode(this.ParseType());
            }
            if (this.CurrentIs(TokenKind.Equal))
            {
                this.Advance();
                letNode.SetInitializerNode(this.ParseExpr());
            }
            return letNode;
        }


        public ASTNodeExprIf ParseExprIf()
        {
            var ifNode = new ASTNodeExprIf();
            ifNode.SetSpan(this.Current().span);
            this.Match(TokenKind.KeywordIf, "expected 'if'");
            this.insideCondition.Push(true);
            ifNode.SetConditionNode(this.ParseExpr());
            this.insideCondition.Pop();
            ifNode.SetTrueBranchNode(this.ParseExprBlock());
            if (this.CurrentIs(TokenKind.KeywordElse))
            {
                this.Advance();
                if (this.CurrentIs(TokenKind.KeywordIf))
                    ifNode.SetFalseBranchNode(this.ParseExprIf());
                else
                    ifNode.SetFalseBranchNode(this.ParseExprBlock());
            }
            return ifNode;
        }


        public ASTNodeExprWhile ParseExprWhile()
        {
            var node = new ASTNodeExprWhile();
            node.SetSpan(this.Current().span);
            this.Match(TokenKind.KeywordWhile, "expected 'while'");
            this.insideCondition.Push(true);
            node.SetConditionNode(this.ParseExpr());
            this.insideCondition.Pop();
            node.SetBodyNode(this.ParseExprBlock());
            return node;
        }


        public ASTNodeExprReturn ParseExprReturn()
        {
            var retNode = new ASTNodeExprReturn();
            retNode.SetSpan(this.Current().span);
            this.Match(TokenKind.KeywordReturn, "expected 'return'");
            if (!this.CurrentIs(TokenKind.Semicolon) &&
                !this.CurrentIs(TokenKind.BraceClose) &&
                !this.CurrentIs(TokenKind.ParenClose))
            {
                retNode.SetExpressionNode(this.ParseExpr());
            }
            return retNode;
        }


        private class OperatorModel
        {
            public enum Associativity { Unary, Left, Right };


            public Associativity associativity;
            public TokenKind tokenKind;


            public OperatorModel(Associativity assoc, TokenKind tokenKind)
            {
                this.associativity = assoc;
                this.tokenKind = tokenKind;
            }
        }


        private static readonly List<OperatorModel>[] opList = new List<OperatorModel>[]
        {
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Right, TokenKind.Equal)
            },
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.Ampersand),
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.VerticalBar),
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.Circumflex)
            },
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.DoubleEqual),
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.ExclamationMarkEqual),
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.LessThan),
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.LessThanEqual),
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.GreaterThan),
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.GreaterThanEqual)
            },
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.Plus),
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.Minus)
            },
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.Asterisk),
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.Slash),
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.PercentSign)
            },
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Unary, TokenKind.Minus),
                new OperatorModel(OperatorModel.Associativity.Unary, TokenKind.ExclamationMark)
            },
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Unary, TokenKind.At),
                new OperatorModel(OperatorModel.Associativity.Unary, TokenKind.Ampersand)
            },
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.Period)
            }
        };


        private static readonly Dictionary<TokenKind, ASTNodeExprUnaryOp.Operator> unaryOperators =
            new Dictionary<TokenKind, ASTNodeExprUnaryOp.Operator>()
            {
                { TokenKind.Minus, ASTNodeExprUnaryOp.Operator.Minus },
                { TokenKind.ExclamationMark, ASTNodeExprUnaryOp.Operator.Exclamation },
                { TokenKind.At, ASTNodeExprUnaryOp.Operator.At },
                { TokenKind.Ampersand, ASTNodeExprUnaryOp.Operator.Ampersand },
            };


        private static readonly Dictionary<TokenKind, ASTNodeExprBinaryOp.Operator> binaryOperators =
            new Dictionary<TokenKind, ASTNodeExprBinaryOp.Operator>()
            {
                { TokenKind.Equal, ASTNodeExprBinaryOp.Operator.Equal },
                { TokenKind.Period, ASTNodeExprBinaryOp.Operator.Dot },
                { TokenKind.Plus, ASTNodeExprBinaryOp.Operator.Plus },
                { TokenKind.Minus, ASTNodeExprBinaryOp.Operator.Minus },
                { TokenKind.Asterisk, ASTNodeExprBinaryOp.Operator.Asterisk },
                { TokenKind.Slash, ASTNodeExprBinaryOp.Operator.Slash },
                { TokenKind.Ampersand, ASTNodeExprBinaryOp.Operator.Ampersand },
                { TokenKind.VerticalBar, ASTNodeExprBinaryOp.Operator.VerticalBar },
                { TokenKind.Circumflex, ASTNodeExprBinaryOp.Operator.Circumflex },
            };


        public ASTNodeExpr ParseBinaryOp(int level)
        {
            // If reached the end of operators list, continue parsing inner expressions.
            if (level >= opList.GetLength(0))
                return this.ParseExprCall();

            // Try to find a unary operator that matches the current token.
            var unaryMatch = opList[level].Find(
                op => op.associativity == OperatorModel.Associativity.Unary &&
                this.CurrentIs(op.tokenKind));

            if (unaryMatch != null)
            {
                // Prepare the unary node.
                var unaryOpNode = new ASTNodeExprUnaryOp();
                unaryOpNode.SetSpan(this.Current().span);
                unaryOpNode.SetOperator(unaryOperators[this.Current().kind]);
                this.Advance();

                // Parse the unary operand.
                unaryOpNode.SetOperandNode(this.ParseBinaryOp(level));

                return unaryOpNode;
            }

            // If no unary operator matched, parse the left-hand side of a binary expression.
            var lhsNode = this.ParseBinaryOp(level + 1);

            // Infinite loop for left associativity.
            while (true)
            {
                var binaryOpNode = new ASTNodeExprBinaryOp();

                // Find a binary operator that matches the current token.
                var binaryMatch = opList[level].Find(
                    op => op.associativity != OperatorModel.Associativity.Unary &&
                    this.CurrentIs(op.tokenKind));

                // If no operator matched, return the current left-hand side.
                if (binaryMatch == null)
                    return lhsNode;

                binaryOpNode.SetOperator(binaryOperators[this.Current().kind]);
                this.Advance();

                // Parse right-hand side. 
                ASTNodeExpr rhsNode;
                if (binaryMatch.associativity == OperatorModel.Associativity.Right)
                    rhsNode = this.ParseExpr();
                else
                    rhsNode = this.ParseBinaryOp(level + 1);

                binaryOpNode.SetLeftOperandNode(lhsNode);
                binaryOpNode.SetRightOperandNode(rhsNode);

                // In a right-associative operator, return the current binary op node.
                if (binaryMatch.associativity == OperatorModel.Associativity.Right)
                    return binaryOpNode;

                // In a left-associative operator, set the current binary op node
                // as the left-hand side for the next iteration.
                lhsNode = binaryOpNode;
            }
        }


        public ASTNodeExpr ParseExprCall()
        {
            var calledNode = this.ParseExprLeaf();
            if (this.CurrentIsNot(TokenKind.ParenOpen))
                return calledNode;

            this.Advance();

            var callNode = new ASTNodeExprCall();
            callNode.SetCalledNode(calledNode);

            while (this.CurrentIsNot(TokenKind.ParenClose))
            {
                callNode.AddArgumentNode(this.ParseExpr());
                this.MatchListSeparator(TokenKind.Comma, TokenKind.ParenClose,
                    MessageCode.Expected, "expected ',' or ')'");
            }

            callNode.AddSpan(this.Current().span);
            this.Match(TokenKind.ParenClose, "expected ')'");

            return callNode;
        }


        /*public ASTNode ParseStructLiteral()
        {
            var targetNode = this.ParseLeafExpression();
            if (this.CurrentIsNot(TokenKind.BraceOpen) || this.insideCondition.Peek())
                return targetNode;

            if (targetNode.kind != ASTNodeKind.Name)
                return targetNode;

            var typeNode = new ASTNode(ASTNodeKind.Type);
            typeNode.AddChild(targetNode);

            var node = new ASTNode(ASTNodeKind.StructLiteral);
            node.AddChild(typeNode);
            node.AddSpan(this.Current().span);
            this.Match(TokenKind.BraceOpen, MessageCode.Expected, "expected '{'");

            while (this.CurrentIsNot(TokenKind.BraceClose))
            {
                var member = new ASTNode(ASTNodeKind.StructFieldInit);
                member.AddChild(this.ParseName(false));
                this.Match(TokenKind.Colon, MessageCode.Expected, "expected ':'");
                member.AddChild(ParseExpression());
                node.AddChild(member);
                this.MatchListSeparator(TokenKind.Comma, TokenKind.BraceClose,
                    MessageCode.Expected, "expected ',' or '}'");
            }
            node.AddSpan(this.Current().span);
            this.Match(TokenKind.BraceClose, MessageCode.Expected, "expected '}'");

            return node;
        }*/


        public ASTNodeExpr ParseExprLeaf()
        {
            if (this.CurrentIs(TokenKind.BraceOpen))
                return this.ParseExprBlock();
            else if (this.CurrentIs(TokenKind.ParenOpen))
                return this.ParseExprParenthesized();
            else if (this.CurrentIs(TokenKind.Number))
                return this.ParseExprLiteralInt();
            else if (this.CurrentIs(TokenKind.BooleanTrue) || this.CurrentIs(TokenKind.BooleanFalse))
                return this.ParseExprLiteralBool();
            else if (this.CurrentIs(TokenKind.Identifier) || this.CurrentIs(TokenKind.Placeholder))
                return this.ParseExprName();
            else
                throw this.FatalBefore(MessageCode.Expected, "expected expression");
        }


        public ASTNodeExprBlock ParseExprBlock()
        {
            var blockNode = new ASTNodeExprBlock();
            blockNode.SetSpan(this.Match(TokenKind.BraceOpen, "expected '{'").span);
            while (this.CurrentIsNot(TokenKind.BraceClose))
            {
                blockNode.AddSubexpressionNode(this.ParseExpr());
                this.MatchListSeparator(TokenKind.Semicolon, TokenKind.BraceClose,
                    MessageCode.Expected, "expected ';' or '}'");
            }
            blockNode.AddSpan(this.Match(TokenKind.BraceClose, "expected '}'").span);
            return blockNode;
        }


        public ASTNodeExprParenthesized ParseExprParenthesized()
        {
            var parenNode = new ASTNodeExprParenthesized();
            parenNode.SetSpan(this.Match(TokenKind.ParenOpen, "expected '('").span);
            this.insideCondition.Push(false);
            parenNode.SetInnerExpressionNode(this.ParseExpr());
            this.insideCondition.Pop();
            parenNode.AddSpan(this.Match(TokenKind.ParenClose, "expected ')'").span);
            return parenNode;
        }


        public ASTNodeExprName ParseExprName()
        {
            if (this.CurrentIs(TokenKind.Placeholder))
            {
                var placeholderNode = new ASTNodeExprNamePlaceholder();
                placeholderNode.SetSpan(this.Advance().span);
                return placeholderNode;
            }
            else
            {
                var nameNode = new ASTNodeExprNameConcrete();
                nameNode.SetNameNode(this.ParseName(true, true));
                return nameNode;
            }
        }


        public ASTNodeExprLiteralBool ParseExprLiteralBool()
        {
            if (!this.CurrentIs(TokenKind.BooleanFalse) &&
                !this.CurrentIs(TokenKind.BooleanTrue))
                throw this.FatalBefore(MessageCode.Expected, "expected boolean literal");

            var token = this.Advance();
            var boolNode = new ASTNodeExprLiteralBool();
            boolNode.SetSpan(token.span);
            boolNode.SetValue(token.kind == TokenKind.BooleanTrue);
            return boolNode;
        }


        public ASTNodeExprLiteralInt ParseExprLiteralInt()
        {
            var token = this.Match(TokenKind.Number, "expected integer literal");

            int radix;
            string value;
            Integer.Type type;
            if (!Integer.Parse(token.span.GetExcerpt(), out radix, out value, out type))
                throw this.FatalAt(token.span, MessageCode.InvalidFormat, "invalid integer");

            var intNode = new ASTNodeExprLiteralInt();
            intNode.SetSpan(token.span);
            intNode.SetRadix(radix);
            intNode.SetValue(value);
            intNode.SetType(type);
            return intNode;
        }

        #endregion
    }
}
