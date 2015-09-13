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


    public enum MessageCode
    {
        Internal,
        UnexpectedChar,
        Expected,
        UnmatchedElse,
        UnknownType,
        ExplicitVoid,
        StructRecursion,
        DoubleDecl,
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
        WrongStructNameStyle
    }


    public abstract class MessageContext
    {

    }


    public class MessageContextStruct : MessageContext
    {
        public Semantics.TopDecl topDecl;


        public MessageContextStruct(Semantics.TopDecl topDecl)
        {
            this.topDecl = topDecl;
        }
    }


    public class Message
    {
		public static Message Make(MessageCode code, string text, MessageKind kind, params MessageCaret[] carets)
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


        public void PrintToConsole()
        {
            PrintContext();
            PrintMessage();
            PrintExcerptWithHighlighting();
        }


        private MessageCode code;
        private string text;
        private MessageKind kind;
        private MessageCaret[] carets;
        private Interface.SourceCode source; // FIXME! Workaround for the time being. Each caret should contain its source.
        private Stack<MessageContext> contextStack = new Stack<MessageContext>();


        private Message(MessageCode code, string text, MessageKind kind, params MessageCaret[] carets)
        {
            this.code = code;
            this.text = text;
            this.kind = kind;
            this.carets = carets;
            this.source = this.carets[0].span.src;
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


        private int GetMinimumLineDistanceFromCarets(int line)
        {
            int min = -1;

            foreach (var caret in this.carets)
            {
                int dist = Math.Min(
                    Math.Abs(this.source.GetLineIndexAtSpanStart(caret.span) - line),
                    Math.Abs(this.source.GetLineIndexAtSpanEnd(caret.span) - line));

                if (dist < min || min == -1)
                    min = dist;
            }

            return min;
        }


        private void PrintContext()
        {
            foreach (var ctx in this.contextStack)
            {
                PrintContextStruct(ctx as MessageContextStruct);
            }
        }


        private bool PrintContextStruct(MessageContextStruct ctx)
        {
            if (ctx == null)
                return false;

            PrintPosition(ctx.topDecl.declASTNode.Span());
            Console.ForegroundColor = GetLightColor(this.kind);
            Console.Write("in instantiation of '" + ctx.topDecl.GetString() + "'");
            if (ctx.topDecl.patternRepl.nameToASTNodeMap.Count > 0)
                Console.Write(" " + ctx.topDecl.patternRepl.GetString());
            Console.Write(":");
            Console.WriteLine();
            Console.ResetColor();
            return true;
        }


        private void PrintMessage()
        {
            if (this.carets.Length > 0)
                PrintPosition(this.carets[0].span);
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


        private void PrintExcerptWithHighlighting()
        {
            if (this.carets.Length == 0 || this.source == null)
                return;
            
            // Find the very first and the very last lines
            // referenced by any caret, with some added margin around.
            int startLine = -1;
            int endLine = -1;
            foreach (var caret in this.carets)
            {
                int start = this.source.GetLineIndexAtSpanStart(caret.span) - 2;
                int end = this.source.GetLineIndexAtSpanEnd(caret.span) + 2;

                if (start < startLine || startLine == -1)
                    startLine = start;

                if (end > endLine || endLine == -1)
                    endLine = end;
            }

            if (startLine < 0)
                startLine = 0;

            if (endLine >= this.source.GetNumberOfLines())
                endLine = this.source.GetNumberOfLines() - 1;

            // Step through the referenced line range,
            // skipping lines in between that are too far away from any caret.
            bool skipped = false;
            for (int i = startLine; i <= endLine; i++)
            {
                if (GetMinimumLineDistanceFromCarets(i) > 2)
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
            if (line >= this.source.GetNumberOfLines())
                return;

            int lineStart = this.source.GetLineStartPos(line);
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
