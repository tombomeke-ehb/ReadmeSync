using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ReadmeSync.Models;

namespace ReadmeSync.Services
{
    /// <summary>
    /// Controls how the generated markdown section looks.
    /// </summary>
    public class MarkdownOptions
    {
        /// <summary>Label shown on the "Language" line, for example "C#, Python".</summary>
        public string LanguageLabel { get; set; } = "";

        public bool UseEmojis { get; set; }

        /// <summary>Adds a linked list of namespaces above the details.</summary>
        public bool IncludeToc { get; set; }

        /// <summary>Adds a "Last updated" line.</summary>
        public bool IncludeTimestamp { get; set; } = true;

        /// <summary>Moment shown on the "Last updated" line. Defaults to now.</summary>
        public DateTime? Timestamp { get; set; }
    }

    /// <summary>
    /// Service responsible for generating markdown documentation.
    /// </summary>
    public class MarkdownGenerator
    {
        public const string StartMarker = "<!-- AUTO-GENERATED BELOW – DO NOT EDIT -->";
        public const string EndMarker = "<!-- AUTO-GENERATED END -->";

        private const string StartMarkerPrefix = "<!-- AUTO-GENERATED BELOW";
        private const string EndMarkerPrefix = "<!-- AUTO-GENERATED END";

        private static readonly string[] NamespaceEmojis =
            { "🧱", "⚔️", "🧙", "🏹", "🐉", "🏰", "🧭", "🪄", "🧰", "🎯", "📦", "🧩" };

        private static readonly Regex TimestampLineRegex = new(
            @"^_Last updated: .*_[ \t]*\n", RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// Generates the complete markdown file, preserving manual content above the marker.
        /// </summary>
        public void GenerateMarkdown(
            string outputFile,
            IEnumerable<IGrouping<string, CodeFileInfo>> fileGroups,
            string language,
            string fileExtension,
            bool useEmojis,
            string repoUrl)
        {
            var options = new MarkdownOptions
            {
                LanguageLabel = LanguagePatterns.IsSupported(language) ? LanguagePatterns.For(language).DisplayName : language,
                UseEmojis = useEmojis
            };
            GenerateMarkdown(outputFile, fileGroups, options);
        }

        /// <summary>
        /// Writes the generated section into the output file, keeping everything outside the
        /// markers. Returns false when the file was already up to date and was left untouched.
        /// </summary>
        public bool GenerateMarkdown(
            string outputFile,
            IEnumerable<IGrouping<string, CodeFileInfo>> fileGroups,
            MarkdownOptions options)
        {
            string? existing = File.Exists(outputFile) ? File.ReadAllText(outputFile) : null;
            string document = BuildDocument(existing, fileGroups, options);

            if (existing != null && IsEquivalent(existing, document))
                return false;

            File.WriteAllText(outputFile, document, new UTF8Encoding(false));
            return true;
        }

        /// <summary>
        /// Returns the full document: the existing content with its generated section replaced
        /// (or appended when it has none yet).
        /// </summary>
        public string BuildDocument(
            string? existing,
            IEnumerable<IGrouping<string, CodeFileInfo>> fileGroups,
            MarkdownOptions options)
        {
            string newline = existing != null && existing.Contains("\r\n") ? "\r\n"
                : existing != null && existing.Contains('\n') ? "\n"
                : Environment.NewLine;

            string section = BuildSection(fileGroups, options);
            SplitDocument(existing, out string before, out string after);

            var sb = new StringBuilder();
            if (before.Length > 0)
                sb.Append(before).Append("\n\n");
            sb.Append(section);
            if (after.Length > 0)
                sb.Append('\n').Append(after).Append('\n');

            string document = sb.ToString().Replace("\r\n", "\n");
            return newline == "\n" ? document : document.Replace("\n", newline);
        }

        /// <summary>
        /// Compares two documents while ignoring line endings, trailing whitespace and the
        /// "Last updated" line, so an unchanged code base never produces a diff.
        /// </summary>
        public static bool IsEquivalent(string left, string right) => Normalize(left) == Normalize(right);

        private static string Normalize(string document)
        {
            string text = document.Replace("\r\n", "\n");
            text = TimestampLineRegex.Replace(text + "\n", "");
            return text.TrimEnd();
        }

        /// <summary>
        /// Splits an existing document into the manual parts before and after the generated section.
        /// A document without a marker is manual content as a whole.
        /// </summary>
        private static void SplitDocument(string? existing, out string before, out string after)
        {
            before = "";
            after = "";
            if (string.IsNullOrEmpty(existing))
                return;

            int start = existing.IndexOf(StartMarkerPrefix, StringComparison.Ordinal);
            if (start < 0)
            {
                before = existing.TrimEnd();
                return;
            }

            before = existing[..start].TrimEnd();

            int end = existing.IndexOf(EndMarkerPrefix, start, StringComparison.Ordinal);
            if (end < 0)
                return;

            int close = existing.IndexOf("-->", end, StringComparison.Ordinal);
            if (close >= 0)
                after = existing[(close + 3)..].Trim();
        }

        /// <summary>
        /// Builds the generated section, from start marker to end marker, with \n line endings.
        /// </summary>
        public string BuildSection(IEnumerable<IGrouping<string, CodeFileInfo>> fileGroups, MarkdownOptions options)
        {
            var groups = fileGroups.ToList();
            var sb = new StringBuilder();

            WriteHeader(sb, options);
            WriteStatistics(sb, groups, options.UseEmojis);

            var headings = groups
                .Select((g, i) => (Group: g, Title: (options.UseEmojis ? NamespaceEmojis[i % NamespaceEmojis.Length] + " " : "") + g.Key))
                .ToList();

            if (options.IncludeToc && groups.Count > 0)
                WriteToc(sb, headings);

            foreach (var (group, title) in headings)
            {
                sb.Append("## ").Append(title).Append("\n\n");
                foreach (var type in group)
                    WriteTypeSection(sb, type);
            }

            sb.Append(EndMarker).Append('\n');
            return sb.ToString();
        }

        private static void WriteHeader(StringBuilder sb, MarkdownOptions options)
        {
            string titleEmoji = options.UseEmojis ? "🧮 " : "";

            sb.Append(StartMarker).Append("\n\n");
            sb.Append($"# {titleEmoji}Code Overview (auto-generated)\n\n");
            sb.Append("This section is automatically generated by [ReadmeSync](https://github.com/tombomeke-ehb/ReadmeSync)\n\n");
            sb.Append("Made by tombomeke Studios. To update, run the ReadmeSync tool locally.\n\n");

            if (!string.IsNullOrWhiteSpace(options.LanguageLabel))
                sb.Append($"_Language: **{options.LanguageLabel}**_\n");
            if (options.IncludeTimestamp)
                sb.Append($"_Last updated: **{options.Timestamp ?? DateTime.Now:yyyy-MM-dd HH:mm}**_\n");
            sb.Append('\n');
        }

        private static void WriteStatistics(StringBuilder sb, List<IGrouping<string, CodeFileInfo>> groups, bool useEmojis)
        {
            int typeCount = groups.Sum(g => g.Count());
            int methodCount = groups.Sum(g => g.Sum(t => t.Methods.Count));
            int todoCount = groups.Sum(g => g.Sum(t => t.Todos.Count));
            string statsEmoji = useEmojis ? "📊 " : "";

            sb.Append($"{statsEmoji}**{groups.Count} Packages · {typeCount} Types · {methodCount} Methods · {todoCount} TODOs**\n\n");
        }

        private static void WriteToc(StringBuilder sb, List<(IGrouping<string, CodeFileInfo> Group, string Title)> headings)
        {
            var used = new Dictionary<string, int>();

            sb.Append("**Contents**\n\n");
            foreach (var (group, title) in headings)
            {
                string anchor = Slug(title);
                if (used.TryGetValue(anchor, out int count))
                {
                    used[anchor] = count + 1;
                    anchor = $"{anchor}-{count}";
                }
                else
                {
                    used[anchor] = 1;
                }

                int types = group.Count();
                sb.Append($"- [{EscapeText(group.Key)}](#{anchor}) ({types} {(types == 1 ? "type" : "types")})\n");
            }
            sb.Append('\n');
        }

        /// <summary>
        /// Builds the anchor GitHub generates for a heading: lower case, punctuation dropped,
        /// spaces turned into hyphens.
        /// </summary>
        public static string Slug(string heading)
        {
            var sb = new StringBuilder();
            foreach (char c in heading.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-') sb.Append(c);
                else if (c == ' ') sb.Append('-');
            }
            return sb.ToString();
        }

        private static void WriteTypeSection(StringBuilder sb, CodeFileInfo type)
        {
            string typeLabel = string.IsNullOrEmpty(type.TypeKeyword) ? "class" : type.TypeKeyword;
            sb.Append($"### `{type.Class}` *({typeLabel})*\n\n");

            if (!string.IsNullOrEmpty(type.Path))
            {
                sb.Append(string.IsNullOrEmpty(type.Link)
                    ? $"File: `{type.Path}`\n\n"
                    : $"File: [`{type.Path}`]({type.Link})\n\n");
            }

            if (!string.IsNullOrEmpty(type.Inheritance))
                sb.Append($"**Inherits:** `{type.Inheritance}`\n\n");

            if (!string.IsNullOrEmpty(type.Summary))
                sb.Append($"> {type.Summary}\n\n");

            if (type.Methods.Count > 0)
            {
                sb.Append("**Public Methods:**\n");
                foreach (string method in type.Methods)
                    sb.Append($"- `{method}()`\n");
            }
            else
            {
                sb.Append("_No public methods found._\n");
            }

            if (type.Todos.Count > 0)
            {
                sb.Append("\n**TODOs:**\n");
                foreach (string todo in type.Todos)
                    sb.Append($"- [ ] {todo}\n");
            }

            sb.Append('\n');
        }

        private static string EscapeText(string text) => text.Replace("[", "\\[").Replace("]", "\\]");
    }
}
