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
        private static readonly HashSet<string> ReservedNames = new(StringComparer.Ordinal)
        {
            "if", "else", "for", "foreach", "while", "do", "switch", "case", "catch", "try", "finally",
            "return", "throw", "new", "typeof", "sizeof", "nameof", "await", "yield", "lock", "using",
            "when", "where", "with", "function", "constructor", "super", "this", "extends", "implements",
            "operator", "delegate", "in", "is", "as", "not", "and", "or"
        };

        private static readonly Regex TodoRegex = new(
            @"^(?<tag>(?i:TODO)|FIXME|HACK)\b(?:\s*\([^)]*\))?\s*[:\-]?\s*(?<text>.*)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex CommentPrefixRegex = new(
            @"^\s*(?://+!?|#+!?|/\*+|\*+(?!/))?\s*",
            RegexOptions.Compiled);

        private static readonly Regex DecoratorRegex = new(
            @"@[\w.]+(?:\([^()]*(?:\([^()]*\)[^()]*)*\))?|#?\[[^\]]*\]",
            RegexOptions.Compiled);

        private static readonly Regex PythonDocstringRegex = new(
            "\\G\\s*[rRuUbB]{0,2}(?:\"\"\"(?<doc>.*?)\"\"\"|'''(?<doc>.*?)''')",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex XmlCrefRegex = new(
            @"<(?:see|paramref|typeparamref)\s+(?:cref|name|langword)\s*=\s*""(?:[A-Z]:)?([^""]+)""\s*/?>",
            RegexOptions.Compiled);

        private static readonly Regex NonPublicModifierRegex = new(
            @"\b(?:private|protected)\s+(?:[a-z]+\s+)*$",
            RegexOptions.Compiled | RegexOptions.RightToLeft);

        private static readonly Regex XmlTagRegex = new(@"</?[A-Za-z][^>]*>", RegexOptions.Compiled);

        /// <summary>
        /// Analyzes source code text and returns the first type found in it,
        /// or null when the file holds no documentable type.
        /// </summary>
        public CodeFileInfo? AnalyzeCode(string text, LanguagePatterns patterns, string language)
        {
            return AnalyzeAll(text, patterns, language).FirstOrDefault();
        }

        /// <summary>
        /// Analyzes source code text and returns every type found in it, in source order,
        /// with its namespace, public methods, documentation and TODO notes.
        /// </summary>
        public IReadOnlyList<CodeFileInfo> AnalyzeAll(string text, LanguagePatterns patterns, string language)
        {
            var result = new List<CodeFileInfo>();
            if (string.IsNullOrWhiteSpace(text))
                return result;

            language = LanguagePatterns.Normalize(language) ?? patterns.Name;
            var source = SourceMasker.Mask(text, language);

            var types = FindTypes(source, patterns, language);
            if (types.Count == 0)
                return result;

            AssignNamespaces(types, source, patterns, language);
            types.RemoveAll(t => string.IsNullOrWhiteSpace(t.Info.Namespace));
            if (types.All(t => t.Hidden))
                return result;

            var typeNames = new HashSet<string>(types.Select(t => t.Info.Class), StringComparer.Ordinal);

            foreach (var type in types)
                type.Info.Summary = ExtractSummary(type, source, patterns, language);

            AssignMethods(types, typeNames, source, patterns, language);
            AssignTodos(types, source);

            result.AddRange(types.Where(t => !t.Hidden).Select(t => t.Info));
            return result;
        }

        // ------------------------------------------------------------------
        // Types
        // ------------------------------------------------------------------

        private sealed class TypeEntry
        {
            public required CodeFileInfo Info { get; init; }
            public required Match Match { get; init; }
            public int Start => Match.Index;
            public int End { get; set; }
            public HashSet<string> MethodSet { get; } = new(StringComparer.Ordinal);

            /// <summary>Private and protected types still own their members, but are not reported.</summary>
            public bool Hidden { get; init; }
        }

        private static List<TypeEntry> FindTypes(MaskedSource source, LanguagePatterns patterns, string language)
        {
            var types = new List<TypeEntry>();

            foreach (Match match in patterns.TypeRegex.Matches(source.Code))
            {
                string name = match.Groups["name"].Value.Trim();
                if (name.Length == 0 || ReservedNames.Contains(name))
                    continue;

                string kind = match.Groups["kind"].Success
                    ? Regex.Replace(match.Groups["kind"].Value.Trim(), @"\s+", " ")
                    : "class";

                types.Add(new TypeEntry
                {
                    Match = match,
                    Hidden = language != "python" && NonPublicModifierRegex.IsMatch(source.Code[..match.Index]),
                    Info = new CodeFileInfo
                    {
                        TypeKeyword = kind,
                        Class = name,
                        Inheritance = BuildInheritance(match, language),
                        Language = language,
                        Line = source.LineOf(match.Index)
                    }
                });
            }

            for (int i = 0; i < types.Count; i++)
            {
                int nextStart = i + 1 < types.Count ? types[i + 1].Start : source.Code.Length;
                types[i].End = language == "python"
                    ? FindIndentedBlockEnd(source.Code, types[i].Start)
                    : FindBraceBlockEnd(source.Code, types[i].Start, nextStart, language == "kotlin");
            }

            return types;
        }

        /// <summary>
        /// Finds where the body of a brace-delimited type ends. A declaration without a body
        /// ends at its semicolon, or at the start of the next type.
        /// </summary>
        private static int FindBraceBlockEnd(string code, int start, int nextTypeStart, bool newlineEndsHeader)
        {
            int parens = 0;
            int open = -1;

            for (int i = start; i < code.Length; i++)
            {
                char c = code[i];
                if (c == '(') parens++;
                else if (c == ')') parens--;
                else if (parens <= 0)
                {
                    if (c == '{') { open = i; break; }
                    if (c == ';') return i;
                    if (i >= nextTypeStart && i > start) return nextTypeStart;
                    if (c == '\n' && newlineEndsHeader && !HeaderContinues(code, start, i)) return i;
                }
            }

            if (open < 0)
                return code.Length;

            int depth = 0;
            for (int i = open; i < code.Length; i++)
            {
                if (code[i] == '{') depth++;
                else if (code[i] == '}' && --depth == 0) return i + 1;
            }
            return code.Length;
        }

        private static bool HeaderContinues(string code, int start, int newlineIndex)
        {
            for (int i = newlineIndex - 1; i > start; i--)
            {
                if (!char.IsWhiteSpace(code[i]))
                    return code[i] is ',' or ':';
            }
            return false;
        }

        /// <summary>
        /// Finds where an indentation-delimited (Python) class ends: the first later line
        /// that is not indented deeper than the class statement.
        /// </summary>
        private static int FindIndentedBlockEnd(string code, int start)
        {
            int lineStart = code.LastIndexOf('\n', Math.Max(start - 1, 0)) + 1;
            if (start == 0) lineStart = 0;
            int classIndent = IndentWidth(code, lineStart, out _);

            int pos = code.IndexOf('\n', start);
            while (pos >= 0 && pos + 1 < code.Length)
            {
                int next = pos + 1;
                int indent = IndentWidth(code, next, out bool blank);
                if (!blank && indent <= classIndent)
                    return next;
                pos = code.IndexOf('\n', next);
            }
            return code.Length;
        }

        private static int IndentWidth(string code, int lineStart, out bool blank)
        {
            int width = 0;
            int i = lineStart;
            while (i < code.Length && (code[i] == ' ' || code[i] == '\t'))
            {
                width += code[i] == '\t' ? 4 : 1;
                i++;
            }
            blank = i >= code.Length || code[i] == '\n' || code[i] == '\r';
            return width;
        }

        private static string BuildInheritance(Match match, string language)
        {
            if (match.Groups["extends"].Success || match.Groups["implements"].Success)
            {
                var parts = new[] { match.Groups["extends"].Value, match.Groups["implements"].Value }
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => Regex.Replace(p.Trim(), @"\s*,\s*", ", "));
                return string.Join(", ", parts);
            }

            return CleanInheritance(match.Groups["inherit"].Value, language);
        }

        private static string CleanInheritance(string inheritance, string language)
        {
            inheritance = inheritance.Trim();
            if (inheritance.Length == 0)
                return "";

            // Primary constructor / record parameters: "(int Id, string Name) : Base"
            if (inheritance.StartsWith('('))
            {
                int close = MatchingParen(inheritance, 0);
                inheritance = close < 0 ? "" : inheritance[(close + 1)..].Trim();
            }

            if (inheritance.StartsWith(':'))
                inheritance = inheritance[1..].Trim();

            if (language is "csharp" or "kotlin")
            {
                // Base constructor arguments: "Base(arg), IFoo"
                inheritance = Regex.Replace(inheritance, @"\([^()]*\)", "");
            }

            if (language == "csharp")
            {
                var where = Regex.Match(inheritance, @"\bwhere\b");
                if (where.Success)
                    inheritance = inheritance[..where.Index];
            }

            if (language is "typescript" or "javascript")
            {
                inheritance = Regex.Replace(inheritance, @"\s*\bimplements\b\s*", ", ");
                inheritance = Regex.Replace(inheritance, @"\bextends\b", "");
            }

            inheritance = Regex.Replace(inheritance, @"\s+", " ").Trim();
            return inheritance.Trim(',', ';', ' ');
        }

        private static int MatchingParen(string text, int open)
        {
            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                if (text[i] == '(') depth++;
                else if (text[i] == ')' && --depth == 0) return i;
            }
            return -1;
        }

        // ------------------------------------------------------------------
        // Namespaces
        // ------------------------------------------------------------------

        private static void AssignNamespaces(List<TypeEntry> types, MaskedSource source, LanguagePatterns patterns, string language)
        {
            // Python declares its package in a comment, which the masked code no longer has.
            string haystack = language == "python" ? source.Original : source.Code;

            var namespaces = patterns.NamespaceRegex.Matches(haystack)
                .Select(m => (m.Index, Name: m.Groups[1].Value.Trim()))
                .Where(n => n.Name.Length > 0)
                .ToList();

            foreach (var type in types)
            {
                string ns = patterns.FallbackNamespace;
                if (namespaces.Count > 0)
                {
                    var preceding = namespaces.Where(n => n.Index <= type.Start).ToList();
                    ns = preceding.Count > 0 ? preceding[^1].Name : namespaces[0].Name;
                }
                type.Info.Namespace = ns;
            }
        }

        // ------------------------------------------------------------------
        // Documentation
        // ------------------------------------------------------------------

        private static string ExtractSummary(TypeEntry type, MaskedSource source, LanguagePatterns patterns, string language)
        {
            string summary;

            if (language == "python")
            {
                var doc = PythonDocstringRegex.Match(source.Original, type.Match.Index + type.Match.Length);
                if (!doc.Success)
                    return "";
                summary = doc.Groups["doc"].Value;
            }
            else if (language == "go")
            {
                summary = PrecedingLineComments(source.Original, type.Start);
            }
            else
            {
                // The doc comment that directly precedes the declaration: only modifiers,
                // attributes and annotations may sit between the two.
                Match? best = null;
                foreach (Match candidate in patterns.SummaryRegex.Matches(source.Original))
                {
                    int end = candidate.Index + candidate.Length;
                    if (end > type.Start) break;
                    best = candidate;
                }
                if (best == null)
                    return "";

                int gapStart = best.Index + best.Length;
                string gap = DecoratorRegex.Replace(source.Code[gapStart..type.Start], "");
                if (gap.IndexOfAny(new[] { ';', '{', '}', '(', ')', '=' }) >= 0 ||
                    Regex.IsMatch(gap, @"\b(?:import|using|package|namespace|require|from)\b"))
                    return "";

                summary = best.Groups[1].Value;
            }

            if (language == "csharp")
            {
                summary = Regex.Replace(summary, @"^\s*///\s?", "", RegexOptions.Multiline);
                summary = XmlCrefRegex.Replace(summary, "$1");
                summary = XmlTagRegex.Replace(summary, "");
            }
            else if (language is "java" or "kotlin" or "typescript" or "javascript" or "php")
            {
                summary = Regex.Replace(summary, @"^[ \t]*\*+[ \t]?", "", RegexOptions.Multiline);
                // Drop block tags (@param, @return, ...) and everything after them.
                var tag = Regex.Match(summary, @"^\s*@\w+", RegexOptions.Multiline);
                if (tag.Success)
                    summary = summary[..tag.Index];
            }

            return Regex.Replace(summary, @"\s+", " ").Trim();
        }

        private static string PrecedingLineComments(string text, int declarationStart)
        {
            var lines = new List<string>();
            int lineStart = text.LastIndexOf('\n', Math.Max(declarationStart - 1, 0)) + 1;

            while (lineStart > 0)
            {
                int prevEnd = lineStart - 1;
                int prevStart = prevEnd == 0 ? 0 : text.LastIndexOf('\n', prevEnd - 1) + 1;
                string line = text[prevStart..prevEnd].Trim();
                if (!line.StartsWith("//"))
                    break;
                lines.Insert(0, line.TrimStart('/').Trim());
                lineStart = prevStart;
            }

            return string.Join(" ", lines);
        }

        // ------------------------------------------------------------------
        // Methods
        // ------------------------------------------------------------------

        private static void AssignMethods(
            List<TypeEntry> types, HashSet<string> typeNames, MaskedSource source, LanguagePatterns patterns, string language)
        {
            foreach (Match match in patterns.MethodRegex.Matches(source.Code))
            {
                string name = match.Groups["name"].Value;
                if (!IsMethodName(name, typeNames, language))
                    continue;
                if (language == "csharp" && Regex.IsMatch(match.Value, @"\b(?:operator|delegate)\b"))
                    continue;

                TypeEntry? owner;
                if (match.Groups["recv"].Success)
                    owner = types.FirstOrDefault(t => t.Info.Class == match.Groups["recv"].Value);
                else if (language == "go")
                    owner = types.LastOrDefault(t => t.Start <= match.Index) ?? types[0];
                else
                    owner = Innermost(types, match.Groups["name"].Index);

                AddMethod(owner, name);
            }

            var interfaceRegex = patterns.InterfaceMethodRegex;
            if (interfaceRegex == null)
                return;

            foreach (var type in types.Where(t => t.Info.TypeKeyword.Contains("interface")))
            {
                string body = source.Code[type.Start..type.End];
                foreach (Match match in interfaceRegex.Matches(body))
                {
                    string name = match.Groups["name"].Value;
                    if (!IsMethodName(name, typeNames, language))
                        continue;
                    if (match.Groups["ret"].Success && ReservedNames.Contains(match.Groups["ret"].Value))
                        continue;
                    if (Innermost(types, type.Start + match.Groups["name"].Index) != type)
                        continue;

                    AddMethod(type, name);
                }
            }
        }

        private static void AddMethod(TypeEntry? owner, string name)
        {
            if (owner != null && owner.MethodSet.Add(name))
                owner.Info.Methods.Add(name);
        }

        private static bool IsMethodName(string name, HashSet<string> typeNames, string language)
        {
            if (string.IsNullOrWhiteSpace(name) || ReservedNames.Contains(name) || typeNames.Contains(name))
                return false;

            // Leading underscores mark private members in Python and magic methods in PHP.
            if (language == "python" && name.StartsWith('_')) return false;
            if (language == "php" && name.StartsWith("__")) return false;

            return true;
        }

        private static TypeEntry? Innermost(List<TypeEntry> types, int index)
        {
            TypeEntry? best = null;
            foreach (var type in types)
            {
                if (index >= type.Start && index < type.End && (best == null || type.Start > best.Start))
                    best = type;
            }
            return best;
        }

        // ------------------------------------------------------------------
        // TODOs
        // ------------------------------------------------------------------

        private static void AssignTodos(List<TypeEntry> types, MaskedSource source)
        {
            foreach (var comment in source.Comments)
            {
                int offset = 0;
                foreach (string rawLine in comment.Text.Split('\n'))
                {
                    int lineStart = comment.Start + offset;
                    offset += rawLine.Length + 1;

                    string line = CommentPrefixRegex.Replace(rawLine.TrimEnd('\r'), "", 1);
                    var match = TodoRegex.Match(line);
                    if (!match.Success)
                        continue;

                    string text = match.Groups["text"].Value.Trim();
                    if (text.EndsWith("*/"))
                        text = text[..^2].TrimEnd();
                    if (text.Length == 0)
                        continue;

                    string tag = match.Groups["tag"].Value.ToUpperInvariant();
                    var visible = types.Where(t => !t.Hidden).ToList();
                    var owner = Innermost(visible, lineStart)
                        ?? visible.LastOrDefault(t => t.Start <= lineStart)
                        ?? visible[0];

                    owner.Info.Todos.Add(tag == "TODO" ? text : $"{tag}: {text}");
                    owner.Info.TodoItems.Add(new TodoItem { Tag = tag, Text = text, Line = source.LineOf(lineStart) });
                }
            }
        }
    }
}
