﻿using System.Collections.Generic;
using System.Text;
using Trapl.Diagnostics;


namespace Trapl.Grammar
{
    public class Tokenizer
    {
        public static TokenCollection Tokenize(Interface.Session session, Interface.SourceCode src)
        {
            var output = new TokenCollection();

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

                var span = new Span(src, index, index + match.representation.Length);

                if (match.kind == TokenKind.Error)
                    session.diagn.Add(MessageKind.Error, MessageCode.UnexpectedChar, "unexpected character", span);

                // Skip line comments.
                if (match.kind == TokenKind.DoubleHash)
                {
                    while (index < src.Length() && src[index] != '\n')
                        index++;
                    continue;
                }

                output.tokens.Add(new Token(match.kind, span));

                index += match.representation.Length;
            }

            output.tokenAfterEnd =
                new Token(TokenKind.Error, new Diagnostics.Span(src, index, index));

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


        private static TokenMatch TryMatchModelToken(Interface.SourceCode src, int index)
        {
            var models = new List<TokenMatch>
            {
                new TokenMatch("{", TokenKind.BraceOpen),
                new TokenMatch("}", TokenKind.BraceClose),
                new TokenMatch("(", TokenKind.ParenOpen),
                new TokenMatch(")", TokenKind.ParenClose),
                new TokenMatch("...", TokenKind.TriplePeriod),
                new TokenMatch(".", TokenKind.Period),
                new TokenMatch(",", TokenKind.Comma),
                new TokenMatch("::", TokenKind.DoubleColon),
                new TokenMatch(":", TokenKind.Colon),
                new TokenMatch(";", TokenKind.Semicolon),
                new TokenMatch("->", TokenKind.Arrow),
                new TokenMatch("=", TokenKind.Equal),
                new TokenMatch("+", TokenKind.Plus),
                new TokenMatch("-", TokenKind.Minus),
                new TokenMatch("*", TokenKind.Asterisk),
                new TokenMatch("/", TokenKind.Slash),
                new TokenMatch("%", TokenKind.PercentSign),
                new TokenMatch("!=", TokenKind.ExclamationMarkEqual),
                new TokenMatch("!", TokenKind.ExclamationMark),
                new TokenMatch("?", TokenKind.QuestionMark),
                new TokenMatch("<=", TokenKind.LessThanEqual),
                new TokenMatch("<", TokenKind.LessThan),
                new TokenMatch(">=", TokenKind.GreaterThanEqual),
                new TokenMatch(">", TokenKind.GreaterThan),
                new TokenMatch("&", TokenKind.Ampersand),
                new TokenMatch("|", TokenKind.VerticalBar),
                new TokenMatch("^", TokenKind.Circumflex),
                new TokenMatch("@", TokenKind.At),
                new TokenMatch("##", TokenKind.DoubleHash),
                new TokenMatch("funct", TokenKind.KeywordFunct),
                new TokenMatch("struct", TokenKind.KeywordStruct),
                new TokenMatch("trait", TokenKind.KeywordTrait),
                new TokenMatch("gen", TokenKind.KeywordGen),
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


        private static TokenMatch TryMatchVaryingToken(Interface.SourceCode src, int index)
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
