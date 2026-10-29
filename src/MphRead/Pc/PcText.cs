using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MphRead
{
    public static class PcText
    {
        public static string Normalize(string text)
        {
            var result = new StringBuilder();
            foreach (char c in text.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark || c == '\r') continue;
                if (c == '\n' || c is >= ' ' and <= '~') result.Append(c);
                else if (c == '\t') result.Append(' ');
            }
            return result.ToString();
        }

        public static IReadOnlyList<string> Wrap(string text, int columns)
        {
            if (columns < 1) throw new ArgumentOutOfRangeException(nameof(columns));
            var lines = new List<string>();
            foreach (string paragraph in Normalize(text).Split('\n'))
            {
                string line = "";
                foreach (string source in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    string word = source;
                    if (line.Length > 0 && line.Length + 1 + word.Length > columns) { lines.Add(line); line = ""; }
                    while (word.Length > columns)
                    {
                        lines.Add(word[..columns]); word = word[columns..];
                    }
                    if (word.Length > 0) line += (line.Length == 0 ? "" : " ") + word;
                }
                lines.Add(line);
            }
            return lines;
        }

        public static string Ellipsis(string value, int columns)
        {
            value = Normalize(value).Replace('\n', ' ');
            return value.Length <= columns ? value : value[..Math.Max(0, columns - 3)] + "...";
        }
    }
}
