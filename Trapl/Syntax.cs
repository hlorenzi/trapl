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


        private Lexer.Token Match(Lexer.TokenKind tokenKind, string err)
        {
            if (this.Current().kind == tokenKind)
                return this.Advance();
            else
                throw this.FatalBefore(err);
        }


        private bool MatchListSeparator(Lexer.TokenKind separatorKind, Lexer.TokenKind endingKind, string err)
        {
            if (this.Current().kind == separatorKind)
            {
                this.Advance();
                return false;
            }
            else if (this.Current().kind == endingKind)
                return true;
            else
                throw this.FatalAfterPrevious(err);
        }


        private ParserException FatalBefore(string err)
        {
            this.diagn.AddError(err, this.source, MessageCaret.Primary(this.Current().span.JustBefore()));
            return new ParserException();
        }


        private ParserException FatalCurrent(string err)
        {
            this.diagn.AddError(err, this.source, MessageCaret.Primary(this.Current().span));
            return new ParserException();
        }


        private ParserException FatalAfterPrevious(string err)
        {
            this.diagn.AddError(err, this.source, MessageCaret.Primary(this.Previous().span.JustAfter()));
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
                    node.AddChild(this.ParseIdentifier());
                    this.Match(Lexer.TokenKind.Colon, "expecting ':'");
                    if (this.CurrentIs(Lexer.TokenKind.KeywordFunct))
                        node.AddChild(this.ParseFunctDecl());
                    else if (this.CurrentIs(Lexer.TokenKind.KeywordStruct))
                        node.AddChild(this.ParseStructDecl());
                    else if (this.CurrentIs(Lexer.TokenKind.KeywordTrait))
                        node.AddChild(this.ParseTraitDecl());
                    else
                        throw this.FatalBefore("expecting a declaration");
                    node.AddLastChildSpan();
                    this.output.topDecls.Add(node);
                }
                else
                    throw this.FatalBefore("expecting a declaration");
            }
        }


        private Node ParseFunctDecl()
        {
            var node = new Node(NodeKind.FunctDecl);
            node.SetSpan(this.Current().span);
            this.Match(Lexer.TokenKind.KeywordFunct, "expecting 'funct'");
            this.Match(Lexer.TokenKind.ParenOpen, "expecting '('");
            while (!this.CurrentIs(Lexer.TokenKind.ParenClose))
            {
                var argNode = new Node(NodeKind.FunctArgDecl);
                argNode.AddChild(this.ParseIdentifier("expecting argument name"));
                argNode.SetLastChildSpan();
                this.Match(Lexer.TokenKind.Colon, "expecting ':'");
                argNode.AddChild(this.ParseType());
                argNode.AddLastChildSpan();
                node.AddChild(argNode);
                node.AddLastChildSpan();
                this.MatchListSeparator(Lexer.TokenKind.Comma, Lexer.TokenKind.ParenClose, "expecting ',' or ')'");
            }
            this.Match(Lexer.TokenKind.ParenClose, "expecting ')'");
            if (this.CurrentIs(Lexer.TokenKind.BraceOpen))
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
            this.Match(Lexer.TokenKind.KeywordStruct, "expecting 'struct'");
            this.Match(Lexer.TokenKind.BraceOpen, "expecting '{'");
            while (!this.CurrentIs(Lexer.TokenKind.BraceClose))
            {
                var memberNode = new Node(NodeKind.StructMemberDecl);
                memberNode.AddChild(this.ParseIdentifier("expecting member name"));
                memberNode.SetLastChildSpan();
                this.Match(Lexer.TokenKind.Colon, "expecting ':'");
                memberNode.AddChild(this.ParseType());
                memberNode.AddLastChildSpan();
                node.AddChild(memberNode);
                this.MatchListSeparator(Lexer.TokenKind.Semicolon, Lexer.TokenKind.BraceClose, "expecting ';' or '}'");
            }
            node.AddSpan(this.Current().span);
            this.Match(Lexer.TokenKind.BraceClose, "expecting '}'");
            return node;
        }


        private Node ParseTraitDecl()
        {
            var node = new Node(NodeKind.TraitDecl);
            node.SetSpan(this.Current().span);
            this.Match(Lexer.TokenKind.KeywordTrait, "expecting 'trait'");
            this.Match(Lexer.TokenKind.BraceOpen, "expecting '{'");
            while (!this.CurrentIs(Lexer.TokenKind.BraceClose))
            {
                var memberNode = new Node(NodeKind.TraitMemberDecl);
                memberNode.AddChild(this.ParseIdentifier("expecting trait funct name"));
                memberNode.SetLastChildSpan();
                this.Match(Lexer.TokenKind.Colon, "expecting ':'");
                memberNode.AddChild(this.ParseFunctDecl());
                memberNode.AddLastChildSpan();
                node.AddChild(memberNode);
                this.MatchListSeparator(Lexer.TokenKind.Semicolon, Lexer.TokenKind.BraceClose, "expecting ';' or '}'");
            }
            node.AddSpan(this.Current().span);
            this.Match(Lexer.TokenKind.BraceClose, "expecting '}'");
            return node;
        }


        private Node ParseIdentifier(string err = "expecting identifier")
        {
            var token = this.Match(Lexer.TokenKind.Identifier, err);
            return new Node(NodeKind.Identifier, token.span);
        }


        private Node ParseNumberLiteral(string err = "expecting number literal")
        {
            var token = this.Match(Lexer.TokenKind.Number, err);
            return new Node(NodeKind.NumberLiteral, token.span);
        }


        private Node ParseType()
        {
            var token = this.Match(Lexer.TokenKind.Identifier, "expecting type name");
            return new Node(NodeKind.TypeName, token.span);
        }


        private Node ParseBlock()
        {
            var node = new Node(NodeKind.Block);
            node.SetSpan(this.Current().span);
            this.Match(Lexer.TokenKind.BraceOpen, "expecting '{'");
            while (!this.CurrentIs(Lexer.TokenKind.BraceClose))
            {
                node.AddChild(ParseExpression());
                this.MatchListSeparator(Lexer.TokenKind.Semicolon, Lexer.TokenKind.BraceClose, "expecting ';' or '}'");
            }
            node.AddSpan(this.Current().span);
            this.Match(Lexer.TokenKind.BraceClose, "expecting '}'");
            return node;
        }


        private Node ParseExpression()
        {
            if (this.CurrentIs(Lexer.TokenKind.KeywordLet))
                return this.ParseLetExpression();
            else if (this.CurrentIs(Lexer.TokenKind.KeywordIf))
                return this.ParseIfExpression();
            else if (this.CurrentIs(Lexer.TokenKind.KeywordElse))
                throw this.FatalCurrent("unmatched 'else'");
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
            this.Match(Lexer.TokenKind.KeywordLet, "expecting 'let'");
            node.AddChild(this.ParseIdentifier());
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
            this.Match(Lexer.TokenKind.KeywordIf, "expecting 'if'");
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
            this.Match(Lexer.TokenKind.KeywordWhile, "expecting 'while'");
            node.AddChild(this.ParseExpression());
            node.AddChild(this.ParseBlock());
            node.AddLastChildSpan();
            return node;
        }


        private Node ParseReturnExpression()
        {
            var node = new Node(NodeKind.ControlReturn);
            node.SetSpan(this.Current().span);
            this.Match(Lexer.TokenKind.KeywordReturn, "expecting 'return'");
            if (!this.CurrentIs(Lexer.TokenKind.Semicolon) &&
                !this.CurrentIs(Lexer.TokenKind.BraceClose) &&
                !this.CurrentIs(Lexer.TokenKind.ParenClose))
            {
                node.AddChild(this.ParseExpression());
                node.AddLastChildSpan();
            }
            return node;
        }


        private class BinaryOp
        {
            public enum Associativity { Left, Right };


            public Associativity associativity;
            public Lexer.TokenKind tokenKind;


            public BinaryOp(Associativity assoc, Lexer.TokenKind tokenKind)
            {
                this.associativity = assoc;
                this.tokenKind = tokenKind;
            }
        }


        private static readonly List<BinaryOp>[] binaryOpList = new List<BinaryOp>[]
        {
            new List<BinaryOp> {
                new BinaryOp(BinaryOp.Associativity.Right, Lexer.TokenKind.Equal)
            },
            new List<BinaryOp> {
                new BinaryOp(BinaryOp.Associativity.Left, Lexer.TokenKind.Plus),
                new BinaryOp(BinaryOp.Associativity.Left, Lexer.TokenKind.Minus)
            },
            new List<BinaryOp> {
                new BinaryOp(BinaryOp.Associativity.Left, Lexer.TokenKind.Asterisk),
                new BinaryOp(BinaryOp.Associativity.Left, Lexer.TokenKind.Slash)
            },
            new List<BinaryOp> {
                new BinaryOp(BinaryOp.Associativity.Left, Lexer.TokenKind.Period)
            }
        };


        private Node ParseBinaryOp(int level)
        {
            // If reached the end of operators list, continue parsing inner expressions.
            if (level >= binaryOpList.GetLength(0))
                return this.ParseLeafExpression();

            // Parse left-hand side.
            var lhsNode = this.ParseBinaryOp(level + 1);

            // Infinite loop for left associativity.
            while (true)
            {
                var node = new Node(NodeKind.BinaryOp);

                // Find an operator that matches the current token.
                var match = binaryOpList[level].Find(op => this.CurrentIs(op.tokenKind));

                // If no operator matched, return the current left-hand side.
                if (match == null)
                    return lhsNode;

                node.AddChild(new Node(NodeKind.Operator, this.Current().span));
                this.Advance();

                // Parse right-hand side. 
                Node rhsNode;
                if (match.associativity == BinaryOp.Associativity.Right)
                    rhsNode = this.ParseExpression();
                else
                    rhsNode = this.ParseBinaryOp(level + 1);

                node.AddChild(lhsNode);
                node.AddChild(rhsNode);
                node.SetSpan(lhsNode.SpanWithDelimiters().Merge(rhsNode.SpanWithDelimiters()));

                // In a right-associative operator, return the current binary op node.
                if (match.associativity == BinaryOp.Associativity.Right)
                    return node;

                // In a left-associative operator, set the current binary op node
                // as the left-hand side for the next iteration.
                lhsNode = node;
            }
        }


        private Node ParseLeafExpression()
        {
            if (this.CurrentIs(Lexer.TokenKind.Identifier))
                return this.ParseIdentifier();
            else if (this.CurrentIs(Lexer.TokenKind.Number))
                return this.ParseNumberLiteral();
            else if (this.CurrentIs(Lexer.TokenKind.BraceOpen))
                return this.ParseBlock();
            else if (this.CurrentIs(Lexer.TokenKind.ParenOpen))
            {
                var parenOpenSpan = this.Advance().span;
                var node = this.ParseExpression();
                node.AddSpanWithDelimiters(parenOpenSpan.Merge(this.Current().span));
                this.Match(Lexer.TokenKind.ParenClose, "expecting ')'");
                return node;
            }
            else
                throw this.FatalBefore("expecting expression");
        }
    }
}
