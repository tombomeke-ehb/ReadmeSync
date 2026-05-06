using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ReadmeSync.Models;

namespace ReadmeSync.Services
{
    /// <summary>
    /// Service responsible for analyzing source code and extracting metadata.
    /// </summary>
    public class CodeAnalyzer
    {
        /// <summary>
        /// Analyzes source code text and extracts namespace, class, methods, TODOs, and documentation.
        /// </summary>
        public CodeFileInfo? AnalyzeCode(string text, LanguagePatterns patterns, string language, bool includePrivate = false)
        {
            // Extract namespace/package
            string ns = ExtractNamespace(text, patterns, language);

            // Extract class information
            var classMatch = Regex.Match(text, patterns.Class, RegexOptions.Compiled);

            string typeKeyword, cls, inheritance;

            // Python has different group structure: class name is in group 1, inheritance in group 2
            if (language == "python")
            {
                typeKeyword = "class";
                cls = classMatch.Groups[1].Value.Trim();
                inheritance = classMatch.Groups[2].Value.Trim();
            }
            else if (language == "php")
            {
                typeKeyword = classMatch.Groups[1].Value.Trim();
                cls = classMatch.Groups[2].Value.Trim();
                string extends = classMatch.Groups[3].Value.Trim();
                string implements = classMatch.Groups[4].Value.Trim();

                var inheritList = new List<string>();
                if (!string.IsNullOrEmpty(extends)) inheritList.Add(extends);
                if (!string.IsNullOrEmpty(implements)) inheritList.Add(implements);

                inheritance = string.Join(", ", inheritList);
            }
            else
            {
                typeKeyword = classMatch.Groups[1].Value.Trim();
                cls = classMatch.Groups[2].Value.Trim();
                inheritance = classMatch.Groups[3].Value.Trim();
            }

            // Validation: both namespace and class name are required
            if (string.IsNullOrWhiteSpace(ns) || string.IsNullOrWhiteSpace(cls))
                return null;

            // Clean up inheritance string
            inheritance = CleanInheritance(inheritance, language);

            // Extract documentation summary
            string summary = ExtractSummary(text, patterns, language);

            // Extract public (and optionally private) methods
            var methods = ExtractMethods(text, patterns, cls, includePrivate);

            // Extract TODO comments
            var todos = ExtractTodos(text);

            return new CodeFileInfo
            {
                Namespace = ns,
                TypeKeyword = typeKeyword,
                Class = cls,
                Inheritance = inheritance,
                Summary = summary,
                Methods = methods,
                Todos = todos
            };
        }

        private string ExtractNamespace(string text, LanguagePatterns patterns, string language)
        {
            var match = Regex.Match(text, patterns.Namespace, RegexOptions.Compiled);
            string ns = match.Groups[1].Value.Trim();

            // For Python: if no explicit package comment, use "default" or derive from file path
            if (string.IsNullOrWhiteSpace(ns) && language == "python")
            {
                ns = "default";
            }

            // For TypeScript/JavaScript: if no explicit namespace, use "global" or module name
            if (string.IsNullOrWhiteSpace(ns) && (language == "typescript" || language == "javascript"))
            {
                ns = "global";
            }

            return ns;
        }

        private string CleanInheritance(string inheritance, string language)
        {
            if (string.IsNullOrWhiteSpace(inheritance))
                return "";

            if (inheritance.StartsWith(":"))
                inheritance = inheritance.Substring(1).Trim();

            // For TypeScript/JavaScript, clean up "extends X" to just "X"
            if (language == "typescript" || language == "javascript")
            {
                inheritance = inheritance.Replace("extends", "").Trim();
                inheritance = inheritance.Replace("implements", "").Trim();
            }

            int whereIdx = inheritance.IndexOf("where");
            if (whereIdx >= 0)
                inheritance = inheritance.Substring(0, whereIdx).Trim();

            return inheritance;
        }

        private string ExtractSummary(string text, LanguagePatterns patterns, string language)
        {
            var summaryMatch = Regex.Match(text, patterns.Summary, RegexOptions.Compiled | RegexOptions.Singleline);
            if (!summaryMatch.Success)
                return "";

            string summary = summaryMatch.Groups[1].Value;

            // Language-specific cleanup
            if (language == "csharp")
                summary = Regex.Replace(summary, @"///\s?", "").Trim();
            else if (language == "java" || language == "typescript" || language == "javascript" || language == "php")
                summary = Regex.Replace(summary, @"\*\s?", "").Trim();
            else if (language == "python")
            {
                // Clean up Python docstrings
                summary = summary.Trim();
                // Remove leading comma and whitespace if present
                if (summary.StartsWith(","))
                    summary = summary.Substring(1).Trim();
            }

            // Normalize whitespace
            summary = Regex.Replace(summary, @"\s+", " ").Trim();

            return summary;
        }

        private List<string> ExtractMethods(string text, LanguagePatterns patterns, string className, bool includePrivate = false)
        {
            var methods = new List<string>();

            methods.AddRange(
                Regex.Matches(text, patterns.Method, RegexOptions.Compiled)
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value)
                    .Where(m => m != className && !string.IsNullOrWhiteSpace(m))
                    .Distinct()
            );

            if (includePrivate)
            {
                var privatePattern = @"(?:private|protected)\s+[A-Za-z0-9_<>,\[\]\s]+\s+([A-Za-z0-9_]+)\s*\(";
                methods.AddRange(
                    Regex.Matches(text, privatePattern, RegexOptions.Compiled)
                        .Cast<Match>()
                        .Select(m => $"[private] {m.Groups[1].Value}")
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .Distinct()
                );
            }

            return methods;
        }

        private List<string> ExtractTodos(string text)
        {
            return Regex.Matches(text, @"(?://|#)\s*TODO[: ](.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value.Trim())
                .ToList();
        }
    }
}
