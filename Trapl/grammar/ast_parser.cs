using System;
using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Grammar
{
    public class ASTParser
    {
        public static AST Parse(Interface.Session session, TokenCollection tokenColl)
        {
            var parser = new ASTParser(session, tokenColl);

            try { parser.ParseTopLevel(); }
            catch (ParserException) { }

            return parser.ast;
        }

        public static ASTNode ParsePattern(Interface.Session session, TokenCollection tokenColl)
        {
            var parser = new ASTParser(session, tokenColl);
            ASTNode result = null;

            try { result = parser.ParseParameterPattern(); }
            catch (ParserException) { }

            return result;
        }

        public static ASTNode ParseType(Interface.Session session, TokenCollection tokenColl)
        {
            var parser = new ASTParser(session, tokenColl);
            ASTNode result = null;

            try { result = parser.ParseType(); }
            catch (ParserException) { }

            return result;
        }


        private class ParserException : Exception
        {

        }


        private int readhead;
        private TokenCollection tokenColl;
        private AST ast;
        private Interface.Session session;


        private ASTParser(Interface.Session session, TokenCollection tokenColl)
        {
            this.readhead = 0;
            this.tokenColl = tokenColl;
            this.ast = new AST();
            this.session = session;
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


        private Token Match(TokenKind tokenKind, MessageCode errCode, string errText)
        {
            if (this.Current().kind == tokenKind)
                return this.Advance();
            else
                throw this.FatalBefore(errCode, errText);
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


        private ParserException FatalBefore(MessageCode errCode, string errText)
        {
            this.session.diagn.Add(MessageKind.Error, errCode, errText, this.Current().span.JustBefore());
            return new ParserException();
        }


        private ParserException FatalCurrent(MessageCode errCode, string errText)
        {
            this.session.diagn.Add(MessageKind.Error, errCode, errText, this.Current().span);
            return new ParserException();
        }


        private ParserException FatalAfterPrevious(MessageCode errCode, string errText)
        {
            this.session.diagn.Add(MessageKind.Error, errCode, errText, this.Previous().span.JustAfter());
            return new ParserException();
        }


        #endregion


        #region Parser Methods


        private void ParseTopLevel()
        {
            while (!this.IsOver())
            {
                if (this.CurrentIs(TokenKind.Identifier))
                {
                    var node = new ASTNode(ASTNodeKind.TopLevelDecl);
                    node.SetSpan(this.Current().span);
                    node.AddChild(this.ParseNameWithParameterPattern(MessageCode.Expected, "expected declaration name"));
                    this.Match(TokenKind.Colon, MessageCode.Expected, "expected ':'");
                    if (this.CurrentIs(TokenKind.KeywordFunct))
                        node.AddChild(this.ParseFunctDecl(true));
                    else if (this.CurrentIs(TokenKind.KeywordStruct))
                        node.AddChild(this.ParseStructDecl());
                    else if (this.CurrentIs(TokenKind.KeywordTrait))
                        node.AddChild(this.ParseTraitDecl());
                    else
                        throw this.FatalBefore(MessageCode.Expected, "expected 'funct', 'struct' or 'trait'");
                    node.AddLastChildSpan();
                    this.ast.topDecls.Add(node);
                }
                else
                    throw this.FatalBefore(MessageCode.Expected, "expected a declaration");
            }
        }


        private ASTNode ParseFunctDecl(bool withBody)
        {
            var node = new ASTNode(ASTNodeKind.FunctDecl);
            node.SetSpan(this.Current().span);
            this.Match(TokenKind.KeywordFunct, MessageCode.Expected, "expected 'funct'");
            this.Match(TokenKind.ParenOpen, MessageCode.Expected, "expected '('");
            while (this.CurrentIsNot(TokenKind.ParenClose, TokenKind.Arrow))
            {
                var argNode = new ASTNode(ASTNodeKind.FunctArgDecl);
                argNode.AddChild(this.ParseIdentifier(MessageCode.Expected, "expected argument name"));
                argNode.SetLastChildSpan();
                this.Match(TokenKind.Colon, MessageCode.Expected, "expected ':'");
                argNode.AddChild(this.ParseType());
                argNode.AddLastChildSpan();
                node.AddChild(argNode);
                node.AddLastChildSpan();
                if (this.Current().kind == TokenKind.Comma)
                    this.Advance();
                else if (this.Current().kind != TokenKind.ParenClose &&
                    this.Current().kind != TokenKind.Arrow)
                    throw this.FatalAfterPrevious(MessageCode.Expected, "expected ',', '->' or ')'");
            }
            if (this.CurrentIs(TokenKind.Arrow))
            {
                this.Advance();
                var retNode = new ASTNode(ASTNodeKind.FunctReturnDecl);
                retNode.AddChild(this.ParseType());
                retNode.SetLastChildSpan();
                node.AddChild(retNode);
                node.AddLastChildSpan();
            }
            this.Match(TokenKind.ParenClose, MessageCode.Expected, "expected ')'");
            if (withBody || this.CurrentIsNot(TokenKind.Semicolon))
            {
                node.AddChild(this.ParseBlock());
                node.AddLastChildSpan();
            }
            else if (this.CurrentIs(TokenKind.Semicolon))
                this.Advance();
            return node;
        }


        private ASTNode ParseStructDecl()
        {
            var node = new ASTNode(ASTNodeKind.StructDecl);
            node.SetSpan(this.Current().span);
            this.Match(TokenKind.KeywordStruct, MessageCode.Expected, "expected 'struct'");
            this.Match(TokenKind.BraceOpen, MessageCode.Expected, "expected '{'");
            while (this.CurrentIsNot(TokenKind.BraceClose))
            {
                var memberNode = new ASTNode(ASTNodeKind.StructMemberDecl);
                memberNode.AddChild(this.ParseIdentifier(MessageCode.Expected, "expected member name"));
                memberNode.SetLastChildSpan();
                this.Match(TokenKind.Colon, MessageCode.Expected, "expected ':'");
                memberNode.AddChild(this.ParseType());
                memberNode.AddLastChildSpan();
                node.AddChild(memberNode);
                this.MatchListSeparator(TokenKind.Comma, TokenKind.BraceClose,
                    MessageCode.Expected, "expected ',' or '}'");
            }
            node.AddSpan(this.Current().span);
            this.Match(TokenKind.BraceClose, MessageCode.Expected, "expected '}'");
            return node;
        }


        private ASTNode ParseTraitDecl()
        {
            var node = new ASTNode(ASTNodeKind.TraitDecl);
            node.SetSpan(this.Current().span);
            this.Match(TokenKind.KeywordTrait, MessageCode.Expected, "expected 'trait'");
            this.Match(TokenKind.BraceOpen, MessageCode.Expected, "expected '{'");
            while (this.CurrentIsNot(TokenKind.BraceClose))
            {
                var memberNode = new ASTNode(ASTNodeKind.TraitMemberDecl);
                memberNode.AddChild(this.ParseIdentifier(MessageCode.Expected, "expected funct name"));
                memberNode.SetLastChildSpan();
                this.Match(TokenKind.Colon, MessageCode.Expected, "expected ':'");
                memberNode.AddChild(this.ParseFunctDecl(false));
                memberNode.AddLastChildSpan();
                node.AddChild(memberNode);
                this.MatchListSeparator(TokenKind.Semicolon, TokenKind.BraceClose,
                    MessageCode.Expected, "expected ';' or '}'");
            }
            node.AddSpan(this.Current().span);
            this.Match(TokenKind.BraceClose, MessageCode.Expected, "expected '}'");
            return node;
        }

        private ASTNode ParseName(MessageCode errCode, string errText)
        {
            var nameToken = this.Match(TokenKind.Identifier, errCode, errText);
            return new ASTNode(ASTNodeKind.Name, nameToken.span);
        }


        private ASTNode ParseIdentifier(MessageCode errCode, string errText)
        {
            var node = new ASTNode(ASTNodeKind.Identifier);
            node.AddChild(this.ParseName(errCode, errText));
            node.SetLastChildSpan();
            return node;
        }


        private ASTNode ParseNumberLiteral()
        {
            var token = this.Match(TokenKind.Number, MessageCode.Expected, "expected number");
            return new ASTNode(ASTNodeKind.NumberLiteral, token.span);
        }


        private ASTNode ParseNameWithParameterPattern(MessageCode errCode, string errText)
        {
            var node = new ASTNode(ASTNodeKind.Identifier);
            node.SetSpan(this.Current().span);
            var nameToken = this.Match(TokenKind.Identifier, errCode, errText);
            node.AddChild(new ASTNode(ASTNodeKind.Name, nameToken.span));
            node.AddLastChildSpan();

            if (this.CurrentIs(TokenKind.DoubleColon))
            {
                this.Advance();
                node.AddChild(this.ParseParameterPattern());
                node.AddLastChildSpan();
            }
            else
            {
                node.AddChild(new ASTNode(ASTNodeKind.ParameterPattern, node.Span().JustAfter()));
            }

            return node;
        }


        public ASTNode ParseParameterPattern()
        {
            var node = new ASTNode(ASTNodeKind.ParameterPattern);
            node.SetSpan(this.Current().span);
            this.Match(TokenKind.LessThan, MessageCode.Expected, "expected '<'");
            while (this.CurrentIsNot(TokenKind.GreaterThan))
            {
                node.AddChild(this.ParseType());

                if (this.CurrentIs(TokenKind.TriplePeriod))
                {
                    this.Advance();
                    this.MatchListSeparator(TokenKind.Comma, TokenKind.GreaterThan,
                        MessageCode.Expected, "expected ',' or '>'");
                    node.kind = ASTNodeKind.VariadicParameterPattern;
                    break;
                }
                else
                    this.MatchListSeparator(TokenKind.Comma, TokenKind.GreaterThan,
                        MessageCode.Expected, "expected ',' or '>'");
            }
            node.AddSpan(this.Current().span);
            this.Match(TokenKind.GreaterThan, MessageCode.Expected, "expected '>'");
            return node;
        }


        private ASTNode ParseType()
        {
            var node = new ASTNode(ASTNodeKind.TypeName);
            node.SetSpan(this.Current().span);

            while (this.CurrentIs(TokenKind.Ampersand))
            {
                node.AddChild(new ASTNode(ASTNodeKind.Operator, this.Advance().span));
                node.AddLastChildSpan();
            }

            if (this.CurrentIs(TokenKind.KeywordGen))
            {
                this.Advance();
                node.AddChild(new ASTNode(ASTNodeKind.GenericIdentifier,
                    this.Match(TokenKind.Identifier, MessageCode.Expected, "expected generic name").span));
                node.AddLastChildSpan();
            }
            else
            {
                node.AddChild(new ASTNode(ASTNodeKind.Identifier,
                    this.Match(TokenKind.Identifier, MessageCode.Expected, "expected type name").span));
                node.AddLastChildSpan();
            }

            if (this.CurrentIs(TokenKind.DoubleColon))
            {
                this.Advance();
                node.AddChild(this.ParseParameterPattern());
                node.AddLastChildSpan();
            }
            else
            {
                node.AddChild(new ASTNode(ASTNodeKind.ParameterPattern, node.Span().JustAfter()));
            }

            return node;
        }


        private ASTNode ParseBlock()
        {
            var node = new ASTNode(ASTNodeKind.Block);
            node.SetSpan(this.Current().span);
            this.Match(TokenKind.BraceOpen, MessageCode.Expected, "expected '{'");
            while (this.CurrentIsNot(TokenKind.BraceClose))
            {
                node.AddChild(ParseExpression());
                this.MatchListSeparator(TokenKind.Semicolon, TokenKind.BraceClose,
                    MessageCode.Expected, "expected ';' or '}'");
            }
            node.AddSpan(this.Current().span);
            this.Match(TokenKind.BraceClose, MessageCode.Expected, "expected '}'");
            return node;
        }


        private ASTNode ParseExpression()
        {
            if (this.CurrentIs(TokenKind.KeywordLet))
                return this.ParseLetExpression();
            else if (this.CurrentIs(TokenKind.KeywordIf))
                return this.ParseIfExpression();
            else if (this.CurrentIs(TokenKind.KeywordElse))
                throw this.FatalCurrent(MessageCode.UnmatchedElse, "unmatched 'else'");
            else if (this.CurrentIs(TokenKind.KeywordWhile))
                return this.ParseWhileExpression();
            else if (this.CurrentIs(TokenKind.KeywordReturn))
                return this.ParseReturnExpression();
            else
                return this.ParseBinaryOp(0);
        }


        private ASTNode ParseLetExpression()
        {
            var node = new ASTNode(ASTNodeKind.ControlLet);
            node.SetSpan(this.Current().span);
            this.Match(TokenKind.KeywordLet, MessageCode.Expected, "expected 'let'");
            node.AddChild(this.ParseName(MessageCode.Expected, "expected variable name"));
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
            node.AddLastChildSpan();
            return node;
        }


        private ASTNode ParseIfExpression()
        {
            var node = new ASTNode(ASTNodeKind.ControlIf);
            node.SetSpan(this.Current().span);
            this.Match(TokenKind.KeywordIf, MessageCode.Expected, "expected 'if'");
            node.AddChild(this.ParseExpression());
            node.AddChild(this.ParseBlock());
            if (this.CurrentIs(TokenKind.KeywordElse))
            {
                this.Advance();
                node.AddChild(this.ParseBlock());
            }
            node.AddLastChildSpan();
            return node;
        }


        private ASTNode ParseWhileExpression()
        {
            var node = new ASTNode(ASTNodeKind.ControlWhile);
            node.SetSpan(this.Current().span);
            this.Match(TokenKind.KeywordWhile, MessageCode.Expected, "expected 'while'");
            node.AddChild(this.ParseExpression());
            node.AddChild(this.ParseBlock());
            node.AddLastChildSpan();
            return node;
        }


        private ASTNode ParseReturnExpression()
        {
            var node = new ASTNode(ASTNodeKind.ControlReturn);
            node.SetSpan(this.Current().span);
            this.Match(TokenKind.KeywordReturn, MessageCode.Expected, "expected 'return'");
            if (!this.CurrentIs(TokenKind.Semicolon) &&
                !this.CurrentIs(TokenKind.BraceClose) &&
                !this.CurrentIs(TokenKind.ParenClose))
            {
                node.AddChild(this.ParseExpression());
                node.AddLastChildSpan();
            }
            return node;
        }


        private class OperatorModel
        {
            public enum Associativity { Left, Right };


            public Associativity associativity;
            public TokenKind tokenKind;


            public OperatorModel(Associativity assoc, TokenKind tokenKind)
            {
                this.associativity = assoc;
                this.tokenKind = tokenKind;
            }
        }


        private static readonly List<OperatorModel>[] binaryOpList = new List<OperatorModel>[]
        {
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Right, TokenKind.Equal)
            },
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.Plus),
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.Minus)
            },
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.Asterisk),
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.Slash)
            },
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.Period)
            }
        };


        private static readonly List<OperatorModel>[] unaryOpList = new List<OperatorModel>[]
        {
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.Plus),
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.Minus)
            },
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.At),
                new OperatorModel(OperatorModel.Associativity.Left, TokenKind.Ampersand)
            }
        };


        private ASTNode ParseBinaryOp(int level)
        {
            // If reached the end of operators list, continue parsing inner expressions.
            if (level >= binaryOpList.GetLength(0))
                return this.ParseUnaryOp(0);

            // Parse left-hand side.
            var lhsNode = this.ParseBinaryOp(level + 1);

            // Infinite loop for left associativity.
            while (true)
            {
                var node = new ASTNode(ASTNodeKind.BinaryOp);

                // Find a binary operator that matches the current token.
                var match = binaryOpList[level].Find(op => this.CurrentIs(op.tokenKind));

                // If no operator matched, return the current left-hand side.
                if (match == null)
                    return lhsNode;

                node.AddChild(new ASTNode(ASTNodeKind.Operator, this.Current().span));
                this.Advance();

                // Parse right-hand side. 
                ASTNode rhsNode;
                if (match.associativity == OperatorModel.Associativity.Right)
                    rhsNode = this.ParseExpression();
                else
                    rhsNode = this.ParseBinaryOp(level + 1);

                node.AddChild(lhsNode);
                node.AddChild(rhsNode);
                node.SetSpan(lhsNode.SpanWithDelimiters().Merge(rhsNode.SpanWithDelimiters()));

                // In a right-associative operator, return the current binary op node.
                if (match.associativity == OperatorModel.Associativity.Right)
                    return node;

                // In a left-associative operator, set the current binary op node
                // as the left-hand side for the next iteration.
                lhsNode = node;
            }
        }


        private ASTNode ParseUnaryOp(int level)
        {
            // If reached the end of operators list, continue parsing inner expressions.
            if (level >= unaryOpList.GetLength(0))
                return this.ParseCallExpression();

            // Find a unary operator that matches the current token.
            var match = unaryOpList[level].Find(op => this.CurrentIs(op.tokenKind));

            // If no operator matched, parse a inner expression.
            if (match == null)
                return this.ParseUnaryOp(level + 1);

            // Prepare the node.
            var node = new ASTNode(ASTNodeKind.UnaryOp);
            node.AddChild(new ASTNode(ASTNodeKind.Operator, this.Current().span));
            node.SetLastChildSpan();
            this.Advance();

            // Parse the operand.
            node.AddChild(this.ParseUnaryOp(level));
            node.AddLastChildSpan();

            return node;
        }


        private ASTNode ParseCallExpression()
        {
            var targetNode = this.ParseLeafExpression();
            if (this.CurrentIsNot(TokenKind.ParenOpen))
                return targetNode;

            this.Advance();

            var callNode = new ASTNode(ASTNodeKind.Call);
            callNode.AddChild(targetNode);
            callNode.SetLastChildSpan();

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


        private ASTNode ParseLeafExpression()
        {
            if (this.CurrentIs(TokenKind.Identifier))
                return this.ParseNameWithParameterPattern(MessageCode.Internal, "expected identifier");
            else if (this.CurrentIs(TokenKind.Number))
                return this.ParseNumberLiteral();
            else if (this.CurrentIs(TokenKind.BraceOpen))
                return this.ParseBlock();
            else if (this.CurrentIs(TokenKind.ParenOpen))
            {
                var parenOpenSpan = this.Advance().span;
                var node = this.ParseExpression();
                node.AddSpanWithDelimiters(parenOpenSpan.Merge(this.Current().span));
                this.Match(TokenKind.ParenClose, MessageCode.Expected, "expected ')'");
                return node;
            }
            else
                throw this.FatalBefore(MessageCode.Expected, "expected expression");
        }

        #endregion
    }
}
