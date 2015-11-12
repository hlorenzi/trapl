using System;
using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Grammar
{
    public class ASTParser
    {
        public static List<ASTNode> Parse(Infrastructure.Session session, TokenCollection tokenColl)
        {
            var parser = new ASTParser(session, tokenColl);

            try { parser.ParseTopLevel(); }
            catch (ParserException) { }

            return parser.topDeclNodes;
        }

        public static ASTNode ParseName(Infrastructure.Session session, TokenCollection tokenColl)
        {
            var parser = new ASTParser(session, tokenColl);
            ASTNode result = null;

            try { result = parser.ParseName(false); }
            catch (ParserException) { }

            return result;
        }

        public static ASTNode ParseTemplateList(Infrastructure.Session session, TokenCollection tokenColl)
        {
            var parser = new ASTParser(session, tokenColl);
            ASTNode result = null;

            try { result = parser.ParseTemplateList(); }
            catch (ParserException) { }

            return result;
        }

        public static ASTNode ParseType(Infrastructure.Session session, TokenCollection tokenColl)
        {
            var parser = new ASTParser(session, tokenColl);
            ASTNode result = null;

            try { result = parser.ParseType(); }
            catch (ParserException) { }

            return result;
        }

        public static ASTNode ParseFunctDecl(Infrastructure.Session session, TokenCollection tokenColl)
        {
            var parser = new ASTParser(session, tokenColl);
            ASTNode result = null;

            try { result = parser.ParseFunctDecl(true); }
            catch (ParserException) { }

            return result;
        }


        private class ParserException : Exception
        {

        }


        private int readhead;
        private TokenCollection tokenColl;
        private List<ASTNode> topDeclNodes;
        private Infrastructure.Session session;


        private ASTParser(Infrastructure.Session session, TokenCollection tokenColl)
        {
            this.readhead = 0;
            this.tokenColl = tokenColl;
            this.topDeclNodes = new List<ASTNode>();
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
                if (this.CurrentIs(TokenKind.KeywordFn))
                    this.topDeclNodes.Add(this.ParseFunctDecl(true));

                else if (this.CurrentIs(TokenKind.KeywordStruct))
                    this.topDeclNodes.Add(this.ParseStructDecl());

                else if (this.CurrentIs(TokenKind.KeywordTrait))
                    this.topDeclNodes.Add(this.ParseTraitDecl());

                else
                    throw this.FatalBefore(MessageCode.Expected, "expected 'struct', 'fn', or 'trait'");
            }
        }


        private ASTNode ParseFunctDecl(bool withBody)
        {
            var topNode = new ASTNode(ASTNodeKind.TopLevelDecl);
            topNode.AddSpan(this.Current().span);
            this.Match(TokenKind.KeywordFn, MessageCode.Expected, "expected 'fn'");
            topNode.AddChild(this.ParseName(false));

            var fnNode = new ASTNode(ASTNodeKind.FunctDecl);
            fnNode.AddSpan(this.Current().span);
            this.Match(TokenKind.ParenOpen, MessageCode.Expected, "expected '('");
            while (this.CurrentIsNot(TokenKind.ParenClose))
            {
                var argNode = new ASTNode(ASTNodeKind.FunctArg);
                argNode.AddChild(this.ParseName(false));
                this.Match(TokenKind.Colon, MessageCode.Expected, "expected ':'");
                argNode.AddChild(this.ParseType());
                fnNode.AddChild(argNode);
                if (this.Current().kind == TokenKind.Comma)
                    this.Advance();
                else if (this.Current().kind != TokenKind.ParenClose)
                    throw this.FatalAfterPrevious(MessageCode.Expected, "expected ',' or ')'");
            }
            this.Match(TokenKind.ParenClose, MessageCode.Expected, "expected ')'");

            if (this.CurrentIs(TokenKind.Arrow))
            {
                this.Advance();
                var retNode = new ASTNode(ASTNodeKind.FunctReturnType);
                retNode.AddChild(this.ParseType());
                fnNode.AddChild(retNode);
            }

            if (withBody)
            {
                var bodyNode = new ASTNode(ASTNodeKind.FunctBody);
                bodyNode.AddChild(this.ParseBlock());
                fnNode.AddChild(bodyNode);
            }

            topNode.AddChild(fnNode);
            return topNode;
        }


        private ASTNode ParseStructDecl()
        {
            var topNode = new ASTNode(ASTNodeKind.TopLevelDecl);
            topNode.AddSpan(this.Current().span);
            this.Match(TokenKind.KeywordStruct, MessageCode.Expected, "expected 'struct'");
            topNode.AddChild(this.ParseName(false));

            var structNode = new ASTNode(ASTNodeKind.StructDecl);
            structNode.AddSpan(this.Current().span);
            this.Match(TokenKind.BraceOpen, MessageCode.Expected, "expected '{'");
            while (this.CurrentIsNot(TokenKind.BraceClose))
            {
                var memberNode = new ASTNode(ASTNodeKind.StructField);
                memberNode.AddChild(this.ParseName(false));
                this.Match(TokenKind.Colon, MessageCode.Expected, "expected ':'");
                memberNode.AddChild(this.ParseType());
                structNode.AddChild(memberNode);
                this.MatchListSeparator(TokenKind.Comma, TokenKind.BraceClose,
                    MessageCode.Expected, "expected ',' or '}'");
            }
            structNode.AddSpan(this.Current().span);
            this.Match(TokenKind.BraceClose, MessageCode.Expected, "expected '}'");

            topNode.AddChild(structNode);
            return topNode;
        }


        private ASTNode ParseTraitDecl()
        {
            var topNode = new ASTNode(ASTNodeKind.TopLevelDecl);
            topNode.AddSpan(this.Current().span);
            this.Match(TokenKind.KeywordTrait, MessageCode.Expected, "expected 'trait'");
            topNode.AddChild(this.ParseName(false));

            var traitNode = new ASTNode(ASTNodeKind.TraitDecl);
            traitNode.AddSpan(this.Current().span);
            this.Match(TokenKind.BraceOpen, MessageCode.Expected, "expected '{'");
            while (this.CurrentIsNot(TokenKind.BraceClose))
            {
                var fnNode = this.ParseFunctDecl(false);
                fnNode.kind = ASTNodeKind.TraitFn;
                traitNode.AddChild(fnNode);

                this.MatchListSeparator(TokenKind.Comma, TokenKind.BraceClose,
                    MessageCode.Expected, "expected ',' or '}'");
            }
            traitNode.AddSpan(this.Current().span);
            this.Match(TokenKind.BraceClose, MessageCode.Expected, "expected '}'");

            topNode.AddChild(traitNode);
            return topNode;
        }


        private ASTNode ParseName(bool explicitPattern)
        {
            var node = new ASTNode(ASTNodeKind.Name);
            node.AddChild(this.ParsePath());

            if ((!explicitPattern && this.CurrentIs(TokenKind.LessThan)) ||
                (this.CurrentIs(TokenKind.DoubleColon) && this.NextIs(TokenKind.LessThan)))
            {
                if (this.CurrentIs(TokenKind.DoubleColon))
                    this.Advance();

                node.AddChild(this.ParseTemplateList());
            }

            return node;
        }


        private ASTNode ParsePath()
        {
            var node = new ASTNode(ASTNodeKind.Path);
            node.AddChild(ParseIdentifier());
            while (this.CurrentIs(TokenKind.DoubleColon) && this.NextIs(TokenKind.Identifier))
            {
                this.Advance();
                node.AddChild(ParseIdentifier());
            }
            return node;
        }


        private ASTNode ParseIdentifier()
        {
            return new ASTNode(ASTNodeKind.Identifier,
                this.Match(TokenKind.Identifier, MessageCode.Expected, "expected identifier").span);
        }


        public ASTNode ParseTemplateList()
        {
            var node = new ASTNode(ASTNodeKind.TemplateList);
            node.AddSpan(this.Current().span);
            this.Match(TokenKind.LessThan, MessageCode.Expected, "expected '<'");
            while (this.CurrentIsNot(TokenKind.GreaterThan))
            {
                node.AddChild(this.ParseTemplateParameter());

                if (this.CurrentIs(TokenKind.TriplePeriod))
                {
                    this.Advance();
                    this.MatchListSeparator(TokenKind.Comma, TokenKind.GreaterThan,
                        MessageCode.Expected, "expected ',' or '>'");
                    node.kind = ASTNodeKind.TemplateVariadicList;
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


        public ASTNode ParseTemplateParameter()
        {
            var node = new ASTNode(ASTNodeKind.TemplateParameter);
            node.AddChild(this.ParseType());
            return node;
        }


        private ASTNode ParseType()
        {
            var node = new ASTNode(ASTNodeKind.Type);
            node.AddSpan(this.Current().span);

            if (this.CurrentIs(TokenKind.KeywordGen))
            {
                node.kind = ASTNodeKind.GenericTypeParameter;
                this.Advance();
            }

            var modifierNodes = new List<ASTNode>();

            while (this.CurrentIs(TokenKind.Ampersand))
            {
                modifierNodes.Add(new ASTNode(ASTNodeKind.Operator, this.Advance().span));
            }

            // Parse a tuple type.
            if (this.CurrentIs(TokenKind.ParenOpen))
            {
                node.kind = ASTNodeKind.TupleType;
                this.Advance();
                while (this.CurrentIsNot(TokenKind.ParenClose))
                {
                    node.AddChild(this.ParseType());
                    this.MatchListSeparator(TokenKind.Comma, TokenKind.ParenClose, MessageCode.Expected, "expected ',' or ')'");
                }
                node.AddSpan(this.Current().span);
                this.Match(TokenKind.ParenClose, MessageCode.Expected, "expected ')'");
            }
            // Parse a struct type.
            else
            {
                node.AddChild(this.ParseName(false));

                foreach (var mod in modifierNodes)
                    node.AddChild(mod);
            }

            return node;
        }


        private ASTNode ParseBooleanLiteral()
        {
            var token = this.Advance();
            return new ASTNode(ASTNodeKind.BooleanLiteral, token.span);
        }


        private ASTNode ParseNumberLiteral()
        {
            var token = this.Match(TokenKind.Number, MessageCode.Expected, "expected number");
            return new ASTNode(ASTNodeKind.NumberLiteral, token.span);
        }


        private ASTNode ParseBlock()
        {
            var node = new ASTNode(ASTNodeKind.Block);
            node.AddSpan(this.Current().span);
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
            node.AddChild(this.ParseExpression());
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
            node.AddChild(this.ParseExpression());
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


        private ASTNode ParseBinaryOp(int level)
        {
            // If reached the end of operators list, continue parsing inner expressions.
            if (level >= opList.GetLength(0))
                return this.ParseCallExpression();

            // Try to find a unary operator that matches the current token.
            var unaryMatch = opList[level].Find(
                op => op.associativity == OperatorModel.Associativity.Unary &&
                this.CurrentIs(op.tokenKind));

            if (unaryMatch != null)
            {
                // Prepare the unary node.
                var node = new ASTNode(ASTNodeKind.UnaryOp);
                node.AddChild(new ASTNode(ASTNodeKind.Operator, this.Current().span));
                this.Advance();

                // Parse the unary operand.
                node.AddChild(this.ParseBinaryOp(level));

                return node;
            }

            // If no unary operator matched, parse the left-hand side of a binary expression.
            var lhsNode = this.ParseBinaryOp(level + 1);

            // Infinite loop for left associativity.
            while (true)
            {
                var node = new ASTNode(ASTNodeKind.BinaryOp);

                // Find a binary operator that matches the current token.
                var binaryMatch = opList[level].Find(
                    op => op.associativity != OperatorModel.Associativity.Unary &&
                    this.CurrentIs(op.tokenKind));

                // If no operator matched, return the current left-hand side.
                if (binaryMatch == null)
                    return lhsNode;

                node.AddChild(new ASTNode(ASTNodeKind.Operator, this.Current().span));
                this.Advance();

                // Parse right-hand side. 
                ASTNode rhsNode;
                if (binaryMatch.associativity == OperatorModel.Associativity.Right)
                    rhsNode = this.ParseExpression();
                else
                    rhsNode = this.ParseBinaryOp(level + 1);

                node.AddChild(lhsNode);
                node.AddChild(rhsNode);

                // In a right-associative operator, return the current binary op node.
                if (binaryMatch.associativity == OperatorModel.Associativity.Right)
                    return node;

                // In a left-associative operator, set the current binary op node
                // as the left-hand side for the next iteration.
                lhsNode = node;
            }
        }


        private ASTNode ParseCallExpression()
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
            if (this.CurrentIsNot(TokenKind.BraceOpen))
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
        }


        private ASTNode ParseLeafExpression()
        {
            if (this.CurrentIs(TokenKind.Identifier))
                return this.ParseName(true);
            else if (this.CurrentIs(TokenKind.Number))
                return this.ParseNumberLiteral();
            else if (this.CurrentIs(TokenKind.BooleanTrue) || this.CurrentIs(TokenKind.BooleanFalse))
                return this.ParseBooleanLiteral();
            else if (this.CurrentIs(TokenKind.BraceOpen))
                return this.ParseBlock();
            else if (this.CurrentIs(TokenKind.ParenOpen))
            {
                var parenOpenSpan = this.Advance().span;
                var node = this.ParseExpression();
                node.AddSpan(parenOpenSpan.Merge(this.Current().span));
                this.Match(TokenKind.ParenClose, MessageCode.Expected, "expected ')'");
                return node;
            }
            else
                throw this.FatalBefore(MessageCode.Expected, "expected expression");
        }

        #endregion
    }
}
