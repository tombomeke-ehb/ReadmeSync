using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReadmeSync.Services
{
    public class DiffLine
    {
        public char Type { get; set; }
        public string Content { get; set; } = "";
    }

    public class DiffService
    {
        public string GenerateDiff(string oldContent, string newContent)
        {
            var oldLines = oldContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var newLines = newContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            var diffLines = ComputeLCS(oldLines, newLines);
            var sb = new StringBuilder();

            sb.AppendLine("--- Original");
            sb.AppendLine("+++ Updated");
            sb.AppendLine();

            foreach (var line in diffLines)
            {
                if (line.Type == '+')
                    sb.AppendLine($"+ {line.Content}");
                else if (line.Type == '-')
                    sb.AppendLine($"- {line.Content}");
                else if (line.Type == ' ')
                    sb.AppendLine($"  {line.Content}");
            }

            return sb.ToString();
        }

        public List<DiffLine> GetDiffLines(string oldContent, string newContent)
        {
            var oldLines = oldContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var newLines = newContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            return ComputeLCS(oldLines, newLines);
        }

        private List<DiffLine> ComputeLCS(string[] oldLines, string[] newLines)
        {
            int m = oldLines.Length;
            int n = newLines.Length;
            int[,] dp = new int[m + 1, n + 1];

            for (int i = 1; i <= m; i++)
            {
                for (int j = 1; j <= n; j++)
                {
                    if (oldLines[i - 1] == newLines[j - 1])
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                    else
                        dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                }
            }

            var diffLines = new List<DiffLine>();
            int x = m, y = n;

            while (x > 0 && y > 0)
            {
                if (oldLines[x - 1] == newLines[y - 1])
                {
                    diffLines.Add(new DiffLine { Type = ' ', Content = oldLines[x - 1] });
                    x--;
                    y--;
                }
                else if (dp[x - 1, y] > dp[x, y - 1])
                {
                    diffLines.Add(new DiffLine { Type = '-', Content = oldLines[x - 1] });
                    x--;
                }
                else
                {
                    diffLines.Add(new DiffLine { Type = '+', Content = newLines[y - 1] });
                    y--;
                }
            }

            while (x > 0)
            {
                diffLines.Add(new DiffLine { Type = '-', Content = oldLines[x - 1] });
                x--;
            }

            while (y > 0)
            {
                diffLines.Add(new DiffLine { Type = '+', Content = newLines[y - 1] });
                y--;
            }

            diffLines.Reverse();
            return diffLines;
        }
    }
}
