using System.Collections.Generic;
using System.Text;
using Trapl.Diagnostics;


namespace Trapl.Grammar
{
    public class Tokenizer
    {
        public static TokenCollection Tokenize(Infrastructure.Session session, Infrastructure.Unit unit)
        {
            var output = new TokenCollection();

            // Iterate through all characters in input.
            var index = 0;
            while (index < unit.Length())
            {
                // Skip whitespace.
                if (IsWhitespace(unit[index]))
                {
                    index++;
                    continue;
                }

                // Match next characters to a token, and add it to the output.
                var match =
                    TryMatchModelToken(unit, index) ??
                    TryMatchVaryingToken(unit, index) ??
                    new TokenMatch(new string(unit[index], 1), TokenKind.Error);

                var span = new Span(unit, index, index + match.representation.Length);

                // Signal errors.
                if (match.kind == TokenKind.Error)
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.UnexpectedChar, "unexpected character", span);
                }
                // Check number format correctness.
                else if (match.kind == TokenKind.Number)
                {
                    CheckNumberValidity(session, match.representation, span);
                }
                // Skip line comments.
                else if (match.kind == TokenKind.DoubleHash)
                {
                    while (index < unit.Length() && unit[index] != '\n')
                        index++;
                    continue;
                }
                // Skip multiline comments.
                else if (match.kind == TokenKind.HashColon)
                {
                    var nesting = 1;
                    index += 2;

                    while (index < unit.Length() - 1)
                    {
                        if (unit[index] == ':' && unit[index + 1] == '#')
                        {
                            nesting--;
                            index += 2;
                            if (nesting == 0)
                                break;
                        }
                        else if (unit[index] == '#' && unit[index + 1] == ':')
                        {
                            nesting++;
                            index += 2;
                        }
                        else
                            index++;
                    }

                    if (nesting > 0)
                        session.diagn.Add(MessageKind.Error, MessageCode.UnexpectedChar, "unterminated comment", span);

                    continue;
                }

                output.tokens.Add(new Token(match.kind, span));

                index += match.representation.Length;
            }

            output.tokenAfterEnd =
                new Token(TokenKind.Error, new Diagnostics.Span(unit, index, index));

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


        private static TokenMatch TryMatchModelToken(Infrastructure.Unit unit, int index)
        {
            var models = new List<TokenMatch>
            {
                new TokenMatch("##", TokenKind.DoubleHash),
                new TokenMatch("#:", TokenKind.HashColon),
                new TokenMatch(":#", TokenKind.ColonHash),
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
                new TokenMatch("@", TokenKind.At)
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


        private static TokenMatch TryMatchVaryingToken(Infrastructure.Unit unit, int index)
        {
            var keywords = new List<TokenMatch>
            {
                new TokenMatch("fn", TokenKind.KeywordFn),
                new TokenMatch("struct", TokenKind.KeywordStruct),
                new TokenMatch("trait", TokenKind.KeywordTrait),
                new TokenMatch("gen", TokenKind.KeywordGen),
                new TokenMatch("let", TokenKind.KeywordLet),
                new TokenMatch("new", TokenKind.KeywordNew),
                new TokenMatch("if", TokenKind.KeywordIf),
                new TokenMatch("else", TokenKind.KeywordElse),
                new TokenMatch("while", TokenKind.KeywordWhile),
                new TokenMatch("return", TokenKind.KeywordReturn),
                new TokenMatch("true", TokenKind.BooleanTrue),
                new TokenMatch("false", TokenKind.BooleanFalse)
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


        private static void CheckNumberValidity(Infrastructure.Session session, string numStr, Diagnostics.Span span)
        {
            var possibleDigits = new char[] {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                'a', 'b', 'c', 'd', 'e', 'f' };

            var index = 0;
            var numBase = 10;

            if (numStr.StartsWith("0b"))
            {
                numBase = 2;
                index += 2;
            }
            else if (numStr.StartsWith("0o"))
            {
                numBase = 8;
                index += 2;
            }
            else if (numStr.StartsWith("0x"))
            {
                numBase = 16;
                index += 2;
            }

            while (index < numStr.Length)
            {
                var c = numStr[index];
                index++;

                if (c == '_')
                    continue;

                var isValid = false;
                for (int d = 0; d < numBase; d++)
                {
                    if (c == possibleDigits[d] || c == char.ToUpper(possibleDigits[d]))
                    {
                        isValid = true;
                        break;
                    }
                }

                if (!isValid)
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.InvalidFormat,
                        "invalid number", span);
                    return;
                }
            }
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
