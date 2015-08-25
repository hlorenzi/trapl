using System;
using System.Collections.Generic;


namespace Trapl.Diagnostics
{
	public enum MessageKind
    {
		Info, Warning, Error
    }


	public class MessageCaret
    {
        public Diagnostics.Span span;


        public static MessageCaret Primary(Diagnostics.Span span)
        {
            return new MessageCaret(span);
        }


		private MessageCaret(Diagnostics.Span span)
        {
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


		public void AddError(string text, Source source, params MessageCaret[] carets)
        {
            this.messages.Add(Message.MakeError(text, source, carets));
        }


        public void Print()
        {
            foreach (var msg in messages)
            {
                msg.Print();
            }
        }
    }


    public class Message
    {
		public static Message MakeError(string text, Source source, params MessageCaret[] carets)
        {
            return new Message(text, MessageKind.Error, source, carets);
        }


        public string GetText()
        {
            return this.text;
        }


        public void Print()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(ErrorPositionString() + ": ");

            switch (this.kind)
            {
                case MessageKind.Error:   Console.ForegroundColor = ConsoleColor.Red; Console.Write("error: "); break;
                case MessageKind.Warning: Console.ForegroundColor = ConsoleColor.Yellow; Console.Write("warning: "); break;
                case MessageKind.Info:    Console.ForegroundColor = ConsoleColor.Cyan; Console.Write("note: "); break;
            }

            Console.WriteLine(this.text);
            PrintErrorWithHighlighting();
            Console.ResetColor();
        }


        private string text;
        private MessageKind kind;
        private Source source;
        private MessageCaret[] carets;


        private Message(string text, MessageKind kind, Source source, params MessageCaret[] carets)
        {
            this.text = text;
            this.kind = kind;
            this.source = source;
            this.carets = carets;
        }


        private string ErrorPositionString()
        {
            string result = "";
            if (this.source != null)
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
                    switch (this.kind)
                    {
                        case MessageKind.Error:
                        {
                            Console.BackgroundColor = ConsoleColor.DarkRed;
                            Console.ForegroundColor = ConsoleColor.Red;
                            break;
                        }
                        case MessageKind.Warning:
                        {
                            Console.BackgroundColor = ConsoleColor.DarkYellow;
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            break;
                        }
                        case MessageKind.Info:
                        {
                            Console.BackgroundColor = ConsoleColor.DarkCyan;
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            break;
                        }
                    }
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
