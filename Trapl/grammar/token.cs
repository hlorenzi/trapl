using System;
using System.Collections.Generic;


namespace Trapl.Grammar
{
    public enum TokenKind
    {
        Error,
        Identifier, Number,
        KeywordFunct, KeywordStruct, KeywordTrait, KeywordGen,
        KeywordLet, KeywordIf, KeywordElse, KeywordWhile, KeywordReturn,
        BraceOpen, BraceClose, ParenOpen, ParenClose,
        Period, Comma, Colon, Semicolon, Arrow, DoubleColon, TriplePeriod,
        Equal, Plus, Minus, Asterisk, Slash,
        Ampersand, VerticalBar, Circumflex, At,
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


    public class TokenCollection
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


        public void PrintDebug(Interface.SourceCode src)
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
                Console.Out.Write(src.GetExcerpt(token.span));
                Console.Out.WriteLine();
            }
        }
    }
}