using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ReadmeSync.Models;

namespace ReadmeSync.Services
{
    public class ValidatorService
    {
        public ValidationResult Validate(string markdownContent, string repoUrl = "")
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(markdownContent))
            {
                result.IsValid = false;
                result.Issues.Add(new ValidationIssue
                {
                    Severity = Severity.Error,
                    Message = "Content is empty"
                });
                return result;
            }

            CheckMarker(markdownContent, result);
            CheckEmptySections(markdownContent, result);
            CheckMissingSummaries(markdownContent, result);
            CheckBrokenLinks(markdownContent, result);

            result.IsValid = !result.Issues.Any(i => i.Severity == Severity.Error);
            return result;
        }

        private void CheckMarker(string content, ValidationResult result)
        {
            if (!content.Contains("<!-- AUTO-GENERATED BELOW"))
            {
                result.Issues.Add(new ValidationIssue
                {
                    Severity = Severity.Error,
                    Message = "Missing auto-generation marker: <!-- AUTO-GENERATED BELOW"
                });
            }
        }

        private void CheckEmptySections(string content, ValidationResult result)
        {
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length - 1; i++)
            {
                string line = lines[i].Trim();
                if ((line.StartsWith("## ") || line.StartsWith("### ")) && !line.Contains("<!--"))
                {
                    string nextLine = i + 1 < lines.Length ? lines[i + 1].Trim() : "";
                    if (string.IsNullOrWhiteSpace(nextLine) || nextLine.StartsWith("#"))
                    {
                        result.Issues.Add(new ValidationIssue
                        {
                            Severity = Severity.Warning,
                            Message = $"Empty section: {line}"
                        });
                    }
                }
            }
        }

        private void CheckMissingSummaries(string content, ValidationResult result)
        {
            var classPattern = new Regex(@"### `.+?\`");
            var matches = classPattern.Matches(content);

            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var lineList = lines.ToList();

            foreach (Match match in matches)
            {
                var lineIdx = lineList.FindIndex(l => l.Contains(match.Value));
                if (lineIdx >= 0 && lineIdx + 1 < lineList.Count)
                {
                    var nextLine = lineList[lineIdx + 1].Trim();
                    if (!nextLine.StartsWith(">") && !nextLine.StartsWith("**Inherits:"))
                    {
                        result.Issues.Add(new ValidationIssue
                        {
                            Severity = Severity.Info,
                            Message = $"Class may be missing summary: {match.Value}"
                        });
                    }
                }
            }
        }

        private void CheckBrokenLinks(string content, ValidationResult result)
        {
            var linkPattern = new Regex(@"\[([^\]]+)\]\(([^)]+)\)");
            var matches = linkPattern.Matches(content);

            foreach (Match match in matches)
            {
                string url = match.Groups[2].Value;

                if (!url.StartsWith("http") && !url.StartsWith("#"))
                {
                    var cleanUrl = url.Split('#')[0];
                    if (!string.IsNullOrEmpty(cleanUrl))
                    {
                        result.Issues.Add(new ValidationIssue
                        {
                            Severity = Severity.Warning,
                            Message = $"Link with local path: {url}"
                        });
                    }
                }
            }
        }
    }
}
