using System;
using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Grammar
{
    public class ASTParser
    {
        private int readhead;
        private TokenCollection tokenColl;
        private List<ASTNode> topDeclNodes;
        private Infrastructure.Session session;
        private Stack<bool> insideCondition;


        public ASTParser(Infrastructure.Session session, TokenCollection tokenColl, int startToken = 0)
        {
            this.readhead = startToken;
            this.tokenColl = tokenColl;
            this.topDeclNodes = new List<ASTNode>();
            this.session = session;
            this.insideCondition = new Stack<bool>();
            this.insideCondition.Push(false);
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


        private Infrastructure.CheckException FatalAt(Span span, MessageCode errCode, string errText)
        {
            this.session.AddMessage(MessageKind.Error, errCode, errText, span);
            return new Infrastructure.CheckException();
        }


        private Infrastructure.CheckException FatalBefore(MessageCode errCode, string errText)
        {
            return this.FatalAt(this.Current().span.JustBefore(), errCode, errText);
        }


        private Infrastructure.CheckException FatalCurrent(MessageCode errCode, string errText)
        {
            return this.FatalAt(this.Current().span, errCode, errText);
        }


        private Infrastructure.CheckException FatalAfterPrevious(MessageCode errCode, string errText)
        {
            return this.FatalAt(this.Previous().span.JustAfter(), errCode, errText);
        }


        #endregion


        #region Parser Methods


        public ASTNodeTopLevel ParseTopLevel()
        {
            var topLevelNode = new ASTNodeTopLevel();
            topLevelNode.SetSpan(this.Current().span);

            while (!this.IsOver())
            {
                if (this.CurrentIs(TokenKind.KeywordFn))
                    topLevelNode.AddStatementNode(this.ParseFunctDecl(true));

                else if (this.CurrentIs(TokenKind.KeywordStruct))
                    topLevelNode.AddStatementNode(this.ParseDeclStruct());

                else
                    throw this.FatalBefore(MessageCode.Expected, "expected 'struct', 'fn', or 'trait'");
            }

            topLevelNode.AddSpan(this.Current().span);
            return topLevelNode;
        }


        private ASTNodeDeclFunct ParseFunctDecl(bool withBody)
        {
            var functNode = new ASTNodeDeclFunct();
            functNode.SetSpan(this.Current().span);

            this.Match(TokenKind.KeywordFn, "expected 'fn'");
            functNode.SetNameNode(this.ParseName(false));

            this.Match(TokenKind.ParenOpen, "expected '('");
            while (this.CurrentIsNot(TokenKind.ParenClose))
            {
                var paramNode = new ASTNodeDeclFunctParam();
                paramNode.SetNameNode(this.ParseName(false));
                this.Match(TokenKind.Colon, "expected ':'");
                paramNode.SetTypeNode(this.ParseType());
                functNode.AddParameterNode(paramNode);
                if (this.Current().kind == TokenKind.Comma)
                    this.Advance();
                else if (this.Current().kind != TokenKind.ParenClose)
                    throw this.FatalAfterPrevious(MessageCode.Expected, "expected ',' or ')'");
            }
            this.Match(TokenKind.ParenClose, "expected ')'");

            if (this.CurrentIs(TokenKind.Arrow))
            {
                this.Advance();
                functNode.SetReturnTypeNode(this.ParseType());
            }

            if (withBody)
            {
                functNode.SetBodyNode(this.ParseExprBlock());
            }

            return functNode;
        }


        private ASTNodeDeclStruct ParseDeclStruct()
        {
            var structNode = new ASTNodeDeclStruct();
            structNode.SetSpan(this.Current().span);

            this.Match(TokenKind.KeywordStruct, "expected 'struct'");
            structNode.SetNameNode(this.ParseName(false));

            this.Match(TokenKind.BraceOpen, "expected '{'");
            while (this.CurrentIsNot(TokenKind.BraceClose))
            {
                var fieldNode = new ASTNodeDeclStructField();
                fieldNode.SetSpan(this.Current().span);
                fieldNode.SetNameNode(this.ParseName(false));
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


        private ASTNodeName ParseName(bool needsExplicitParameterSeparator)
        {
            var nameNode = new ASTNodeName();
            nameNode.SetSpan(this.Current().span);

            var pathNode = new ASTNodePath();
            pathNode.SetSpan(this.Current().span);
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
            // Parse a struct type.
            else
            {
                var structTypeNode = new ASTNodeTypeStruct();
                structTypeNode.SetSpan(this.Current().span);
                structTypeNode.SetNameNode(this.ParseName(false));
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


        private ASTNodeExpr ParseExpr()
        {
            /*if (this.CurrentIs(TokenKind.KeywordLet))
                return this.ParseLetExpression();
            else if (this.CurrentIs(TokenKind.KeywordIf))
                return this.ParseIfExpression();
            else if (this.CurrentIs(TokenKind.KeywordElse))
                throw this.FatalCurrent(MessageCode.UnmatchedElse, "unmatched 'else'");
            else if (this.CurrentIs(TokenKind.KeywordWhile))
                return this.ParseWhileExpression();
            else if (this.CurrentIs(TokenKind.KeywordReturn))
                return this.ParseReturnExpression();
            else*/
                return this.ParseBinaryOp(0);
        }


        /*private ASTNode ParseLetExpression()
        {
            var node = new ASTNode(ASTNodeKind.ControlLet);
            node.AddSpan(this.Current().span);
            this.Match(TokenKind.KeywordLet, MessageCode.Expected, "expected 'let'");
            node.AddChild(this.ParseName(false));
            if (this.CurrentIs(TokenKind.Colon))
            {
                this.Advance();
                node.AddChild(this.ParseType());
            }
            if (this.CurrentIs(TokenKind.Equal))
            {
                this.Advance();
                node.AddChild(this.ParseExpression());
            }
            return node;
        }


        private ASTNode ParseIfExpression()
        {
            var node = new ASTNode(ASTNodeKind.ControlIf);
            node.AddSpan(this.Current().span);
            this.Match(TokenKind.KeywordIf, MessageCode.Expected, "expected 'if'");
            this.insideCondition.Push(true);
            node.AddChild(this.ParseExpression());
            this.insideCondition.Pop();
            node.AddChild(this.ParseBlock());
            if (this.CurrentIs(TokenKind.KeywordElse))
            {
                this.Advance();
                node.AddChild(this.ParseBlock());
            }
            return node;
        }


        private ASTNode ParseWhileExpression()
        {
            var node = new ASTNode(ASTNodeKind.ControlWhile);
            node.AddSpan(this.Current().span);
            this.Match(TokenKind.KeywordWhile, MessageCode.Expected, "expected 'while'");
            this.insideCondition.Push(true);
            node.AddChild(this.ParseExpression());
            this.insideCondition.Pop();
            node.AddChild(this.ParseBlock());
            return node;
        }


        private ASTNode ParseReturnExpression()
        {
            var node = new ASTNode(ASTNodeKind.ControlReturn);
            node.AddSpan(this.Current().span);
            this.Match(TokenKind.KeywordReturn, MessageCode.Expected, "expected 'return'");
            if (!this.CurrentIs(TokenKind.Semicolon) &&
                !this.CurrentIs(TokenKind.BraceClose) &&
                !this.CurrentIs(TokenKind.ParenClose))
            {
                node.AddChild(this.ParseExpression());
            }
            return node;
        }*/


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
                { TokenKind.Plus, ASTNodeExprBinaryOp.Operator.Plus },
                { TokenKind.Minus, ASTNodeExprBinaryOp.Operator.Minus },
                { TokenKind.Asterisk, ASTNodeExprBinaryOp.Operator.Asterisk },
                { TokenKind.Slash, ASTNodeExprBinaryOp.Operator.Slash },
                { TokenKind.Ampersand, ASTNodeExprBinaryOp.Operator.Ampersand },
                { TokenKind.VerticalBar, ASTNodeExprBinaryOp.Operator.VerticalBar },
                { TokenKind.Circumflex, ASTNodeExprBinaryOp.Operator.Circumflex },
            };


        private ASTNodeExpr ParseBinaryOp(int level)
        {
            // If reached the end of operators list, continue parsing inner expressions.
            if (level >= opList.GetLength(0))
                return this.ParseExprLeaf();

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


        /*private ASTNode ParseCallExpression()
        {
            var targetNode = this.ParseStructLiteral();
            if (this.CurrentIsNot(TokenKind.ParenOpen))
                return targetNode;

            this.Advance();

            var callNode = new ASTNode(ASTNodeKind.Call);
            callNode.AddChild(targetNode);

            while (this.CurrentIsNot(TokenKind.ParenClose))
            {
                callNode.AddChild(this.ParseExpression());
                this.MatchListSeparator(TokenKind.Comma, TokenKind.ParenClose,
                    MessageCode.Expected, "expected ',' or ')'");
            }

            callNode.AddSpan(this.Current().span);
            this.Match(TokenKind.ParenClose, MessageCode.Expected, "expected ')'");

            return callNode;
        }


        private ASTNode ParseStructLiteral()
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


        private ASTNodeExpr ParseExprLeaf()
        {
            if (this.CurrentIs(TokenKind.BraceOpen))
                return this.ParseExprBlock();
            else if (this.CurrentIs(TokenKind.ParenOpen))
                return this.ParseExprParenthesized();
            else if (this.CurrentIs(TokenKind.Number))
                return this.ParseExprLiteralInt();
            else if (this.CurrentIs(TokenKind.BooleanTrue) || this.CurrentIs(TokenKind.BooleanFalse))
                return this.ParseExprLiteralBool();
            /*else if (this.CurrentIs(TokenKind.Identifier))
                return this.ParseName(true);*/
            else
                throw this.FatalBefore(MessageCode.Expected, "expected expression");
        }


        private ASTNodeExprBlock ParseExprBlock()
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


        private ASTNodeExprParenthesized ParseExprParenthesized()
        {
            var parenNode = new ASTNodeExprParenthesized();
            parenNode.SetSpan(this.Match(TokenKind.ParenOpen, "expected '('").span);
            this.insideCondition.Push(false);
            parenNode.SetInnerExpressionNode(this.ParseExpr());
            this.insideCondition.Pop();
            parenNode.AddSpan(this.Match(TokenKind.ParenClose, "expected ')'").span);
            return parenNode;
        }


        private ASTNodeExprLiteralBool ParseExprLiteralBool()
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


        private ASTNodeExprLiteralInt ParseExprLiteralInt()
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
