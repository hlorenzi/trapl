using System;
using System.IO;


namespace Trapl
{
    public class SourceCode
    {
        public static SourceCode MakeFromFile(string filepath)
        {
            var src = new SourceCode();
            src.filepath = filepath;
            return src;
        }

        public static SourceCode MakeFromString(string str)
        {
            var src = new SourceCode();
            src.stringContents = str;
            return src;
        }


        private string stringContents;
        private string filepath;
        private WeakReference<string> fileContents;


        private SourceCode()
        {
            this.stringContents = null;
            this.filepath = null;
            this.fileContents = new WeakReference<string>(null);
        }


        public string GetFullName()
        {
            if (this.stringContents != null)
                return "string";
            else if (this.filepath != null)
                return filepath;
            else
                return "unknown";
        }


        public string GetExcerpt(Diagnostics.Span span)
        {
            return this.GetContentString().Substring(span.start, span.Length());
        }


        public string GetContentString()
        {
            if (this.stringContents != null)
                return this.stringContents;

            string str;
            if (!this.fileContents.TryGetTarget(out str) || str == null)
            {
                using (var fileStream = File.Open(this.filepath, FileMode.Open))
                {
                    var bytes = new byte[fileStream.Length];
                    fileStream.Read(bytes, 0, (int)fileStream.Length);
                    str = System.Text.Encoding.Default.GetString(bytes);
                    this.fileContents.SetTarget(str);
                }
            }

            return str;
        }


        public char this[int index]
        {
            get
            {
                if (index < 0 || index >= this.Length())
                    return '\0';
                else
                    return this.GetContentString()[index];
            }
        }


        public int Length()
        {
            return this.GetContentString().Length;
        }


        public int GetNumberOfLines()
        {
            int lineCount = 0;
            int charCount = 0;

            while (charCount < this.GetContentString().Length)
            {
                var c = this.GetContentString()[charCount];
                charCount++;

                if (c == '\n')
                    lineCount++;
            }

            return lineCount + 1;
        }


        public int GetLineAtPos(int pos)
        {
            int lineCount = 0;
            int charCount = 0;

            while (charCount < this.GetContentString().Length && charCount < pos)
            {
                var c = this.GetContentString()[charCount];
                charCount++;

                if (c == '\n')
                    lineCount++;
            }

            return lineCount;
        }


        public int GetColumnAtPos(int pos)
        {
            int lineCount = 0;
            int columnCount = 0;
            int charCount = 0;

            while (charCount < this.GetContentString().Length && charCount < pos)
            {
                var c = this.GetContentString()[charCount];
                charCount++;
                columnCount++;

                if (c == '\n')
                {
                    lineCount++;
                    columnCount = 0;
                }
            }

            return columnCount;
        }


        public int GetLineStartPos(int line)
        {
            int lineCount = 0;
            int charCount = 0;

            while (charCount < this.GetContentString().Length && lineCount < line)
            {
                var c = this.GetContentString()[charCount];
                charCount++;

                if (c == '\n')
                    lineCount++;
            }

            return charCount;
        }


        public int GetLineEndPos(int line)
        {
            int charCount = GetLineStartPos(line);

            while (charCount < this.GetContentString().Length)
            {
                var c = this.GetContentString()[charCount];
                charCount++;

                if (c == '\n')
                    break;
            }

            return charCount;
        }


        public string GetLineExcerpt(int line)
        {
            int start = GetLineStartPos(line);
            int end = GetLineEndPos(line);
            return this.GetContentString().Substring(start, end - start);
        }


        public int GetLineIndexAtSpanStart(Diagnostics.Span span)
        {
            return this.GetLineAtPos(span.start);
        }


        public int GetLineIndexAtSpanEnd(Diagnostics.Span span)
        {
            return this.GetLineAtPos(span.end);
        }


        public int GetColumnAtSpanStart(Diagnostics.Span span)
        {
            return this.GetColumnAtPos(span.start);
        }


        public int GetColumnAtSpanEnd(Diagnostics.Span span)
        {
            return this.GetColumnAtPos(span.end);
        }
    }
}
