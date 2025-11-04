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
            Console.WriteLine("🛠️ ReadmeSync – Automatically update README or ROADMAP with project code overview");
            Console.WriteLine("--------------------------------------------------------------------------\n");

            // ==============================
            //  🧭  Command Line Arguments
            // ==============================
            if (args.Length == 0)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  ReadmeSync [project-root] [output-file] [optional-repo-url]");
                Console.WriteLine("\nExamples:");
                Console.WriteLine("  ReadmeSync . README.md");
                Console.WriteLine("  ReadmeSync ../MySolution ROADMAP.md");
                Console.WriteLine("  ReadmeSync C:/Projects/MyGame README.md https://github.com/USERNAME/REPO");
                Console.WriteLine();
                return;
            }

            string root = Path.GetFullPath(args[0]);
            string outputFile = args.Length > 1 ? args[1] : "README.md";
            string repoUrl = args.Length > 2 ? args[2].TrimEnd('/') : "[YOUR_REPOSITORY_URL_HERE]";

            if (!Directory.Exists(root))
            {
                Console.WriteLine($"❌ Error: directory not found → {root}");
                return;
            }

            // ==============================
            //  🔍  Discover source files
            // ==============================
            Console.WriteLine($"📂 Scanning directory: {root}");
            var csFiles = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);

            if (csFiles.Length == 0)
            {
                Console.WriteLine("⚠️ No .cs files found in project.");
                return;
            }

            // ==============================
            //  🧠  Analyze code files
            // ==============================
            var files = csFiles.Select(f =>
            {
                string text = File.ReadAllText(f);
                string ns = Regex.Match(text, @"namespace\s+([A-Za-z0-9_.]+)").Groups[1].Value.Trim();
                string cls = Regex.Match(text, @"(?<!\/\/.*)(?<![A-Za-z0-9_])class\s+([A-Za-z0-9_]+)").Groups[1].Value.Trim();

                if (string.IsNullOrWhiteSpace(ns) || string.IsNullOrWhiteSpace(cls))
                    return null;

                var methods = Regex.Matches(text, @"public\s+[A-Za-z0-9_<>,\[\]\s]+\s+([A-Za-z0-9_]+)\s*\(")
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value)
                    .Where(m => m != cls) // exclude constructors
                    .Distinct()
                    .ToList();

                var todos = Regex.Matches(text, @"//\s*TODO[: ](.*)", RegexOptions.IgnoreCase)
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value.Trim())
                    .ToList();

                string rel = Path.GetRelativePath(root, f).Replace("\\", "/");
                string fileUrl = $"{repoUrl}/{rel}"; // 👈 customizable placeholder

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

            // ==============================
            //  📊  Count statistics
            // ==============================
            int nsCount = files.Count;
            int classCount = files.Sum(g => g.Count());
            int methodCount = files.Sum(g => g.SelectMany(c => c.Methods).Count());
            int todoCount = files.Sum(g => g.SelectMany(c => c.Todos).Count());

            // ==============================
            //  ✍️  Preserve manual content
            // ==============================
            string manual = "";
            if (File.Exists(outputFile))
            {
                string existing = File.ReadAllText(outputFile);
                int marker = existing.IndexOf("<!-- AUTO-GENERATED BELOW");
                if (marker >= 0)
                    manual = existing[..marker].TrimEnd() + "\n\n";
            }

            // ==============================
            //  💾  Write output file
            // ==============================
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

            Console.WriteLine($"\n✅ ReadmeSync completed successfully!");
            Console.WriteLine($"📁 Output file: {Path.GetFullPath(outputFile)}\n");
        }
    }
}
