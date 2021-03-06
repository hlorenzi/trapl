﻿using System;
using System.Collections.Generic;


namespace Trapl.Grammar
{
    public enum TokenKind
    {
        Error,
        DoubleSlash, SlashAsterisk, AsteriskSlash,
        Identifier, Number, NumberPrefix, NumberSuffix, BooleanTrue, BooleanFalse, Placeholder,
        KeywordFn, KeywordStruct, KeywordTrait, KeywordGen, KeywordUse,
        KeywordLet, KeywordMut, KeywordIf, KeywordElse, KeywordWhile, KeywordReturn,
        BraceOpen, BraceClose, ParenOpen, ParenClose,
        Period, Comma, Colon, Semicolon, Arrow, DoubleColon, TriplePeriod,
        Equal, Plus, Minus, Asterisk, Slash, PercentSign,
        Ampersand, VerticalBar, Circumflex, At, SingleQuote,
        DoubleEqual, ExclamationMark, ExclamationMarkEqual, QuestionMark,
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
                if (index >= tokens.Count)
                    return tokenAfterEnd;

                return tokens[index];
            }
        }


        public void PrintDebug()
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
                Console.Out.Write(token.span.GetExcerpt());
                Console.Out.WriteLine();
            }
        }
    }
}