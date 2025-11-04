using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReadmeSync
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("🛠️ ReadmeSync – Automatically update README or ROADMAP with project code overview");
            Console.ResetColor();
            Console.WriteLine("--------------------------------------------------------------------------\n");

            try
            {
                // 1) Normalize working directory for IDE launches (helps VS)
                string adjustedCwd = FindRepoRootNearest(AppContext.BaseDirectory, out var adjustedWhy) ?? AppContext.BaseDirectory;
                Environment.CurrentDirectory = adjustedCwd;

                // 2) Args
                if (args.Length == 0)
                {
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  readmesync [scan-root] [output-file] [optional-repo-url]");
                    Console.WriteLine("\nExamples:");
                    Console.WriteLine("  readmesync . README.md");
                    Console.WriteLine("  readmesync ../MySolution ROADMAP.md");
                    Console.WriteLine("  readmesync C:/Projects/MyGame README.md https://github.com/USERNAME/REPO\n");
                    return;
                }

                // 3) Scan root (where we look for .cs files)
                string scanRoot = Path.GetFullPath(args[0]);

                // 4) Repo root (NEAREST upwards match)
                string repoRoot = FindRepoRootNearest(scanRoot, out var reason) ?? scanRoot;

                // 5) Output file (relative to repo root unless absolute)
                string outputFile =
                    args.Length > 1
                        ? (Path.IsPathRooted(args[1]) ? args[1] : Path.Combine(repoRoot, args[1]))
                        : Path.Combine(repoRoot, "README.md");

                Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

                string repoUrl = args.Length > 2 ? args[2].TrimEnd('/') : "[YOUR_REPOSITORY_URL_HERE]";

                // 6) Diagnostics
                Console.WriteLine($"📂 Scanning directory: {scanRoot}");
                Console.WriteLine($"📁 Repo root:         {repoRoot}");
                Console.WriteLine($"🔎 Detected by:       {reason ?? "(no marker; using scanRoot)"}");
                Console.WriteLine($"📝 Output file:       {outputFile}\n");

                if (!Directory.Exists(scanRoot))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ Error: scan directory not found → {scanRoot}");
                    Console.ResetColor();
                    return;
                }

                // 7) Discover .cs files
                var csFiles = Directory.GetFiles(scanRoot, "*.cs", SearchOption.AllDirectories);
                if (csFiles.Length == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠️ No .cs files found in project.");
                    Console.ResetColor();
                    return;
                }

                // 8) Analyze
                var files = csFiles.Select(f =>
                {
                    string text = File.ReadAllText(f);

                    string ns = Regex.Match(text, @"namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Compiled).Groups[1].Value.Trim();
                    string cls = Regex.Match(text, @"(?<!\/\/.*)(?<![A-Za-z0-9_])class\s+([A-Za-z0-9_]+)", RegexOptions.Compiled).Groups[1].Value.Trim();
                    if (string.IsNullOrWhiteSpace(ns) || string.IsNullOrWhiteSpace(cls))
                        return null;

                    var methods = Regex.Matches(text, @"public\s+[A-Za-z0-9_<>,\[\]\s]+\s+([A-Za-z0-9_]+)\s*\(", RegexOptions.Compiled)
                        .Cast<Match>()
                        .Select(m => m.Groups[1].Value)
                        .Where(m => m != cls)
                        .Distinct()
                        .ToList();

                    var todos = Regex.Matches(text, @"//\s*TODO[: ](.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
                        .Cast<Match>()
                        .Select(m => m.Groups[1].Value.Trim())
                        .ToList();

                    // Links: relative to repo root
                    string rel = Path.GetRelativePath(repoRoot, f).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string fileUrl = $"{repoUrl}/{rel}";

                    return new
                    {
                        Namespace = ns,
                        Class = cls,
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

                // 9) Stats
                int nsCount = files.Count;
                int classCount = files.Sum(g => g.Count());
                int methodCount = files.Sum(g => g.SelectMany(c => c.Methods).Count());
                int todoCount = files.Sum(g => g.SelectMany(c => c.Todos).Count());

                // 10) Preserve manual header
                string manual = "";
                if (File.Exists(outputFile))
                {
                    string existing = File.ReadAllText(outputFile);
                    int marker = existing.IndexOf("<!-- AUTO-GENERATED BELOW");
                    if (marker >= 0)
                        manual = existing[..marker].TrimEnd() + "\n\n";
                }

                // 11) Write
                using var sw = new StreamWriter(outputFile, false);
                sw.WriteLine(manual);
                sw.WriteLine("<!-- AUTO-GENERATED BELOW – DO NOT EDIT -->");
                sw.WriteLine("\n# 🧮 Code Overview (auto-generated)\n");
                sw.WriteLine($"_Last updated: **{DateTime.Now:yyyy-MM-dd HH:mm}**_\n");
                sw.WriteLine($"📊 **{nsCount} Namespaces · {classCount} Classes · {methodCount} Methods · {todoCount} TODOs**\n");

                string[] emojis = { "🧱", "⚔️", "🧙", "🏹", "🐉", "🏰", "🧭", "🪄", "🧰", "🎯", "📦", "🧩" };
                int eIndex = 0;

                foreach (var nsGroup in files)
                {
                    string nsEmoji = emojis[eIndex++ % emojis.Length];
                    sw.WriteLine($"\n## {nsEmoji} {nsGroup.Key}\n");

                    foreach (var file in nsGroup)
                    {
                        sw.WriteLine($"### [{file.Class}.cs]({file.Link})");
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
        // 🧭 Nearest Repo Root Finder (prefers closest marker upward)
        // ============================================================
        private static string? FindRepoRootNearest(string startPath, out string? reason)
        {
            reason = null;
            var dir = new DirectoryInfo(Path.GetFullPath(startPath));

            // 1) Prefer NEAREST .git (dir or file)
            var cur = dir;
            while (cur != null)
            {
                if (Directory.Exists(Path.Combine(cur.FullName, ".git")) || File.Exists(Path.Combine(cur.FullName, ".git")))
                {
                    reason = $".git at {cur.FullName}";
                    return cur.FullName;
                }
                cur = cur.Parent;
            }

            // 2) Prefer NEAREST common markers
            string[] markerFiles = { "README.md", "LICENSE" };
            cur = dir;
            while (cur != null)
            {
                if (markerFiles.Any(m => File.Exists(Path.Combine(cur.FullName, m))) ||
                    Directory.Exists(Path.Combine(cur.FullName, ".github")))
                {
                    reason = $"marker (README/LICENSE/.github) at {cur.FullName}";
                    return cur.FullName;
                }
                cur = cur.Parent;
            }

            // 3) Prefer NEAREST folder with a *.sln
            cur = dir;
            while (cur != null)
            {
                if (Directory.EnumerateFiles(cur.FullName, "*.sln").Any())
                {
                    reason = $"solution (*.sln) at {cur.FullName}";
                    return cur.FullName;
                }
                cur = cur.Parent;
            }

            return null;
        }
    }
}
