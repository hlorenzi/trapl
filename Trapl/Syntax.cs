using System;
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


        private Lexer.Token Next()
        {
            var cur = this.Current();
            this.readhead++;
            return cur;
        }


        private bool IsOver()
        {
            return (this.readhead >= this.input.tokens.Count);
        }


        private Lexer.Token Match(Lexer.TokenKind tokenKind, string err)
        {
            if (this.Current().kind == tokenKind)
                return this.Next();
            else
            {
                this.FatalBefore(err);
                return null;
            }
        }


        private bool MatchListSeparator(Lexer.TokenKind separatorKind, Lexer.TokenKind endingKind, string err)
        {
            if (this.Current().kind == separatorKind)
            {
                this.Next();
                return false;
            }
            else if (this.Current().kind == endingKind)
            {
                return true;
            }
            else
            {
                this.FatalBefore(err);
                return true;
            }
        }


        private void FatalBefore(string err)
        {
            this.diagn.AddError(err, this.source, MessageCaret.Primary(this.Current().span.JustBefore()));
            throw new ParserException();
        }


        private void ParseTopLevel()
        {
            while (!this.IsOver())
            {
                if (this.CurrentIs(Lexer.TokenKind.Identifier))
                {
                    var identifier = ParseIdentifier();
                    this.Match(Lexer.TokenKind.Colon, "expecting ':'");
                    if (this.CurrentIs(Lexer.TokenKind.KeywordFunct))
                        this.output.topDecls.Add(ParseFunctDecl(identifier));
                }
                else
                    this.FatalBefore("expecting a top-level declaration");
            }
        }


        private Node ParseFunctDecl(Node identifierNode)
        {
            var node = new Node(NodeKind.FunctDecl);
            node.AddChild(identifierNode);
            this.Match(Lexer.TokenKind.KeywordFunct, "[internal] expecting 'funct'");
            this.Match(Lexer.TokenKind.ParenOpen, "expecting '('");
            while (true)
            {
                node.AddChild(this.ParseFunctArgument());
                if (this.MatchListSeparator(Lexer.TokenKind.Comma, Lexer.TokenKind.ParenClose, "expecting ',' or ')'"))
                    break;
            }
            this.Match(Lexer.TokenKind.ParenClose, "expecting ')'");
            return node;
        }


        private Node ParseFunctArgument()
        {
            var node = new Node(NodeKind.FunctArgDecl);
            node.AddChild(ParseIdentifier("expecting argument name"));
            this.Match(Lexer.TokenKind.Colon, "expecting ':'");
            node.AddChild(ParseType());
            return node;
        }


        private Node ParseIdentifier(string err = "expecting identifier")
        {
            var token = this.Match(Lexer.TokenKind.Identifier, err);
            return new Node(NodeKind.Identifier, token.span);
        }


        private Node ParseType()
        {
            var token = this.Match(Lexer.TokenKind.Identifier, "expecting type name");
            return new Node(NodeKind.TypeName, token.span);
        }
    }
}
