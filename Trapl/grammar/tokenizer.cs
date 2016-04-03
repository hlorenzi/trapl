using System.Collections.Generic;
using System.Text;
using Trapl.Diagnostics;


namespace Trapl.Grammar
{
    public class Tokenizer
    {
        public static TokenCollection Tokenize(Core.Session session, Core.TextInput input)
        {
            var output = new TokenCollection();

            // Iterate through all characters in input.
            var index = 0;
            while (index < input.Length())
            {
                // Skip whitespace.
                if (IsWhitespace(input[index]))
                {
                    index++;
                    continue;
                }

                // Match next characters to a token, and add it to the output.
                var match =
                    TryMatchModelToken(input, index) ??
                    TryMatchVaryingToken(input, index) ??
                    new TokenMatch(new string(input[index], 1), TokenKind.Error);

                var span = new Span(input, index, index + match.representation.Length);

                // Signal errors.
                if (match.kind == TokenKind.Error)
                {
                    session.AddMessage(MessageKind.Error, MessageCode.UnexpectedChar, "unexpected character", span);
                }
                // Skip line comments.
                else if (match.kind == TokenKind.DoubleSlash)
                {
                    while (index < input.Length() && input[index] != '\n')
                        index++;
                    continue;
                }
                // Skip multiline comments.
                else if (match.kind == TokenKind.SlashAsterisk)
                {
                    var nesting = 1;
                    index += 2;

                    while (index < input.Length() - 1)
                    {
                        if (input[index] == '*' && input[index + 1] == '/')
                        {
                            nesting--;
                            index += 2;
                            if (nesting == 0)
                                break;
                        }
                        else if (input[index] == '/' && input[index + 1] == '*')
                        {
                            nesting++;
                            index += 2;
                        }
                        else
                            index++;
                    }

                    if (nesting > 0)
                        session.AddMessage(MessageKind.Error, MessageCode.UnexpectedChar, "unterminated comment", span);

                    continue;
                }

                output.tokens.Add(new Token(match.kind, span));

                index += match.representation.Length;
            }

            output.tokenAfterEnd =
                new Token(TokenKind.Error, new Diagnostics.Span(input, index, index));

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


        private static TokenMatch TryMatchModelToken(Core.TextInput unit, int index)
        {
            var models = new List<TokenMatch>
            {
                new TokenMatch("//", TokenKind.DoubleSlash),
                new TokenMatch("/*", TokenKind.SlashAsterisk),
                new TokenMatch("*/", TokenKind.AsteriskSlash),
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
                new TokenMatch("==", TokenKind.DoubleEqual),
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
                new TokenMatch("'", TokenKind.SingleQuote)
            };

            // Check all model tokens for matches to the next input.
            foreach (var model in models)
            {
                // Check if all characters in the model's representation match
                // the next input characters.
                var matched = true;
                for (int i = 0; i < model.representation.Length; i++)
                {
                    if (index + i >= unit.Length())
                    {
                        matched = false;
                        break;
                    }

                    if (unit[index + i] != model.representation[i])
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


        private static TokenMatch TryMatchVaryingToken(Core.TextInput unit, int index)
        {
            var keywords = new List<TokenMatch>
            {
                new TokenMatch("fn", TokenKind.KeywordFn),
                new TokenMatch("struct", TokenKind.KeywordStruct),
                new TokenMatch("trait", TokenKind.KeywordTrait),
                new TokenMatch("gen", TokenKind.KeywordGen),
                new TokenMatch("use", TokenKind.KeywordUse),
                new TokenMatch("let", TokenKind.KeywordLet),
                new TokenMatch("mut", TokenKind.KeywordMut),
                new TokenMatch("if", TokenKind.KeywordIf),
                new TokenMatch("else", TokenKind.KeywordElse),
                new TokenMatch("while", TokenKind.KeywordWhile),
                new TokenMatch("return", TokenKind.KeywordReturn),
                new TokenMatch("true", TokenKind.BooleanTrue),
                new TokenMatch("false", TokenKind.BooleanFalse),
                new TokenMatch("_", TokenKind.Placeholder)
            };


            // Check for alphabetic identifiers.
            if (IsIdentifierBeginning(unit[index]))
            {
                var identifier = new StringBuilder();
                identifier.Append(unit[index]);
                index++;

                while (index < unit.Length() && IsIdentifier(unit[index]))
                {
                    identifier.Append(unit[index]);
                    index++;
                }

                var identifierStr = identifier.ToString();

                // Check if it is a keyword.
                var keywordMatch = keywords.Find(k => k.representation == identifierStr);
                return (keywordMatch ?? new TokenMatch(identifierStr, TokenKind.Identifier));
            }
            // Check for number literals.
            else if (IsNumberBeginning(unit[index]))
            {
                var number = new StringBuilder();
                number.Append(unit[index]);
                index++;

                while (index < unit.Length() && IsNumber(unit[index]))
                {
                    number.Append(unit[index]);
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
