using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReadmeSync.Models;
using ReadmeSync.Services;
using Spectre.Console;

#nullable enable

namespace ReadmeSync.Cli
{
    public class CliRunner
    {
        public static async Task RunAsync(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    ShowUsage();
                    return;
                }

                var config = ParseArguments(args);

                var repoFinder = new RepoRootFinder();
                var analyzer = new CodeAnalyzer();
                var markdownGen = new MarkdownGenerator();
                var jsonGen = new JsonGenerator();
                var telemetry = new TelemetryService();

                string scanRoot = ResolveScanRoot(config.Args);
                string repoRoot = repoFinder.FindRepoRoot(scanRoot, out var reason) ?? scanRoot;
                string outputFile = ResolveOutputFile(config.Args, repoRoot);
                string repoUrl = config.Args.Length > 2 ? config.Args[2].TrimEnd('/') : string.Empty;

                Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

                var patterns = LanguagePatterns.For(config.Language);
                string fileExt = patterns.Extension;

                Task? telemetryTask = null;
                if (!config.NoTracking)
                {
                    telemetryTask = telemetry.SendTelemetryAsync(config.Language);
                }

                AnsiConsole.MarkupLine("[cyan]ReadmeSync – Automatically update README or ROADMAP with code overview[/]");
                AnsiConsole.MarkupLine("[cyan]Made by tombomeke Studios[/]");
                AnsiConsole.WriteLine("--------------------------------------------------------------------------\n");

                AnsiConsole.MarkupLine($"[white]Language:[/] {config.Language}");
                AnsiConsole.MarkupLine($"[white]Scanning directory:[/] {scanRoot}");
                AnsiConsole.MarkupLine($"[white]Repo root:[/] {repoRoot}");
                AnsiConsole.MarkupLine($"[white]Detected by:[/] {reason ?? "(no marker; using scanRoot)"}");
                AnsiConsole.MarkupLine($"[white]Output file:[/] {outputFile}\n");

                var codeFiles = Directory.GetFiles(scanRoot, $"*{fileExt}", SearchOption.AllDirectories)
                    .Where(f => !config.Excludes.Any(ex =>
                        f.Contains($"{Path.DirectorySeparatorChar}{ex}{Path.DirectorySeparatorChar}") ||
                        f.EndsWith($"{Path.DirectorySeparatorChar}{ex}")))
                    .ToArray();

                if (codeFiles.Length == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No {0} files found in project.[/]", fileExt);
                    return;
                }

                var files = codeFiles
                    .Select(f =>
                    {
                        string text = File.ReadAllText(f);
                        var info = analyzer.AnalyzeCode(text, patterns, config.Language);
                        if (info == null) return null;

                        string rel = Path.GetRelativePath(repoRoot, f).Replace(Path.DirectorySeparatorChar, '/');
                        string? fileUrl = repoUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? $"{repoUrl}/{rel}"
                            : null;

                        info.Path = rel;
                        info.Link = fileUrl;
                        return info;
                    })
                    .Where(f => f != null)
                    .Select(f => f!)
                    .GroupBy(f => f.Namespace)
                    .OrderBy(g => g.Key)
                    .ToList();

                if (config.DryRun)
                {
                    AnsiConsole.MarkupLine("[yellow]DRY RUN MODE - No files will be written[/]\n");

                    if (config.OutputJson)
                    {
                        string jsonPreview = jsonGen.PreviewJson(files);
                        var panel = new Panel(jsonPreview)
                        {
                            Border = BoxBorder.Rounded,
                            Expand = false,
                            Header = new PanelHeader("JSON Preview")
                        };
                        AnsiConsole.Write(panel);
                    }
                    else
                    {
                        string markdownPreview = markdownGen.PreviewMarkdown(files, config.Language, fileExt, config.UseEmojis, repoUrl);
                        var panel = new Panel(markdownPreview)
                        {
                            Border = BoxBorder.Rounded,
                            Expand = false,
                            Header = new PanelHeader("Markdown Preview")
                        };
                        AnsiConsole.Write(panel);
                    }
                }
                else
                {
                    if (config.OutputJson)
                    {
                        jsonGen.GenerateJson(outputFile, files);
                    }
                    else
                    {
                        markdownGen.GenerateMarkdown(outputFile, files, config.Language, fileExt, config.UseEmojis, repoUrl);
                    }

                    AnsiConsole.MarkupLine("[green]\n✓ ReadmeSync completed successfully![/]");
                    AnsiConsole.MarkupLine($"[white]Output file:[/] {Path.GetFullPath(outputFile)}\n");
                }

                if (telemetryTask != null)
                {
                    await Task.WhenAny(telemetryTask, Task.Delay(1500));
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]❌ An unexpected error occurred:[/]");
                AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
            }
        }

        private static void ShowUsage()
        {
            AnsiConsole.MarkupLine("[yellow]Usage:[/]");
            AnsiConsole.MarkupLine("  readmesync [--lang csharp|java|python|typescript|javascript|php|go|rust|ruby|kotlin|swift|cpp] [--use-emojis] [--json] [--dry-run] [--exclude folders] [--no-tracking] [scan-root] [output-file] [optional-repo-url]");
            AnsiConsole.MarkupLine("\n[yellow]Examples:[/]");
            AnsiConsole.MarkupLine("  readmesync . README.md");
            AnsiConsole.MarkupLine("  readmesync --lang go ./src README.md");
            AnsiConsole.MarkupLine("  readmesync --lang rust . README.md");
            AnsiConsole.MarkupLine("  readmesync --dry-run . README.md");
            AnsiConsole.MarkupLine("  readmesync --use-emojis . README.md");
            AnsiConsole.MarkupLine("  readmesync --json . roadmap.json\n");
        }

        private static Config ParseArguments(string[] args)
        {
            var config = new Config();

            int langIndex = Array.IndexOf(args, "--lang");
            if (langIndex != -1 && langIndex + 1 < args.Length)
            {
                config.Language = args[langIndex + 1].ToLowerInvariant();
                args = args.Where((x, i) => i != langIndex && i != langIndex + 1).ToArray();
            }

            int excludeIndex = Array.IndexOf(args, "--exclude");
            if (excludeIndex != -1 && excludeIndex + 1 < args.Length)
            {
                config.Excludes = args[excludeIndex + 1].Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                args = args.Where((x, i) => i != excludeIndex && i != excludeIndex + 1).ToArray();
            }

            config.NoTracking = args.Contains("--no-tracking");
            config.UseEmojis = args.Contains("--use-emojis");
            config.OutputJson = args.Contains("--json");
            config.DryRun = args.Contains("--dry-run");

            args = args.Where(x => x != "--no-tracking" && x != "--use-emojis" && x != "--json" && x != "--dry-run").ToArray();
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
            public bool OutputJson { get; set; } = false;
            public bool DryRun { get; set; } = false;
            public string[] Args { get; set; } = Array.Empty<string>();
        }
    }
}
