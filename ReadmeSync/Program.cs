using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReadmeSync.Models;
using ReadmeSync.Services;

#nullable enable

// ============================================================================
// ReadmeSync.cs
// ----------------------------------------------------------------------------
// A command-line tool that scans your source code and auto-generates or updates
// a README or ROADMAP file based on namespaces/packages, classes, public methods,
// and // TODO comments.
//
// Features
// - Supports multiple languages: C#, Java, Python, TypeScript, JavaScript
// - Detects repository root automatically (.git, .sln, README.md, LICENSE)
// - Keeps manual content above marker intact
// - Creates structured markdown summaries of the codebase
// - Supports optional GitHub URL linking
// - Optional emoji icons in output (disabled by default)
//
// Usage
//   readmesync [--lang csharp|java|python|typescript|javascript] [--use-emojis] [--exclude folders] [--no-tracking] [scan-root] [output-file] [optional-repo-url]
//
// Example:
//   readmesync --lang java ./src README.md https://github.com/tombomeke-ehb/ReadmeSync
//   readmesync --lang python . README.md
//   readmesync --use-emojis . README.md
//
// Notes
// - Safe to run multiple times; it only replaces content *below* the marker
// - Compatible with .NET 8.0+
// - Emojis are disabled by default for a more professional output
//
// © 2025 Tombomeke Studios — All rights reserved
// ============================================================================

namespace ReadmeSync
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("ReadmeSync – Automatically update README or ROADMAP with code overview");
            Console.WriteLine("Made by tombomeke Studios");
            Console.ResetColor();
            Console.WriteLine("--------------------------------------------------------------------------\n");

            try
            {
                // ============================================================
                // 1️ Parse CLI Arguments
                // ============================================================
                if (args.Length == 0)
                {
                    ShowUsage();
                    return;
                }

                var config = ParseArguments(args);

                // ============================================================
                // 2️ Initialize Services
                // ============================================================
                var repoFinder = new RepoRootFinder();
                var analyzer = new CodeAnalyzer();
                var markdownGen = new MarkdownGenerator();
                var telemetry = new TelemetryService();

                // ============================================================
                // 3️ Resolve Paths
                // ============================================================
                string scanRoot = ResolveScanRoot(config.Args);
                string repoRoot = repoFinder.FindRepoRoot(scanRoot, out var reason) ?? scanRoot;
                string outputFile = ResolveOutputFile(config.Args, repoRoot);
                string repoUrl = config.Args.Length > 2 ? config.Args[2].TrimEnd('/') : string.Empty;

                Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

                // ============================================================
                // 4️ Get Language Configuration
                // ============================================================
                var patterns = LanguagePatterns.For(config.Language);
                string fileExt = patterns.Extension;

                // Start telemetry (non-blocking)
                Task? telemetryTask = null;
                if (!config.NoTracking)
                {
                    telemetryTask = telemetry.SendTelemetryAsync(config.Language);
                }

                Console.WriteLine($"Language: {config.Language}");
                Console.WriteLine($"Scanning directory: {scanRoot}");
                Console.WriteLine($"Repo root:         {repoRoot}");
                Console.WriteLine($"Detected by:       {reason ?? "(no marker; using scanRoot)"}");
                Console.WriteLine($"Output file:       {outputFile}\n");

                // ============================================================
                // 5️ Scan Files
                // ============================================================
                var codeFiles = Directory.GetFiles(scanRoot, $"*{fileExt}", SearchOption.AllDirectories)
                    .Where(f => !config.Excludes.Any(ex =>
                        f.Contains($"{Path.DirectorySeparatorChar}{ex}{Path.DirectorySeparatorChar}") ||
                        f.EndsWith($"{Path.DirectorySeparatorChar}{ex}")))
                    .ToArray();

                if (codeFiles.Length == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"No {fileExt} files found in project.");
                    Console.ResetColor();
                    return;
                }

                // ============================================================
                // 6️ Analyze Source Files
                // ============================================================
                var files = codeFiles
                    .Select(f =>
                    {
                        string text = File.ReadAllText(f);
                        var info = analyzer.AnalyzeCode(text, patterns, config.Language);
                        if (info == null) return null;

                        // Build relative path and optional link
                        string rel = Path.GetRelativePath(repoRoot, f).Replace(Path.DirectorySeparatorChar, '/');
                        string? fileUrl = repoUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? $"{repoUrl}/{rel}"
                            : null;

                        info.Path = rel;
                        info.Link = fileUrl;
                        return info;
                    })
                    .Where(f => f != null)
                    .GroupBy(f => f.Namespace)
                    .OrderBy(g => g.Key)
                    .ToList();

                // ============================================================
                // 7️ Generate Markdown
                // ============================================================
                markdownGen.GenerateMarkdown(
                    outputFile,
                    files,
                    config.Language,
                    fileExt,
                    config.UseEmojis,
                    repoUrl
                );

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nReadmeSync completed successfully!");
                Console.ResetColor();
                Console.WriteLine($"Output file: {Path.GetFullPath(outputFile)}\n");

                // Wait for telemetry (max 1.5 seconds)
                if (telemetryTask != null)
                {
                    Task.WaitAny(telemetryTask, Task.Delay(1500));
                }
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
        // Helper Methods
        // ============================================================

        private static void ShowUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  readmesync [--lang csharp|java|python|typescript|javascript] [--use-emojis] [--exclude folders] [--no-tracking] [scan-root] [output-file] [optional-repo-url]");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  readmesync . README.md");
            Console.WriteLine("  readmesync --lang java ./src ROADMAP.md https://github.com/USER/REPO");
            Console.WriteLine("  readmesync --lang python ./app README.md");
            Console.WriteLine("  readmesync --lang typescript ./src DOCS.md");
            Console.WriteLine("  readmesync --use-emojis . README.md\n");
        }

        private static Config ParseArguments(string[] args)
        {
            var config = new Config();

            // Parse --lang
            int langIndex = Array.IndexOf(args, "--lang");
            if (langIndex != -1 && langIndex + 1 < args.Length)
            {
                config.Language = args[langIndex + 1].ToLowerInvariant();
                args = args.Where((x, i) => i != langIndex && i != langIndex + 1).ToArray();
            }

            // Parse --exclude
            int excludeIndex = Array.IndexOf(args, "--exclude");
            if (excludeIndex != -1 && excludeIndex + 1 < args.Length)
            {
                config.Excludes = args[excludeIndex + 1].Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                args = args.Where((x, i) => i != excludeIndex && i != excludeIndex + 1).ToArray();
            }

            // Parse flags
            config.NoTracking = args.Contains("--no-tracking");
            config.UseEmojis = args.Contains("--use-emojis");

            args = args.Where(x => x != "--no-tracking" && x != "--use-emojis").ToArray();
            config.Args = args;

            return config;
        }

        private static string ResolveScanRoot(string[] args)
        {
            string scanRoot = ".";

            if (args.Length > 0 && Directory.Exists(args[0]))
                scanRoot = args[0];
            else if (args.Length > 1 && Directory.Exists(args[1]))
                scanRoot = args[1];

            return Path.GetFullPath(scanRoot);
        }

        private static string ResolveOutputFile(string[] args, string repoRoot)
        {
            if (args.Length > 1)
                return Path.IsPathRooted(args[1]) ? args[1] : Path.Combine(repoRoot, args[1]);

            return Path.Combine(repoRoot, "README.md");
        }

        private class Config
        {
            public string Language { get; set; } = "csharp";
            public string[] Excludes { get; set; } = { "bin", "obj", "node_modules", ".git", ".vs", "__pycache__", "dist", "build" };
            public bool NoTracking { get; set; } = false;
            public bool UseEmojis { get; set; } = false;
            public string[] Args { get; set; } = Array.Empty<string>();
        }
    }
}
