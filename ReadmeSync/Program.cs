using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

// ============================================================================
// ReadmeSync.cs
// ----------------------------------------------------------------------------
// A command-line tool that scans your source code and auto-generates or updates
// a README or ROADMAP file based on namespaces/packages, classes, public methods,
// and // TODO comments.
//
// Features
// - Supports multiple languages (currently: C# and Java)
// - Detects repository root automatically (.git, .sln, README.md, LICENSE)
// - Keeps manual content above marker intact
// - Creates structured markdown summaries of the codebase
// - Supports optional GitHub URL linking
//
// Usage
//   readmesync [--lang csharp|java] [scan-root] [output-file] [optional-repo-url]
//
// Example:
//   readmesync --lang java ./src README.md https://github.com/tombomeke-ehb/ReadmeSync
//
// Notes
// - Safe to run multiple times; it only replaces content *below* the marker
// - Compatible with .NET 8.0+
// - Emojis, patterns, and markdown output are fully customizable
//
// Future improvements
// - Add JSON config file (readmesync.json)
// - Add folder exclusion rules
// - Support for Python / TypeScript
// - Richer syntax highlighting or tree views
//
// © 2025 Tombomeke Studios — All rights reserved
// ============================================================================

namespace ReadmeSync
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("🛠️ ReadmeSync – Automatically update README or ROADMAP with code overview");
            Console.WriteLine("Made by tombomeke Studios");
            Console.ResetColor();
            Console.WriteLine("Made by tombomeke studios");
            Console.WriteLine("--------------------------------------------------------------------------\n");

            try
            {
                // ============================================================
                // 1️ Parse CLI Arguments
                // ============================================================
                if (args.Length == 0)
                {
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  readmesync [--lang csharp|java] [scan-root] [output-file] [optional-repo-url]");
                    Console.WriteLine("\nExamples:");
                    Console.WriteLine("  readmesync . README.md");
                    Console.WriteLine("  readmesync --lang java ./src ROADMAP.md https://github.com/USER/REPO\n");
                    return;
                }

                // ------------------------------------------------------------
                // Detect optional --lang flag
                // ------------------------------------------------------------
                string language = "csharp";
                int langIndex = Array.IndexOf(args, "--lang");
                if (langIndex != -1 && langIndex + 1 < args.Length)
                {
                    language = args[langIndex + 1].ToLowerInvariant();
                    args = args.Where((x, i) => i != langIndex && i != langIndex + 1).ToArray();
                }

                // ============================================================
                // 2️ Prepare Paths and Repo Info
                // ============================================================
                // Safely handle different argument orders and directories
                string scanRoot = ".";
                if (args.Length > 0 && Directory.Exists(args[0]))
                    scanRoot = args[0];
                else if (args.Length == 0)
                    scanRoot = ".";
                else if (args.Length > 1 && Directory.Exists(args[1]))
                    scanRoot = args[1];

                scanRoot = Path.GetFullPath(scanRoot);
                string repoRoot = FindRepoRootNearest(scanRoot, out var reason) ?? scanRoot;

                string outputFile = args.Length > 1
                    ? (Path.IsPathRooted(args[1]) ? args[1] : Path.Combine(repoRoot, args[1]))
                    : Path.Combine(repoRoot, "README.md");

                Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

                // Do NOT default to [YOUR_REPOSITORY_URL_HERE]
                string repoUrl = args.Length > 2 ? args[2].TrimEnd('/') : string.Empty;

                // ============================================================
                // 3️ Language Configuration (Regex Patterns)
                // ============================================================
                var patterns = LanguagePatterns.For(language);
                string fileExt = patterns.Extension;

                Console.WriteLine($"📦 Language: {language}");
                Console.WriteLine($"📂 Scanning directory: {scanRoot}");
                Console.WriteLine($"📁 Repo root:         {repoRoot}");
                Console.WriteLine($"🔎 Detected by:       {reason ?? "(no marker; using scanRoot)"}");
                Console.WriteLine($"📝 Output file:       {outputFile}\n");

                // ============================================================
                // 4️ Scan Files
                // ============================================================
                var codeFiles = Directory.GetFiles(scanRoot, $"*{fileExt}", SearchOption.AllDirectories);
                if (codeFiles.Length == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"⚠️ No {fileExt} files found in project.");
                    Console.ResetColor();
                    return;
                }

                // ============================================================
                // 5️ Analyze Source Files
                // ============================================================
                var files = codeFiles.Select(f =>
                {
                    string text = File.ReadAllText(f);

                    // ------------------------------------------------------------
                    // Regex: Namespace / Package Extraction
                    // ------------------------------------------------------------
                    // Matches:
                    //  - `namespace MyApp.Core`  (C#)
                    //  - `package com.example.app;`  (Java)
                    //
                    // Explanation:
                    //  - `namespace|package` → keyword to match
                    //  - `\s+` → one or more spaces
                    //  - `([A-Za-z0-9_.]+)` → capture valid namespace/package name
                    //    containing letters, digits, underscores, and dots.
                    string ns = Regex.Match(text, patterns.Namespace, RegexOptions.Compiled).Groups[1].Value.Trim();

                    // ------------------------------------------------------------
                    // Regex: Class Extraction
                    // ------------------------------------------------------------
                    var classMatch = Regex.Match(text, patterns.Class, RegexOptions.Compiled);
                    string typeKeyword = classMatch.Groups[1].Value.Trim();
                    string cls = classMatch.Groups[2].Value.Trim();
                    string inheritance = classMatch.Groups[3].Value.Trim();

                    if (string.IsNullOrWhiteSpace(ns) || string.IsNullOrWhiteSpace(cls))
                        return null;

                    if (inheritance.StartsWith(":")) 
                        inheritance = inheritance.Substring(1).Trim();
                    
                    int whereIdx = inheritance.IndexOf("where");
                    if (whereIdx >= 0) 
                        inheritance = inheritance.Substring(0, whereIdx).Trim();

                    // ------------------------------------------------------------
                    // Regex: Summary Extraction
                    // ------------------------------------------------------------
                    string summary = "";
                    var summaryMatch = Regex.Match(text, patterns.Summary, RegexOptions.Compiled | RegexOptions.Singleline);
                    if (summaryMatch.Success)
                    {
                        summary = summaryMatch.Groups[1].Value;
                        if (language == "csharp")
                        {
                            summary = Regex.Replace(summary, @"///\s?", "").Trim();
                        }
                        else if (language == "java")
                        {
                            summary = Regex.Replace(summary, @"\*\s?", "").Trim();
                        }
                        summary = Regex.Replace(summary, @"\s+", " ").Trim();
                    }

                    // ------------------------------------------------------------
                    // Regex: Public Method Extraction
                    // ------------------------------------------------------------
                    // Matches:
                    //  - `public void Attack()`  (C# / Java)
                    //  - `public int getHealth()`  (Java)
                    //
                    // Explanation:
                    //  - `public` → ensures only public methods are captured
                    //  - `[A-Za-z0-9_<>,\[\]\s]+` → matches return type
                    //  - `([A-Za-z0-9_]+)` → captures the method name
                    //  - `\s*\(` → ensures it’s actually a function
                    //
                    // NOTE: Constructors with the same name as the class are ignored below.
                    var methods = Regex.Matches(text, patterns.Method, RegexOptions.Compiled)
                        .Cast<Match>()
                        .Select(m => m.Groups[1].Value)
                        .Where(m => m != cls)
                        .Distinct()
                        .ToList();

                    // ------------------------------------------------------------
                    // Regex: TODO Comment Extraction
                    // ------------------------------------------------------------
                    // Matches:
                    //  - `// TODO: refactor this`
                    //  - `// todo fix null check`
                    //
                    // Explanation:
                    //  - `//\s*TODO[: ](.*)` → finds "// TODO" with any spacing or case
                    //  - `(.*)` → captures the remainder of the line
                    var todos = Regex.Matches(text, @"//\s*TODO[: ](.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
                        .Cast<Match>()
                        .Select(m => m.Groups[1].Value.Trim())
                        .ToList();

                    // ------------------------------------------------------------
                    // Link construction
                    // ------------------------------------------------------------
                    // Converts full file paths into relative URLs for GitHub/GitLab/etc.
                    // Only create clickable links if repoUrl is valid (http/https)
                    string rel = Path.GetRelativePath(repoRoot, f).Replace(Path.DirectorySeparatorChar, '/');
                    string? fileUrl = repoUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? $"{repoUrl}/{rel}"
                        : null;

                    return new
                    {
                        Namespace = ns,
                        TypeKeyword = typeKeyword,
                        Class = cls,
                        Inheritance = inheritance,
                        Summary = summary,
                        Methods = methods,
                        Todos = todos,
                        Path = rel,
                        Link = fileUrl
                    };
                })
                .Where(f => f != null)
                .GroupBy(f => f.Namespace)
                .OrderBy(g => g.Key)
                .ToList();

                // ============================================================
                // 6️ Compute Summary Statistics
                // ============================================================
                int nsCount = files.Count;
                int classCount = files.Sum(g => g.Count());
                int methodCount = files.Sum(g => g.SelectMany(c => c.Methods).Count());
                int todoCount = files.Sum(g => g.SelectMany(c => c.Todos).Count());

                // ============================================================
                // 7️ Preserve Manual Section
                // ============================================================
                string manual = "";
                if (File.Exists(outputFile))
                {
                    string existing = File.ReadAllText(outputFile);
                    int marker = existing.IndexOf("<!-- AUTO-GENERATED BELOW");
                    if (marker >= 0)
                        manual = existing[..marker].TrimEnd() + "\n\n";
                }

                // ============================================================
                // 8️ Write Auto-Generated Markdown
                // ============================================================
                using var sw = new StreamWriter(outputFile, false);
                sw.WriteLine(manual);
                sw.WriteLine("<!-- AUTO-GENERATED BELOW – DO NOT EDIT -->");
                sw.WriteLine($"\n# 🧮 Code Overview (auto-generated)\n");
                sw.WriteLine("This section is automatically generated by [ReadmeSync](https://github.com/tombomeke-ehb/ReadmeSync)\n");
                sw.WriteLine("Made by tombomeke Studios. To update, run the ReadmeSync tool locally.\n");
                sw.WriteLine($"_Language: **{language.ToUpper()}**_");
                sw.WriteLine($"_Last updated: **{DateTime.Now:yyyy-MM-dd HH:mm}**_\n");
                sw.WriteLine($"📊 **{nsCount} Packages · {classCount} Types · {methodCount} Methods · {todoCount} TODOs**\n");
                sw.WriteLine("");
                sw.WriteLine("Generated with ReadmeSync made by tombomeke");

                // ============================================================
                // Namespace Emojis
                // ============================================================
                string[] emojis = { "🧱", "⚔️", "🧙", "🏹", "🐉", "🏰", "🧭", "🪄", "🧰", "🎯", "📦", "🧩" };
                int eIndex = 0;

                foreach (var nsGroup in files)
                {
                    string nsEmoji = emojis[eIndex++ % emojis.Length];
                    sw.WriteLine($"\n## {nsEmoji} {nsGroup.Key}\n");

                    foreach (var file in nsGroup)
                    {
                        // Only clickable if valid link, else inline code
                        string typeLabel = string.IsNullOrEmpty(file.TypeKeyword) ? "class" : file.TypeKeyword;
                        if (!string.IsNullOrEmpty(file.Link))
                            sw.WriteLine($"### [{file.Class}{fileExt}]({file.Link}) *({typeLabel})*");
                        else
                            sw.WriteLine($"### `{file.Class}{fileExt}` *({typeLabel})*");

                        if (!string.IsNullOrEmpty(file.Inheritance))
                            sw.WriteLine($"**Inherits:** `{file.Inheritance}`\n");

                        if (!string.IsNullOrEmpty(file.Summary))
                            sw.WriteLine($"> {file.Summary}\n");

                        if (file.Methods.Any())
                        {
                            sw.WriteLine("**Public Methods:**");
                            foreach (var m in file.Methods)
                                sw.WriteLine($"- `{m}()`");
                        }
                        else
                            sw.WriteLine("_No public methods found._");

                        if (file.Todos.Any())
                        {
                            sw.WriteLine("\n**TODOs:**");
                            foreach (var todo in file.Todos)
                                sw.WriteLine($"- [ ] {todo}");
                        }

                        sw.WriteLine();
                    }
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✅ ReadmeSync completed successfully!");
                Console.ResetColor();
                Console.WriteLine($"📁 Output file: {Path.GetFullPath(outputFile)}\n");
            }

            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ An unexpected error occurred:");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }
        }

        // ============================================================
        // Repo Root Finder
        // ============================================================
        /// <summary>
        /// Recursively searches upward from the given path to locate the
        /// most likely repository root (preferring .git, solution, or README).
        /// </summary>
        private static string? FindRepoRootNearest(string startPath, out string? reason)
        {
            reason = null;
            var dir = new DirectoryInfo(Path.GetFullPath(startPath));

            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                {
                    reason = $".git at {dir.FullName}";
                    return dir.FullName;
                }

                if (Directory.EnumerateFiles(dir.FullName, "*.sln").Any())
                {
                    reason = $"solution (*.sln) at {dir.FullName}";
                    return dir.FullName;
                }

                if (File.Exists(Path.Combine(dir.FullName, "README.md")) ||
                    File.Exists(Path.Combine(dir.FullName, "LICENSE")) ||
                    Directory.Exists(Path.Combine(dir.FullName, ".github")))
                {
                    reason = $"marker file at {dir.FullName}";
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            return null;
        }
    }

    // ============================================================
    // Language Patterns (Extensible)
    // ============================================================
    /// <summary>
    /// Defines the regex profiles for different languages.
    /// Each profile includes:
    ///  - Namespace/Package pattern
    ///  - Class pattern
    ///  - Public method pattern
    ///  - File extension
    ///
    /// To extend:
    /// Add a new case (e.g. "python") and specify patterns accordingly.
    /// </summary>
    internal class LanguagePatterns
    {
        public string Namespace { get; }
        public string Class { get; }
        public string Method { get; }
        public string Extension { get; }
        public string Summary { get; }

        private LanguagePatterns(string ns, string cls, string method, string ext, string summary)
        {
            Namespace = ns;
            Class = cls;
            Method = method;
            Extension = ext;
            Summary = summary;
        }

        public static LanguagePatterns For(string lang)
        {
            return lang switch
            {
                "java" => new LanguagePatterns(
                    @"package\s+([A-Za-z0-9_.]+)",                  // Matches `package com.example.app`
                    @"(class|interface|enum|record)\s+([A-Za-z0-9_]+)\s*([^{]*)", // Matches `class Player extends Entity`
                    @"public\s+[A-Za-z0-9_<>,\[\]\s]+\s+([A-Za-z0-9_]+)\s*\(", // Matches `public void attack(`
                    ".java",
                    @"/\*\*(.*?)\*/"                                // Matches Javadoc
                ),

                _ => new LanguagePatterns( // Default: C#
                    @"namespace\s+([A-Za-z0-9_.]+)",               // Matches `namespace MyApp.Core`
                    @"(class|interface|record|struct|enum)\s+([A-Za-z0-9_]+)\s*([^{]*)", // Matches `class Player : Entity`
                    @"public\s+[A-Za-z0-9_<>,\[\]\s]+\s+([A-Za-z0-9_]+)\s*\(", // Matches `public int Attack(`
                    ".cs",
                    @"///\s*<summary>(.*?)</summary>"              // Matches XML doc summary
                )
            };
        }
    }
}
