using System;
using System.Collections.Generic;


namespace Trapl.Diagnostics
{
	public enum MessageKind
    {
		Info, Style, Warning, Error
    }


	public class MessageCaret
    {
        public Source source;
        public Diagnostics.Span span;


        public static MessageCaret Primary(Source source, Diagnostics.Span span)
        {
            return new MessageCaret(source, span);
        }


		private MessageCaret(Source source, Diagnostics.Span span)
        {
            this.source = source;
            this.span = span;
        }
    }


    public class MessageList
    {
        private List<Message> messages;


        public MessageList()
        {
            this.messages = new List<Message>();
        }

        public void AddError(MessageID id, Source source, Diagnostics.Span span)
        {
            this.messages.Add(Message.MakeError(id, MessageCaret.Primary(source, span)));
        }


        public void AddError(MessageID id, params MessageCaret[] carets)
        {
            this.messages.Add(Message.MakeError(id, carets));
        }


        public bool Passed()
        {
            foreach (var msg in this.messages)
            {
                if (msg.GetKind() == MessageKind.Error)
                    return false;
            }
            return true;
        }


        public bool Failed()
        {
            foreach (var msg in this.messages)
            {
                if (msg.GetKind() == MessageKind.Error)
                    return true;
            }
            return false;
        }


        public bool ContainsCode(string code)
        {
            foreach (var msg in this.messages)
            {
                if (msg.GetMessageID().code == code)
                    return true;
            }
            return false;
        }


        public void Print()
        {
            foreach (var msg in messages)
            {
                msg.Print();
            }
        }
    }


    public static class MessageCode
    {
        public const string
            Internal = "0",
            LexerUnexpectedChar = "100",
            SyntaxExpected = "200",
            SyntaxExpectedExpression = "201",
            SyntaxExpectedType = "202",
            SyntaxExpectedIdentifier = "203",
            SyntaxExpectedNumber = "203",
            SyntaxExpectedDecl = "203",
            SyntaxUnmatchedElse = "204",
            SemanticsUnknownType = "301",
            SemanticsVoidType = "302",
            SemanticsStructCycleDetected = "303",
            SemanticsDoubleDef = "304";
    }


    public class MessageID
    {
        public string code;
        public string text;


        public MessageID(string code, string text)
        {
            this.code = code;
            this.text = text;
        }


        public static MessageID Internal(string str) { return new MessageID(MessageCode.Internal, "[internal] " + str); }
        public static MessageID LexerUnexpectedChar() { return new MessageID(MessageCode.LexerUnexpectedChar, "unexpected character"); }
        public static MessageID SyntaxExpected(params string[] strs)
        {
            string charsStr = "";
            for (int i = 0; i < strs.Length; i++)
            {
                charsStr += "'" + strs[i] + "'";
                if (i < strs.Length - 2) charsStr += ", ";
                else if (i < strs.Length - 1) charsStr += " or ";
            }
            return new MessageID(MessageCode.SyntaxExpected, "expecting " + charsStr);
        }
        public static MessageID SyntaxExpectedExpression() { return new MessageID(MessageCode.SyntaxExpectedExpression, "expecting expression"); }
        public static MessageID SyntaxExpectedType() { return new MessageID(MessageCode.SyntaxExpectedType, "expecting type name"); }
        public static MessageID SyntaxExpectedIdentifier() { return new MessageID(MessageCode.SyntaxExpectedIdentifier, "expecting identifier"); }
        public static MessageID SyntaxExpectedNumber() { return new MessageID(MessageCode.SyntaxExpectedNumber, "expecting number literal"); }
        public static MessageID SyntaxExpectedDecl() { return new MessageID(MessageCode.SyntaxExpectedDecl, "expecting a declaration"); }
        public static MessageID SyntaxUnmatchedElse() { return new MessageID(MessageCode.SyntaxUnmatchedElse, "unmatched 'else'"); }
        public static MessageID SemanticsStructCycleDetected() { return new MessageID(MessageCode.SemanticsStructCycleDetected, "struct cycle detected"); }
        public static MessageID SemanticsUnknownType() { return new MessageID(MessageCode.SemanticsUnknownType, "unknown type"); }
        public static MessageID SemanticsVoidType() { return new MessageID(MessageCode.SemanticsVoidType, "cannot use Void explicitly"); }
        public static MessageID SemanticsDoubleDef() { return new MessageID(MessageCode.SemanticsDoubleDef, "double declaration with the same name"); }
    }


    public class Message
    {
		public static Message MakeError(MessageID id, params MessageCaret[] carets)
        {
            return new Message(id, MessageKind.Error, carets);
        }


        public string GetText()
        {
            return this.id.text;
        }


        public MessageKind GetKind()
        {
            return this.kind;
        }


        public MessageID GetMessageID()
        {
            return this.id;
        }


        public void Print()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(ErrorPositionString() + ":err" + this.id.code + ": ");
            Console.ForegroundColor = GetLightColor(this.kind);
            Console.Write(GetKindName(this.kind) + ": ");
            Console.WriteLine(this.id.text);
            PrintErrorWithHighlighting();
            Console.ResetColor();
        }


        private MessageID id;
        private MessageKind kind;
        private MessageCaret[] carets;
        private Source source; // FIXME! Workaround for the time being. Each caret should contain its source.


        private Message(MessageID id, MessageKind kind, params MessageCaret[] carets)
        {
            this.id = id;
            this.kind = kind;
            this.carets = carets;
            this.source = this.carets[0].source;
        }


        private string GetKindName(MessageKind kind)
        {
            switch (this.kind)
            {
                case MessageKind.Error: return "error";
                case MessageKind.Warning: return "warning";
                case MessageKind.Style: return "style";
                case MessageKind.Info: return "info";
                default: return "unknown";
            }
        }


        private ConsoleColor GetLightColor(MessageKind kind)
        {
            switch (this.kind)
            {
                case MessageKind.Error: return ConsoleColor.Red;
                case MessageKind.Warning: return ConsoleColor.Yellow;
                case MessageKind.Style: return ConsoleColor.Magenta;
                case MessageKind.Info: return ConsoleColor.Cyan;
                default: return ConsoleColor.White;
            }
        }


        private ConsoleColor GetDarkColor(MessageKind kind)
        {
            switch (this.kind)
            {
                case MessageKind.Error: return ConsoleColor.DarkRed;
                case MessageKind.Warning: return ConsoleColor.DarkYellow;
                case MessageKind.Style: return ConsoleColor.DarkMagenta;
                case MessageKind.Info: return ConsoleColor.DarkCyan;
                default: return ConsoleColor.Gray;
            }
        }


        private string ErrorPositionString()
        {
            string result = "";
            if (this.carets.Length > 0)
            {
                result = this.source.Name() + ":";

                if (this.carets.Length > 0)
                {
                    result += (this.source.LineStart(this.carets[0].span) + 1) + ":";
                    result += (this.source.ColumnStart(this.carets[0].span) + 1);
                }
            }
            else
                result = "<unknown location>";

            return result;
        }


        private int MinimumLineDistanceFromCarets(int line)
        {
            int min = -1;

            foreach (var caret in this.carets)
            {
                int dist = Math.Min(
                    Math.Abs(this.source.LineStart(caret.span) - line),
                    Math.Abs(this.source.LineEnd(caret.span) - line));

                if (dist < min || min == -1)
                    min = dist;
            }

            return min;
        }


        private void PrintErrorWithHighlighting()
        {
            if (this.carets.Length == 0)
                return;
            
            // Find the very first and the very last lines
            // referenced by any caret, with some added margin around.
            int startLine = -1;
            int endLine = -1;
            foreach (var caret in this.carets)
            {
                int start = this.source.LineStart(caret.span) - 2;
                int end = this.source.LineEnd(caret.span) + 2;

                if (start < startLine || startLine == -1)
                    startLine = start;

                if (end > endLine || endLine == -1)
                    endLine = end;
            }

            if (startLine < 0)
                startLine = 0;

            if (endLine >= this.source.LineNumber())
                endLine = this.source.LineNumber() - 1;

            // Step through the referenced line range,
            // skipping lines in between that are too far away from any caret.
            bool skipped = false;
            for (int i = startLine; i <= endLine; i++)
            {
                if (MinimumLineDistanceFromCarets(i) > 2)
                {
                    if (!skipped)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("".PadLeft(6) + "...");
                    }
                    skipped = true;
                    continue;
                }
                else
                    skipped = false;


                Console.ForegroundColor = ConsoleColor.White;
                Console.Write((i + 1).ToString().PadLeft(5) + ": ");

                PrintLineExcerptWithHighlighting(i);

                Console.ResetColor();
                Console.WriteLine();
            }
        }


        private void PrintLineExcerptWithHighlighting(int line)
        {
            if (line >= this.source.LineNumber())
                return;

            int lineStart = this.source.LineStartIndex(line);
            int index = lineStart;
            var inbetweenLast = false;

            // Step through each character in line.
            while (index <= this.source.Length())
            {
                // Find if this character is referenced by any caret.
                var highlight = false;
                var inbetween = false;
                foreach (var caret in this.carets)
                {
                    if (index >= caret.span.start && index < caret.span.end)
                    {
                        highlight = true;
                    }
                    else if (!inbetweenLast && index == caret.span.start && index == caret.span.end)
                    {
                        highlight = true;
                        inbetween = true;
                    }
                }

                inbetweenLast = inbetween;

                // Set up text color for highlighting.
                if (highlight)
                {
                    Console.BackgroundColor = this.GetDarkColor(this.kind);
                    Console.ForegroundColor = this.GetLightColor(this.kind);
                }
                else
                {
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                // Print a space if two carets meet ends,
                // or print the current character.
                if (inbetween && (index == this.source.Length() || !(this.source[index] >= 0 && this.source[index] <= ' ')))
                {
                    Console.Write(" ");
                }
                else if (index == this.source.Length() || this.source[index] == '\n')
                {
                    Console.Write(" ");
                    break;
                }
                else
                {
                    if (this.source[index] == '\t')
                        Console.Write("  ");
                    else if (this.source[index] >= 0 && this.source[index] <= ' ')
                        Console.Write(" ");
                    else
                        Console.Write(this.source[index]);

                    index++;
                }

                Console.ResetColor();
            }
        }
    }
}
