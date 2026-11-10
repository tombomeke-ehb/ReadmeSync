namespace ReadmeSync.Services
{
    /// <summary>
    /// A comment found in a source file.
    /// </summary>
    public readonly record struct CommentSpan(int Start, string Text);

    /// <summary>
    /// Source text with comments and string literals blanked out, so that structural
    /// patterns never match inside them. Offsets are identical to the original text.
    /// </summary>
    public sealed class MaskedSource
    {
        private readonly List<int> _lineStarts = new() { 0 };

        public string Original { get; }
        public string Code { get; }
        public IReadOnlyList<CommentSpan> Comments { get; }

        internal MaskedSource(string original, string code, IReadOnlyList<CommentSpan> comments)
        {
            Original = original;
            Code = code;
            Comments = comments;

            for (int i = 0; i < original.Length; i++)
            {
                if (original[i] == '\n')
                    _lineStarts.Add(i + 1);
            }
        }

        /// <summary>Returns the 1-based line number of a character offset.</summary>
        public int LineOf(int index)
        {
            int pos = _lineStarts.BinarySearch(index);
            return pos >= 0 ? pos + 1 : ~pos;
        }
    }

    /// <summary>
    /// Blanks out comments and string literals while collecting the comments.
    /// </summary>
    public static class SourceMasker
    {
        public static MaskedSource Mask(string text, string language)
        {
            bool slashComments = language != "python";
            bool hashComments = language is "python" or "php";
            bool backtickStrings = language is "typescript" or "javascript" or "go";
            bool tripleDouble = language is "python" or "csharp" or "java" or "kotlin";
            bool tripleSingle = language == "python";
            bool verbatim = language == "csharp";
            bool multilineQuotes = language == "php";

            char[] buffer = text.ToCharArray();
            var comments = new List<CommentSpan>();
            int n = text.Length;
            int i = 0;

            while (i < n)
            {
                char c = text[i];
                int end;

                if (slashComments && c == '/' && i + 1 < n && text[i + 1] == '/')
                {
                    end = LineEnd(text, i);
                    comments.Add(new CommentSpan(i, text[i..end]));
                }
                else if (slashComments && c == '/' && i + 1 < n && text[i + 1] == '*')
                {
                    int close = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    end = close < 0 ? n : close + 2;
                    comments.Add(new CommentSpan(i, text[i..end]));
                }
                else if (hashComments && c == '#' && !(language == "php" && i + 1 < n && text[i + 1] == '['))
                {
                    end = LineEnd(text, i);
                    comments.Add(new CommentSpan(i, text[i..end]));
                }
                else if (c == '"' && tripleDouble && StartsWith(text, i, "\"\"\""))
                {
                    end = FindClosing(text, i + 3, "\"\"\"");
                }
                else if (c == '\'' && tripleSingle && StartsWith(text, i, "'''"))
                {
                    end = FindClosing(text, i + 3, "'''");
                }
                else if (c == '"' && verbatim && IsVerbatimPrefix(text, i))
                {
                    end = VerbatimEnd(text, i);
                }
                else if (c == '`' && backtickStrings)
                {
                    end = language == "go" ? FindClosing(text, i + 1, "`") : QuotedEnd(text, i, multiline: true);
                }
                else if (c == '"' || c == '\'')
                {
                    end = QuotedEnd(text, i, multilineQuotes);
                }
                else
                {
                    i++;
                    continue;
                }

                for (int k = i; k < end; k++)
                {
                    if (buffer[k] != '\n' && buffer[k] != '\r')
                        buffer[k] = ' ';
                }
                i = end;
            }

            return new MaskedSource(text, new string(buffer), comments);
        }

        private static int LineEnd(string text, int from)
        {
            int end = text.IndexOf('\n', from);
            return end < 0 ? text.Length : end;
        }

        private static bool StartsWith(string text, int index, string value) =>
            string.CompareOrdinal(text, index, value, 0, value.Length) == 0;

        private static int FindClosing(string text, int from, string terminator)
        {
            int close = from <= text.Length ? text.IndexOf(terminator, from, StringComparison.Ordinal) : -1;
            return close < 0 ? text.Length : close + terminator.Length;
        }

        private static bool IsVerbatimPrefix(string text, int quoteIndex)
        {
            if (quoteIndex == 0) return false;
            char prev = text[quoteIndex - 1];
            if (prev == '@') return true;
            return prev == '$' && quoteIndex > 1 && text[quoteIndex - 2] == '@';
        }

        private static int VerbatimEnd(string text, int quoteIndex)
        {
            int j = quoteIndex + 1;
            while (j < text.Length)
            {
                if (text[j] == '"')
                {
                    if (j + 1 < text.Length && text[j + 1] == '"')
                    {
                        j += 2;
                        continue;
                    }
                    return j + 1;
                }
                j++;
            }
            return text.Length;
        }

        private static int QuotedEnd(string text, int quoteIndex, bool multiline)
        {
            char quote = text[quoteIndex];
            int j = quoteIndex + 1;
            while (j < text.Length)
            {
                char ch = text[j];
                if (ch == '\\')
                {
                    j += 2;
                    continue;
                }
                if (ch == quote) return j + 1;
                if (ch == '\n' && !multiline) return j;
                j++;
            }
            return text.Length;
        }
    }
}
