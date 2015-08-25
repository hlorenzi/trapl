using System;
using System.IO;


namespace Trapl
{
    public class Source
    {
        public static Source FromFile(string filepath)
        {
            var src = new Source();
            src.filepath = filepath;
            return src;
        }


        private WeakReference<string> contents;
        private string filepath;


        private Source()
        {
            this.contents = new WeakReference<string>(null);
            this.filepath = null;
        }


        public string Name()
        {
            return (filepath == null ? "unknown" : filepath);
        }


        public string Excerpt(Diagnostics.Span span)
        {
            return this.Contents().Substring(span.start, span.Length());
        }


        public string Contents()
        {
            string str;
            if (!this.contents.TryGetTarget(out str) || str == null)
            {
                using (var fileStream = File.Open(this.filepath, FileMode.Open))
                {
                    var bytes = new byte[fileStream.Length];
                    fileStream.Read(bytes, 0, (int)fileStream.Length);
                    str = System.Text.Encoding.Default.GetString(bytes);
                    this.contents.SetTarget(str);
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
                    return this.Contents()[index];
            }
        }


        public int Length()
        {
            return this.Contents().Length;
        }


        public int LineNumber()
        {
            int lineCount = 0;
            int charCount = 0;

            while (charCount < this.Contents().Length)
            {
                var c = this.Contents()[charCount];
                charCount++;

                if (c == '\n')
                    lineCount++;
            }

            return lineCount + 1;
        }


        public int LineAt(int pos)
        {
            int lineCount = 0;
            int charCount = 0;

            while (charCount < this.Contents().Length && charCount < pos)
            {
                var c = this.Contents()[charCount];
                charCount++;

                if (c == '\n')
                    lineCount++;
            }

            return lineCount;
        }


        public int ColumnAt(int pos)
        {
            int lineCount = 0;
            int columnCount = 0;
            int charCount = 0;

            while (charCount < this.Contents().Length && charCount < pos)
            {
                var c = this.Contents()[charCount];
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


        public int LineStartIndex(int line)
        {
            int lineCount = 0;
            int charCount = 0;

            while (charCount < this.Contents().Length && lineCount < line)
            {
                var c = this.Contents()[charCount];
                charCount++;

                if (c == '\n')
                    lineCount++;
            }

            return charCount;
        }


        public int LineEndIndex(int line)
        {
            int charCount = LineStartIndex(line);

            while (charCount < this.Contents().Length)
            {
                var c = this.Contents()[charCount];
                charCount++;

                if (c == '\n')
                    break;
            }

            return charCount;
        }


        public string LineExcerpt(int line)
        {
            int start = LineStartIndex(line);
            int end = LineEndIndex(line);
            return this.Contents().Substring(start, end - start);
        }


        public int LineStart(Diagnostics.Span span)
        {
            return this.LineAt(span.start);
        }


        public int LineEnd(Diagnostics.Span span)
        {
            return this.LineAt(span.end);
        }


        public int ColumnStart(Diagnostics.Span span)
        {
            return this.ColumnAt(span.start);
        }


        public int ColumnEnd(Diagnostics.Span span)
        {
            return this.ColumnAt(span.end);
        }
    }
}
