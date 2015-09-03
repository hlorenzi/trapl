using System;
using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Syntax
{
    public class Analyzer
    {
        public static Output Pass(Lexer.Output lex, Source source, Diagnostics.MessageList diagn)
        {
            var analyzer = new Analyzer(lex, source, diagn);
            try
            {
                analyzer.ParseTopLevel();
            }
            catch (ParserException)
            {

            }
            return analyzer.output;
        }


        private class ParserException : Exception
        {

        }


        private int readhead;
        private Lexer.Output input;
        private Source source;
        private Output output;
        private Diagnostics.MessageList diagn;


        private Analyzer(Lexer.Output lex, Source source, Diagnostics.MessageList diagn)
        {
            this.readhead = 0;
            this.input = lex;
            this.source = source;
            this.output = new Output();
            this.diagn = diagn;
        }


        private Lexer.Token Current()
        {
            return this.input[this.readhead];
        }


        private bool CurrentIs(Lexer.TokenKind kind)
        {
            return (this.Current().kind == kind);
        }


        private bool CurrentIsNot(params Lexer.TokenKind[] kinds)
        {
            for (int i = 0; i < kinds.Length; i++)
                if (this.Current().kind == kinds[i])
                    return false;
            return (this.Current().kind != Lexer.TokenKind.Error);
        }


        private Lexer.Token Advance()
        {
            var cur = this.Current();
            this.readhead++;
            return cur;
        }


        private Lexer.Token Previous()
        {
            return this.input[this.readhead - 1];
        }


        private bool IsOver()
        {
            return (this.readhead >= this.input.tokens.Count);
        }


        private Lexer.Token Match(Lexer.TokenKind tokenKind, MessageCode errCode, string errText)
        {
            if (this.Current().kind == tokenKind)
                return this.Advance();
            else
                throw this.FatalBefore(errCode, errText);
        }


        private bool MatchListSeparator(Lexer.TokenKind separatorKind, Lexer.TokenKind endingKind, MessageCode errCode, string errText)
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
            this.diagn.Add(MessageKind.Error, errCode, errText, this.source, this.Current().span.JustBefore());
            return new ParserException();
        }


        private ParserException FatalCurrent(MessageCode errCode, string errText)
        {
            this.diagn.Add(MessageKind.Error, errCode, errText, this.source, this.Current().span);
            return new ParserException();
        }


        private ParserException FatalAfterPrevious(MessageCode errCode, string errText)
        {
            this.diagn.Add(MessageKind.Error, errCode, errText, this.source, this.Previous().span.JustAfter());
            return new ParserException();
        }








        private void ParseTopLevel()
        {
            while (!this.IsOver())
            {
                if (this.CurrentIs(Lexer.TokenKind.Identifier))
                {
                    var node = new Node(NodeKind.TopLevelDecl);
                    node.SetSpan(this.Current().span);
                    node.AddChild(this.ParseTemplatedIdentifier(MessageCode.Expected, "expected declaration name"));
                    this.Match(Lexer.TokenKind.Colon, MessageCode.Expected, "expected ':'");
                    if (this.CurrentIs(Lexer.TokenKind.KeywordFunct))
                        node.AddChild(this.ParseFunctDecl(true));
                    else if (this.CurrentIs(Lexer.TokenKind.KeywordStruct))
                        node.AddChild(this.ParseStructDecl());
                    else if (this.CurrentIs(Lexer.TokenKind.KeywordTrait))
                        node.AddChild(this.ParseTraitDecl());
                    else
                        throw this.FatalBefore(MessageCode.Expected, "expected 'funct', 'struct' or 'trait'");
                    node.AddLastChildSpan();
                    this.output.topDecls.Add(node);
                }
                else
                    throw this.FatalBefore(MessageCode.Expected, "expected a declaration");
            }
        }


        private Node ParseFunctDecl(bool withBody)
        {
            var node = new Node(NodeKind.FunctDecl);
            node.SetSpan(this.Current().span);
            this.Match(Lexer.TokenKind.KeywordFunct, MessageCode.Expected, "expected 'funct'");
            this.Match(Lexer.TokenKind.ParenOpen, MessageCode.Expected, "expected '('");
            while (this.CurrentIsNot(Lexer.TokenKind.ParenClose, Lexer.TokenKind.Arrow))
            {
                var argNode = new Node(NodeKind.FunctArgDecl);
                argNode.AddChild(this.ParseIdentifier(MessageCode.Expected, "expected argument name"));
                argNode.SetLastChildSpan();
                this.Match(Lexer.TokenKind.Colon, MessageCode.Expected, "expected ':'");
                argNode.AddChild(this.ParseType());
                argNode.AddLastChildSpan();
                node.AddChild(argNode);
                node.AddLastChildSpan();
                if (this.Current().kind == Lexer.TokenKind.Comma)
                    this.Advance();
                else if (this.Current().kind != Lexer.TokenKind.ParenClose &&
                    this.Current().kind != Lexer.TokenKind.Arrow)
                    throw this.FatalAfterPrevious(MessageCode.Expected, "expected ',', '->' or ')'");
            }
            if (this.CurrentIs(Lexer.TokenKind.Arrow))
            {
                this.Advance();
                var retNode = new Node(NodeKind.FunctReturnDecl);
                retNode.AddChild(this.ParseType());
                retNode.SetLastChildSpan();
                node.AddChild(retNode);
                node.AddLastChildSpan();
            }
            this.Match(Lexer.TokenKind.ParenClose, MessageCode.Expected, "expected ')'");
            if (withBody)
            {
                node.AddChild(this.ParseBlock());
                node.AddLastChildSpan();
            }
            return node;
        }


        private Node ParseStructDecl()
        {
            var node = new Node(NodeKind.StructDecl);
            node.SetSpan(this.Current().span);
            this.Match(Lexer.TokenKind.KeywordStruct, MessageCode.Expected, "expected 'struct'");
            this.Match(Lexer.TokenKind.BraceOpen, MessageCode.Expected, "expected '{'");
            while (this.CurrentIsNot(Lexer.TokenKind.BraceClose))
            {
                var memberNode = new Node(NodeKind.StructMemberDecl);
                memberNode.AddChild(this.ParseIdentifier(MessageCode.Expected, "expected member name"));
                memberNode.SetLastChildSpan();
                this.Match(Lexer.TokenKind.Colon, MessageCode.Expected, "expected ':'");
                memberNode.AddChild(this.ParseType());
                memberNode.AddLastChildSpan();
                node.AddChild(memberNode);
                this.MatchListSeparator(Lexer.TokenKind.Comma, Lexer.TokenKind.BraceClose,
                    MessageCode.Expected, "expected ',' or '}'");
            }
            node.AddSpan(this.Current().span);
            this.Match(Lexer.TokenKind.BraceClose, MessageCode.Expected, "expected '}'");
            return node;
        }


        private Node ParseTraitDecl()
        {
            var node = new Node(NodeKind.TraitDecl);
            node.SetSpan(this.Current().span);
            this.Match(Lexer.TokenKind.KeywordTrait, MessageCode.Expected, "expected 'trait'");
            this.Match(Lexer.TokenKind.BraceOpen, MessageCode.Expected, "expected '{'");
            while (this.CurrentIsNot(Lexer.TokenKind.BraceClose))
            {
                var memberNode = new Node(NodeKind.TraitMemberDecl);
                memberNode.AddChild(this.ParseIdentifier(MessageCode.Expected, "expected funct name"));
                memberNode.SetLastChildSpan();
                this.Match(Lexer.TokenKind.Colon, MessageCode.Expected, "expected ':'");
                memberNode.AddChild(this.ParseFunctDecl(false));
                memberNode.AddLastChildSpan();
                node.AddChild(memberNode);
                this.MatchListSeparator(Lexer.TokenKind.Semicolon, Lexer.TokenKind.BraceClose,
                    MessageCode.Expected, "expected ';' or '}'");
            }
            node.AddSpan(this.Current().span);
            this.Match(Lexer.TokenKind.BraceClose, MessageCode.Expected, "expected '}'");
            return node;
        }

        private Node ParseName(MessageCode errCode, string errText)
        {
            var nameToken = this.Match(Lexer.TokenKind.Identifier, errCode, errText);
            return new Node(NodeKind.Name, nameToken.span);
        }


        private Node ParseIdentifier(MessageCode errCode, string errText)
        {
            var node = new Node(NodeKind.Identifier);
            node.AddChild(this.ParseName(errCode, errText));
            node.SetLastChildSpan();
            return node;
        }


        private Node ParseTemplatedIdentifier(MessageCode errCode, string errText)
        {
            var node = new Node(NodeKind.Identifier);
            node.SetSpan(this.Current().span);
            var nameToken = this.Match(Lexer.TokenKind.Identifier, errCode, errText);
            node.AddChild(new Node(NodeKind.Name, nameToken.span));
            node.AddLastChildSpan();

            if (this.CurrentIs(Lexer.TokenKind.DoubleColon))
            {
                this.Advance();
                node.AddChild(this.ParseTemplateList());
                node.AddLastChildSpan();
            }

            return node;
        }


        private Node ParseTemplateList()
        {
            var node = new Node(NodeKind.TemplateList);
            node.SetSpan(this.Current().span);
            this.Match(Lexer.TokenKind.LessThan, MessageCode.Expected, "expected '<'");
            while (this.CurrentIsNot(Lexer.TokenKind.GreaterThan))
            {
                node.AddChild(this.ParseType());
                this.MatchListSeparator(Lexer.TokenKind.Comma, Lexer.TokenKind.GreaterThan,
                    MessageCode.Expected, "expected ',' or '>'");
            }
            node.AddSpan(this.Current().span);
            this.Match(Lexer.TokenKind.GreaterThan, MessageCode.Expected, "expected '>'");
            return node;
        }


        private Node ParseNumberLiteral()
        {
            var token = this.Match(Lexer.TokenKind.Number, MessageCode.Expected, "expected number");
            return new Node(NodeKind.NumberLiteral, token.span);
        }


        private Node ParseType()
        {
            var node = new Node(NodeKind.TypeName);
            node.SetSpan(this.Current().span);
            while (this.CurrentIs(Lexer.TokenKind.Ampersand))
                node.AddChild(new Node(NodeKind.Operator, this.Advance().span));
            node.AddChild(new Node(NodeKind.Identifier,
                this.Match(Lexer.TokenKind.Identifier, MessageCode.Expected, "expected type").span));
            node.AddLastChildSpan();
            return node;
        }


        private Node ParseBlock()
        {
            var node = new Node(NodeKind.Block);
            node.SetSpan(this.Current().span);
            this.Match(Lexer.TokenKind.BraceOpen, MessageCode.Expected, "expected '{'");
            while (this.CurrentIsNot(Lexer.TokenKind.BraceClose))
            {
                node.AddChild(ParseExpression());
                this.MatchListSeparator(Lexer.TokenKind.Semicolon, Lexer.TokenKind.BraceClose,
                    MessageCode.Expected, "expected ';' or '}'");
            }
            node.AddSpan(this.Current().span);
            this.Match(Lexer.TokenKind.BraceClose, MessageCode.Expected, "expected '}'");
            return node;
        }


        private Node ParseExpression()
        {
            if (this.CurrentIs(Lexer.TokenKind.KeywordLet))
                return this.ParseLetExpression();
            else if (this.CurrentIs(Lexer.TokenKind.KeywordIf))
                return this.ParseIfExpression();
            else if (this.CurrentIs(Lexer.TokenKind.KeywordElse))
                throw this.FatalCurrent(MessageCode.UnmatchedElse, "unmatched 'else'");
            else if (this.CurrentIs(Lexer.TokenKind.KeywordWhile))
                return this.ParseWhileExpression();
            else if (this.CurrentIs(Lexer.TokenKind.KeywordReturn))
                return this.ParseReturnExpression();
            else
                return this.ParseBinaryOp(0);
        }


        private Node ParseLetExpression()
        {
            var node = new Node(NodeKind.ControlLet);
            node.SetSpan(this.Current().span);
            this.Match(Lexer.TokenKind.KeywordLet, MessageCode.Expected, "expected 'let'");
            node.AddChild(this.ParseName(MessageCode.Expected, "expected variable name"));
            if (this.CurrentIs(Lexer.TokenKind.Colon))
            {
                this.Advance();
                node.AddChild(this.ParseType());
            }
            if (this.CurrentIs(Lexer.TokenKind.Equal))
            {
                this.Advance();
                node.AddChild(this.ParseExpression());
            }
            node.AddLastChildSpan();
            return node;
        }


        private Node ParseIfExpression()
        {
            var node = new Node(NodeKind.ControlIf);
            node.SetSpan(this.Current().span);
            this.Match(Lexer.TokenKind.KeywordIf, MessageCode.Expected, "expected 'if'");
            node.AddChild(this.ParseExpression());
            node.AddChild(this.ParseBlock());
            if (this.CurrentIs(Lexer.TokenKind.KeywordElse))
            {
                this.Advance();
                node.AddChild(this.ParseBlock());
            }
            node.AddLastChildSpan();
            return node;
        }


        private Node ParseWhileExpression()
        {
            var node = new Node(NodeKind.ControlWhile);
            node.SetSpan(this.Current().span);
            this.Match(Lexer.TokenKind.KeywordWhile, MessageCode.Expected, "expected 'while'");
            node.AddChild(this.ParseExpression());
            node.AddChild(this.ParseBlock());
            node.AddLastChildSpan();
            return node;
        }


        private Node ParseReturnExpression()
        {
            var node = new Node(NodeKind.ControlReturn);
            node.SetSpan(this.Current().span);
            this.Match(Lexer.TokenKind.KeywordReturn, MessageCode.Expected, "expected 'return'");
            if (!this.CurrentIs(Lexer.TokenKind.Semicolon) &&
                !this.CurrentIs(Lexer.TokenKind.BraceClose) &&
                !this.CurrentIs(Lexer.TokenKind.ParenClose))
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
            public Lexer.TokenKind tokenKind;


            public OperatorModel(Associativity assoc, Lexer.TokenKind tokenKind)
            {
                this.associativity = assoc;
                this.tokenKind = tokenKind;
            }
        }


        private static readonly List<OperatorModel>[] binaryOpList = new List<OperatorModel>[]
        {
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Right, Lexer.TokenKind.Equal)
            },
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Left, Lexer.TokenKind.Plus),
                new OperatorModel(OperatorModel.Associativity.Left, Lexer.TokenKind.Minus)
            },
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Left, Lexer.TokenKind.Asterisk),
                new OperatorModel(OperatorModel.Associativity.Left, Lexer.TokenKind.Slash)
            },
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Left, Lexer.TokenKind.Period)
            }
        };


        private static readonly List<OperatorModel>[] unaryOpList = new List<OperatorModel>[]
        {
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Left, Lexer.TokenKind.Plus),
                new OperatorModel(OperatorModel.Associativity.Left, Lexer.TokenKind.Minus)
            },
            new List<OperatorModel> {
                new OperatorModel(OperatorModel.Associativity.Left, Lexer.TokenKind.At),
                new OperatorModel(OperatorModel.Associativity.Left, Lexer.TokenKind.Ampersand)
            }
        };


        private Node ParseBinaryOp(int level)
        {
            // If reached the end of operators list, continue parsing inner expressions.
            if (level >= binaryOpList.GetLength(0))
                return this.ParseUnaryOp(0);

            // Parse left-hand side.
            var lhsNode = this.ParseBinaryOp(level + 1);

            // Infinite loop for left associativity.
            while (true)
            {
                var node = new Node(NodeKind.BinaryOp);

                // Find a binary operator that matches the current token.
                var match = binaryOpList[level].Find(op => this.CurrentIs(op.tokenKind));

                // If no operator matched, return the current left-hand side.
                if (match == null)
                    return lhsNode;

                node.AddChild(new Node(NodeKind.Operator, this.Current().span));
                this.Advance();

                // Parse right-hand side. 
                Node rhsNode;
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


        private Node ParseUnaryOp(int level)
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
            var node = new Node(NodeKind.UnaryOp);
            node.AddChild(new Node(NodeKind.Operator, this.Current().span));
            node.SetLastChildSpan();
            this.Advance();

            // Parse the operand.
            node.AddChild(this.ParseUnaryOp(level));
            node.AddLastChildSpan();

            return node;
        }


        private Node ParseCallExpression()
        {
            var targetNode = this.ParseLeafExpression();
            if (this.CurrentIsNot(Lexer.TokenKind.ParenOpen))
                return targetNode;

            this.Advance();

            var callNode = new Node(NodeKind.Call);
            callNode.AddChild(targetNode);
            callNode.SetLastChildSpan();

            while (this.CurrentIsNot(Lexer.TokenKind.ParenClose))
            {
                callNode.AddChild(this.ParseExpression());
                this.MatchListSeparator(Lexer.TokenKind.Comma, Lexer.TokenKind.ParenClose,
                    MessageCode.Expected, "expected ',' or ')'");
            }

            callNode.AddSpan(this.Current().span);
            this.Match(Lexer.TokenKind.ParenClose, MessageCode.Expected, "expected ')'");

            return callNode;
        }


        private Node ParseLeafExpression()
        {
            if (this.CurrentIs(Lexer.TokenKind.Identifier))
                return this.ParseTemplatedIdentifier(MessageCode.Internal, "expected identifier");
            else if (this.CurrentIs(Lexer.TokenKind.Number))
                return this.ParseNumberLiteral();
            else if (this.CurrentIs(Lexer.TokenKind.BraceOpen))
                return this.ParseBlock();
            else if (this.CurrentIs(Lexer.TokenKind.ParenOpen))
            {
                var parenOpenSpan = this.Advance().span;
                var node = this.ParseExpression();
                node.AddSpanWithDelimiters(parenOpenSpan.Merge(this.Current().span));
                this.Match(Lexer.TokenKind.ParenClose, MessageCode.Expected, "expected ')'");
                return node;
            }
            else
                throw this.FatalBefore(MessageCode.Expected, "expected expression");
        }
    }
}
