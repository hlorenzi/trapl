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

        public static Source FromString(string str)
        {
            var src = new Source();
            src.stringContents = str;
            return src;
        }


        private string stringContents;
        private string filepath;
        private WeakReference<string> fileContents;


        private Source()
        {
            this.stringContents = null;
            this.filepath = null;
            this.fileContents = new WeakReference<string>(null);
        }


        public string Name()
        {
            if (this.stringContents != null)
                return "string";
            else if (this.filepath != null)
                return filepath;
            else
                return "unknown";
        }


        public string Excerpt(Diagnostics.Span span)
        {
            return this.Contents().Substring(span.start, span.Length());
        }


        public string Contents()
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
