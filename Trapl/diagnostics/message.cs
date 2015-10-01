using System;
using System.Collections.Generic;


namespace Trapl.Diagnostics
{
	public enum MessageKind
    {
		Info, Style, Warning, Error
    }


    public enum MessageCode
    {
        Internal,
        UnexpectedChar,
        Expected,
        UnmatchedElse,
        UnknownType,
        ExplicitVoid,
        StructRecursion,
        DuplicateDecl,
        Shadowing,
        InferenceImpossible,
        UnknownIdentifier,
        CannotAssign,
        CannotAddress,
        CannotDereference,
        CannotCall,
        WrongArgumentNumber,
        IncompatibleTypes,
        IncompatibleTemplate,
        WrongFunctNameStyle,
        WrongStructNameStyle,
        UninitializedLocal
    }


    public class MessageContext
    {
        public string text;
        public Diagnostics.Span span;


        public MessageContext(string text, Diagnostics.Span span)
        {
            this.text = text;
            this.span = span;
        }
    }


    public class Message
    {
		public static Message Make(MessageCode code, string text, MessageKind kind, params Diagnostics.Span[] carets)
        {
            return new Message(code, text, kind, carets);
        }


        public string GetText()
        {
            return this.text;
        }


        public MessageKind GetKind()
        {
            return this.kind;
        }


        public MessageCode GetCode()
        {
            return this.code;
        }


        public void SetContext(Stack<MessageContext> ctx)
        {
            this.contextStack = new Stack<MessageContext>(ctx);
        }


        public void PrintToConsole(Interface.Session session)
        {
            PrintContext();
            PrintMessage();
            PrintExcerptWithHighlighting(this.kind, this.spans);
        }


        private MessageCode code;
        private string text;
        private MessageKind kind;
        private Diagnostics.Span[] spans;
        private Stack<MessageContext> contextStack = new Stack<MessageContext>();


        private Message(MessageCode code, string text, MessageKind kind, params Diagnostics.Span[] carets)
        {
            this.code = code;
            this.text = text;
            this.kind = kind;
            this.spans = carets;
        }


        private void PrintContext()
        {
            foreach (var ctx in this.contextStack)
            {
                PrintPosition(ctx.span);
                Console.ForegroundColor = GetLightColor(MessageKind.Info);
                Console.Write(ctx.text);
                Console.Write(":");
                Console.WriteLine();
                Console.ResetColor();
                // Probably enable this via a compiler switch.
                //PrintExcerptWithHighlighting(MessageKind.Info, ctx.span);
            }
        }


        private void PrintMessage()
        {
            if (this.spans.Length > 0)
                PrintPosition(this.spans[0]);
            else
                PrintPosition(new Diagnostics.Span());

            Console.ForegroundColor = GetLightColor(this.kind);
            Console.Write(GetKindName(this.kind) + ": ");
            Console.Write(this.text);
            Console.WriteLine();
        }


        private void PrintPosition(Diagnostics.Span span)
        {
            Console.ForegroundColor = ConsoleColor.White;

            if (span.src != null)
            {
                Console.Write(span.src.GetFullName() + ":");
                Console.Write((span.src.GetLineIndexAtSpanStart(span) + 1) + ":");
                Console.Write((span.src.GetColumnAtSpanStart(span) + 1) + ": ");
            }
            else
                Console.Write("<unknown location>: ");

            Console.ResetColor();
        }


        private string GetKindName(MessageKind kind)
        {
            switch (kind)
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
            switch (kind)
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
            switch (kind)
            {
                case MessageKind.Error: return ConsoleColor.DarkRed;
                case MessageKind.Warning: return ConsoleColor.DarkYellow;
                case MessageKind.Style: return ConsoleColor.DarkMagenta;
                case MessageKind.Info: return ConsoleColor.DarkCyan;
                default: return ConsoleColor.Gray;
            }
        }


        private int GetMinimumLineDistanceFromCarets(Diagnostics.Span[] spans, int line)
        {
            int min = -1;

            foreach (var span in spans)
            {
                int dist = Math.Min(
                    Math.Abs(span.src.GetLineIndexAtSpanStart(span) - line),
                    Math.Abs(span.src.GetLineIndexAtSpanEnd(span) - line));

                if (dist < min || min == -1)
                    min = dist;
            }

            return min;
        }


        private void PrintExcerptWithHighlighting(MessageKind color, params Diagnostics.Span[] spans)
        {
            if (spans.Length == 0)
                return;

            // FIXME! Not ready for multiple sources yet.
            var source = spans[0].src;
            if (source == null)
                return;
            
            // Find the very first and the very last lines
            // referenced by any caret, with some added margin around.
            int startLine = -1;
            int endLine = -1;
            foreach (var span in spans)
            {
                int start = source.GetLineIndexAtSpanStart(span) - 2;
                int end = source.GetLineIndexAtSpanEnd(span) + 2;

                if (start < startLine || startLine == -1)
                    startLine = start;

                if (end > endLine || endLine == -1)
                    endLine = end;
            }

            if (startLine < 0)
                startLine = 0;

            if (endLine >= source.GetNumberOfLines())
                endLine = source.GetNumberOfLines() - 1;

            // Step through the referenced line range,
            // skipping lines in between that are too far away from any caret.
            bool skipped = false;
            for (int i = startLine; i <= endLine; i++)
            {
                if (GetMinimumLineDistanceFromCarets(spans, i) > 2)
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

                PrintLineExcerptWithHighlighting(color, i, spans);

                Console.ResetColor();
                Console.WriteLine();
            }
        }


        private void PrintLineExcerptWithHighlighting(MessageKind color, int line, Diagnostics.Span[] spans)
        {
            // FIXME! Not ready for multiple sources yet.
            var source = spans[0].src;

            if (line >= source.GetNumberOfLines())
                return;

            int lineStart = source.GetLineStartPos(line);
            int index = lineStart;
            var inbetweenLast = false;

            // Step through each character in line.
            while (index <= source.Length())
            {
                // Find if this character is referenced by any caret.
                var highlight = false;
                var inbetween = false;
                foreach (var span in spans)
                {
                    if (index >= span.start && index < span.end)
                    {
                        highlight = true;
                    }
                    else if (!inbetweenLast && index == span.start && index == span.end)
                    {
                        highlight = true;
                        inbetween = true;
                    }
                }

                inbetweenLast = inbetween;

                // Set up text color for highlighting.
                if (highlight)
                {
                    Console.BackgroundColor = this.GetDarkColor(color);
                    Console.ForegroundColor = this.GetLightColor(color);
                }
                else
                {
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                // Print a space if two carets meet ends,
                // or print the current character.
                if (inbetween && (index == source.Length() || !(source[index] >= 0 && source[index] <= ' ')))
                {
                    Console.Write(" ");
                }
                else if (index == source.Length() || source[index] == '\n')
                {
                    Console.Write(" ");
                    break;
                }
                else
                {
                    if (source[index] == '\t')
                        Console.Write("  ");
                    else if (source[index] >= 0 && source[index] <= ' ')
                        Console.Write(" ");
                    else
                        Console.Write(source[index]);

                    index++;
                }

                Console.ResetColor();
            }
        }
    }
}
