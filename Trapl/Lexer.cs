using System;
using System.Collections.Generic;
using System.Text;
using Trapl.Diagnostics;


namespace Trapl.Lexer
{
    public enum TokenKind
    {
        Error,
        Identifier, Number,
        KeywordFunct, KeywordStruct, KeywordTrait,
        KeywordLet, KeywordIf, KeywordElse, KeywordWhile, KeywordReturn,
        BraceOpen, BraceClose, ParenOpen, ParenClose,
        Period, Comma, Colon, Semicolon, Arrow,
        Equal, Plus, Minus, Asterisk, Slash,
        ExclamationMark, ExclamationMarkEqual, QuestionMark,
        LessThan, LessThanEqual, GreaterThan, GreaterThanEqual
    }


    public class Token
    {
        public TokenKind kind;
        public Diagnostics.Span span;


        public Token(TokenKind kind, Diagnostics.Span span)
        {
            this.kind = kind;
            this.span = span;
        }
    }


    public class Output
    {
        public List<Token> tokens = new List<Token>();
        public Token tokenAfterEnd;


        public Token this[int index]
        {
            get
            {
                if (index < 0)
                    return new Token(TokenKind.Error, new Diagnostics.Span());

                if (index >= tokens.Count)
                    return tokenAfterEnd;

                return tokens[index];
            }
        }


        public void PrintDebug(Source src)
        {
            foreach (var token in this.tokens)
            {
                Console.Out.Write(token.span.start.ToString().PadLeft(4));
                Console.Out.Write(" ");
                Console.Out.Write(token.span.Length().ToString().PadLeft(2));
                Console.Out.Write(" ");
                Console.Out.Write(
                    System.Enum.GetName(typeof(TokenKind), token.kind).PadRight(14));
                Console.Out.Write(" ");
                Console.Out.Write(src.Excerpt(token.span));
                Console.Out.WriteLine();
            }
        }
    }


    public class Analyzer
    {
        public static Output Pass(Source src, Diagnostics.MessageList diagn)
        {
            var output = new Output();

            // Iterate through all characters in input.
            var index = 0;
            while (index < src.Length())
            {
                // Skip whitespace.
                if (IsWhitespace(src[index]))
                {
                    index++;
                    continue;
                }

                // Match next characters to a token, and add it to the output.
                var match =
                    TryMatchModelToken(src, index) ??
                    TryMatchVaryingToken(src, index) ??
                    new TokenMatch(new string(src[index], 1), TokenKind.Error);

                var span = new Span(index, index + match.representation.Length);

                if (match.kind == TokenKind.Error)
                    diagn.AddError("unexpected character", src, MessageCaret.Primary(span));

                output.tokens.Add(new Token(match.kind, span));

                index += match.representation.Length;
            }

            output.tokenAfterEnd =
                new Token(TokenKind.Error, new Diagnostics.Span(index, index));

            return output;
        }


        private class TokenMatch
        {
            public string representation;
            public TokenKind kind;


            public TokenMatch(string representation, TokenKind kind)
            {
                this.representation = representation;
                this.kind = kind;
            }
        }


        private static TokenMatch TryMatchModelToken(Source src, int index)
        {
            var models = new List<TokenMatch>
            {
                new TokenMatch("{", TokenKind.BraceOpen),
                new TokenMatch("}", TokenKind.BraceClose),
                new TokenMatch("(", TokenKind.ParenOpen),
                new TokenMatch(")", TokenKind.ParenClose),
                new TokenMatch(".", TokenKind.Period),
                new TokenMatch(",", TokenKind.Comma),
                new TokenMatch(":", TokenKind.Colon),
                new TokenMatch(";", TokenKind.Semicolon),
                new TokenMatch("->", TokenKind.Arrow),
                new TokenMatch("=", TokenKind.Equal),
                new TokenMatch("+", TokenKind.Plus),
                new TokenMatch("-", TokenKind.Minus),
                new TokenMatch("*", TokenKind.Asterisk),
                new TokenMatch("/", TokenKind.Slash),
                new TokenMatch("!=", TokenKind.ExclamationMarkEqual),
                new TokenMatch("!", TokenKind.ExclamationMark),
                new TokenMatch("?", TokenKind.QuestionMark),
                new TokenMatch("<=", TokenKind.LessThanEqual),
                new TokenMatch("<", TokenKind.LessThan),
                new TokenMatch(">=", TokenKind.GreaterThanEqual),
                new TokenMatch(">", TokenKind.GreaterThan),
                new TokenMatch("funct", TokenKind.KeywordFunct),
                new TokenMatch("struct", TokenKind.KeywordStruct),
                new TokenMatch("trait", TokenKind.KeywordTrait),
                new TokenMatch("let", TokenKind.KeywordLet),
                new TokenMatch("if", TokenKind.KeywordIf),
                new TokenMatch("else", TokenKind.KeywordElse),
                new TokenMatch("while", TokenKind.KeywordWhile),
                new TokenMatch("return", TokenKind.KeywordReturn),
            };

            // Check all model tokens for matches to the next input.
            foreach (var model in models)
            {
                // Check if all characters in the model's representation match
                // the next input characters.
                var matched = true;
                for (int i = 0; i < model.representation.Length; i++)
                {
                    if (index + i >= src.Length())
                    {
                        matched = false;
                        break;
                    }

                    if (src[index + i] != model.representation[i])
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched)
                    return model;
            }

            return null;
        }


        private static TokenMatch TryMatchVaryingToken(Source src, int index)
        {
            // Check for alphabetic identifiers.
            if (IsIdentifierBeginning(src[index]))
            {
                var identifier = new StringBuilder();
                identifier.Append(src[index]);
                index++;

                while (index < src.Length() && IsIdentifier(src[index]))
                {
                    identifier.Append(src[index]);
                    index++;
                }

                return new TokenMatch(identifier.ToString(), TokenKind.Identifier);
            }
            // Check for number literals.
            else if (IsNumberBeginning(src[index]))
            {
                var number = new StringBuilder();
                number.Append(src[index]);
                index++;

                while (index < src.Length() && IsNumber(src[index]))
                {
                    number.Append(src[index]);
                    index++;
                }

                return new TokenMatch(number.ToString(), TokenKind.Number);
            }

            return null;
        }


        private static bool IsWhitespace(char c)
        {
            return
                (c >= 0 && c <= ' ');
        }


        private static bool IsIdentifierBeginning(char c)
        {
            return
                (c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z') ||
                (c == '_');
        }


        private static bool IsIdentifier(char c)
        {
            return
                IsIdentifierBeginning(c) ||
                (c >= '0' && c <= '9');
        }


        private static bool IsNumberBeginning(char c)
        {
            return
                (c >= '0' && c <= '9');
        }


        private static bool IsNumber(char c)
        {
            return
                IsNumberBeginning(c) ||
                (c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z') ||
                (c == '_') ||
                (c == '.');
        }
    }
}