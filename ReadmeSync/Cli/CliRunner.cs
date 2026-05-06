using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ReadmeSync.Models;
using ReadmeSync.Services;
using Spectre.Console;

#nullable enable

namespace ReadmeSync.Cli
{
    public class CliRunner
    {
        private static DiffService _diffService = new();
        private static StatsService _statsService = new();
        private static ValidatorService _validatorService = new();

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

                // Handle special single-action flags
                if (config.StatsOnly)
                {
                    await HandleStatsOnlyAsync(config);
                    return;
                }

                if (config.Validate)
                {
                    await HandleValidateAsync(config);
                    return;
                }

                if (config.Compare)
                {
                    await HandleCompareAsync(config);
                    return;
                }

                if (config.Watch)
                {
                    await HandleWatchAsync(config);
                    return;
                }

                // Normal scan flow
                await HandleNormalScanAsync(config);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ An unexpected error occurred:[/]");
                AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
            }
        }

        private static async Task HandleNormalScanAsync(Config config)
        {
            var repoFinder = new RepoRootFinder();
            var analyzer = new CodeAnalyzer();
            var markdownGen = new MarkdownGenerator();
            var jsonGen = new JsonGenerator();
            var telemetry = new TelemetryService();

            string scanRoot = ResolveScanRoot(config.Args);
            string repoRoot = repoFinder.FindRepoRoot(scanRoot, out var reason) ?? scanRoot;
            string outputFile = ResolveOutputFile(config.Args, repoRoot);
            string repoUrl = config.Args.Length > 2 ? config.Args[2].TrimEnd('/') : string.Empty;

            // Update-only check
            if (config.UpdateOnly && !File.Exists(outputFile))
            {
                AnsiConsole.MarkupLine("[red]❌ --update-only mode: output file does not exist.[/]");
                return;
            }

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
                AnsiConsole.MarkupLine($"[yellow]No {fileExt} files found in project.[/]");
                return;
            }

            var files = codeFiles
                .Select(f =>
                {
                    string text = File.ReadAllText(f);
                    var info = analyzer.AnalyzeCode(text, patterns, config.Language, config.IncludePrivate);
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

            // TODO filter
            if (!string.IsNullOrEmpty(config.TodoFilter))
            {
                var todoRegex = new Regex(config.TodoFilter, RegexOptions.IgnoreCase);
                foreach (var file in files.SelectMany(g => g))
                {
                    file.Todos = file.Todos.Where(t => todoRegex.IsMatch(t)).ToList();
                }
            }

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
                    string markdownPreview = markdownGen.PreviewMarkdown(files, config.Language, fileExt, config.UseEmojis, repoUrl, config.SummaryOnly);
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
                    markdownGen.GenerateMarkdown(outputFile, files, config.Language, fileExt, config.UseEmojis, repoUrl, config.SummaryOnly);
                }

                AnsiConsole.MarkupLine("[green]\n✓ ReadmeSync completed successfully![/]");
                AnsiConsole.MarkupLine($"[white]Output file:[/] {Path.GetFullPath(outputFile)}\n");
            }

            if (telemetryTask != null)
            {
                await Task.WhenAny(telemetryTask, Task.Delay(1500));
            }
        }

        private static async Task HandleStatsOnlyAsync(Config config)
        {
            var repoFinder = new RepoRootFinder();
            var analyzer = new CodeAnalyzer();

            string scanRoot = ResolveScanRoot(config.Args);
            string repoRoot = repoFinder.FindRepoRoot(scanRoot, out _) ?? scanRoot;

            var patterns = LanguagePatterns.For(config.Language);
            string fileExt = patterns.Extension;

            var codeFiles = Directory.GetFiles(scanRoot, $"*{fileExt}", SearchOption.AllDirectories)
                .Where(f => !config.Excludes.Any(ex =>
                    f.Contains($"{Path.DirectorySeparatorChar}{ex}{Path.DirectorySeparatorChar}") ||
                    f.EndsWith($"{Path.DirectorySeparatorChar}{ex}")))
                .ToArray();

            var files = codeFiles
                .Select(f =>
                {
                    var info = analyzer.AnalyzeCode(File.ReadAllText(f), patterns, config.Language);
                    return info;
                })
                .Where(f => f != null)
                .Select(f => f!)
                .GroupBy(f => f.Namespace)
                .OrderBy(g => g.Key)
                .ToList();

            var stats = _statsService.ComputeStats(files, codeFiles.Length);

            AnsiConsole.MarkupLine("[cyan bold]📊 Code Statistics[/]\n");

            var table = new Table();
            table.AddColumn("[cyan]Metric[/]");
            table.AddColumn("[cyan]Count[/]");
            table.AddRow("Total Files", stats.TotalFiles.ToString());
            table.AddRow("Namespaces", stats.TotalNamespaces.ToString());
            table.AddRow("Types", stats.TotalTypes.ToString());
            table.AddRow("Methods", stats.TotalMethods.ToString());
            table.AddRow("TODOs", stats.TotalTodos.ToString());
            table.AddRow("Avg Methods/Type", stats.AvgMethodsPerType.ToString());

            AnsiConsole.Write(table);

            if (stats.TypeBreakdown.Any())
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[cyan]Type Breakdown:[/]");
                foreach (var kvp in stats.TypeBreakdown.OrderByDescending(x => x.Value))
                {
                    AnsiConsole.MarkupLine($"  {kvp.Key}: [green]{kvp.Value}[/]");
                }
            }
        }

        private static async Task HandleValidateAsync(Config config)
        {
            string scanRoot = ResolveScanRoot(config.Args);
            string outputFile = ResolveOutputFile(config.Args, scanRoot);

            if (!File.Exists(outputFile))
            {
                AnsiConsole.MarkupLine($"[red]❌ File not found: {outputFile}[/]");
                return;
            }

            string content = File.ReadAllText(outputFile);
            string repoUrl = config.Args.Length > 2 ? config.Args[2] : "";

            var result = _validatorService.Validate(content, repoUrl);

            AnsiConsole.MarkupLine("[cyan bold]✅ Validation Report[/]\n");

            if (result.IsValid)
            {
                AnsiConsole.MarkupLine("[green]✓ Markdown is valid![/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]✗ Validation found issues:[/]\n");
            }

            if (result.Issues.Any())
            {
                var table = new Table();
                table.AddColumn("[cyan]Severity[/]");
                table.AddColumn("[cyan]Message[/]");

                foreach (var issue in result.Issues)
                {
                    string severityColor = issue.Severity switch
                    {
                        Severity.Error => "red",
                        Severity.Warning => "yellow",
                        _ => "cyan"
                    };
                    table.AddRow($"[{severityColor}]{issue.Severity}[/]", issue.Message);
                }

                AnsiConsole.Write(table);
            }
        }

        private static async Task HandleCompareAsync(Config config)
        {
            var repoFinder = new RepoRootFinder();
            var analyzer = new CodeAnalyzer();
            var markdownGen = new MarkdownGenerator();

            string scanRoot = ResolveScanRoot(config.Args);
            string repoRoot = repoFinder.FindRepoRoot(scanRoot, out _) ?? scanRoot;
            string outputFile = ResolveOutputFile(config.Args, repoRoot);
            string repoUrl = config.Args.Length > 2 ? config.Args[2].TrimEnd('/') : string.Empty;

            if (!File.Exists(outputFile))
            {
                AnsiConsole.MarkupLine($"[red]❌ File not found for comparison: {outputFile}[/]");
                return;
            }

            var patterns = LanguagePatterns.For(config.Language);
            string fileExt = patterns.Extension;

            var codeFiles = Directory.GetFiles(scanRoot, $"*{fileExt}", SearchOption.AllDirectories)
                .Where(f => !config.Excludes.Any(ex =>
                    f.Contains($"{Path.DirectorySeparatorChar}{ex}{Path.DirectorySeparatorChar}") ||
                    f.EndsWith($"{Path.DirectorySeparatorChar}{ex}")))
                .ToArray();

            var files = codeFiles
                .Select(f =>
                {
                    var info = analyzer.AnalyzeCode(File.ReadAllText(f), patterns, config.Language);
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

            string newContent = markdownGen.PreviewMarkdown(files, config.Language, fileExt, config.UseEmojis, repoUrl);
            string oldContent = File.ReadAllText(outputFile);

            var diffLines = _diffService.GetDiffLines(oldContent, newContent);

            AnsiConsole.MarkupLine("[cyan bold]📝 Diff: Current vs Generated[/]\n");

            foreach (var line in diffLines.Take(100))
            {
                string color = line.Type == '+' ? "green" : (line.Type == '-' ? "red" : "white");
                char symbol = line.Type == '+' ? '+' : (line.Type == '-' ? '-' : ' ');
                AnsiConsole.MarkupLine($"[{color}]{symbol} {line.Content}[/]");
            }

            if (diffLines.Count > 100)
            {
                AnsiConsole.MarkupLine($"[yellow]... and {diffLines.Count - 100} more lines[/]");
            }
        }

        private static async Task HandleWatchAsync(Config config)
        {
            string scanRoot = ResolveScanRoot(config.Args);
            string repoRoot = new RepoRootFinder().FindRepoRoot(scanRoot, out _) ?? scanRoot;
            string outputFile = ResolveOutputFile(config.Args, repoRoot);
            string repoUrl = config.Args.Length > 2 ? config.Args[2].TrimEnd('/') : string.Empty;

            var patterns = LanguagePatterns.For(config.Language);
            string fileExt = patterns.Extension;

            var watcher = new WatchRunner(scanRoot, outputFile, config.Language, fileExt, config.Excludes, repoUrl, config.UseEmojis, config.NoTracking);
            await watcher.RunAsync();
        }

        private static void ShowUsage()
        {
            AnsiConsole.MarkupLine("[yellow]Usage:[/]");
            AnsiConsole.MarkupLine("  readmesync [--lang lang] [--dry-run] [--stats] [--compare] [--validate] [--watch] [--filter-todos pattern] [--update-only] [--include-private] [--summary-only] [--use-emojis] [--json] [--exclude folders] [--no-tracking] [scan-root] [output-file] [optional-repo-url]");
            AnsiConsole.MarkupLine("\n[yellow]Examples:[/]");
            AnsiConsole.MarkupLine("  readmesync . README.md");
            AnsiConsole.MarkupLine("  readmesync --lang go ./src README.md");
            AnsiConsole.MarkupLine("  readmesync --dry-run . README.md");
            AnsiConsole.MarkupLine("  readmesync --stats . README.md");
            AnsiConsole.MarkupLine("  readmesync --compare . README.md");
            AnsiConsole.MarkupLine("  readmesync --validate README.md");
            AnsiConsole.MarkupLine("  readmesync --watch . README.md");
            AnsiConsole.MarkupLine("  readmesync --filter-todos \"ERROR\" . README.md");
            AnsiConsole.MarkupLine("  readmesync --include-private . README.md");
            AnsiConsole.MarkupLine("  readmesync --summary-only . README.md\n");
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

            int todoFilterIndex = Array.IndexOf(args, "--filter-todos");
            if (todoFilterIndex != -1 && todoFilterIndex + 1 < args.Length)
            {
                config.TodoFilter = args[todoFilterIndex + 1];
                args = args.Where((x, i) => i != todoFilterIndex && i != todoFilterIndex + 1).ToArray();
            }

            config.NoTracking = args.Contains("--no-tracking");
            config.UseEmojis = args.Contains("--use-emojis");
            config.OutputJson = args.Contains("--json");
            config.DryRun = args.Contains("--dry-run");
            config.StatsOnly = args.Contains("--stats");
            config.Validate = args.Contains("--validate");
            config.Compare = args.Contains("--compare");
            config.Watch = args.Contains("--watch");
            config.UpdateOnly = args.Contains("--update-only");
            config.IncludePrivate = args.Contains("--include-private");
            config.SummaryOnly = args.Contains("--summary-only");

            args = args.Where(x => x != "--no-tracking" && x != "--use-emojis" && x != "--json" && x != "--dry-run" &&
                x != "--stats" && x != "--validate" && x != "--compare" && x != "--watch" && x != "--update-only" &&
                x != "--include-private" && x != "--summary-only").ToArray();
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
            public bool StatsOnly { get; set; } = false;
            public bool Validate { get; set; } = false;
            public bool Compare { get; set; } = false;
            public bool Watch { get; set; } = false;
            public bool UpdateOnly { get; set; } = false;
            public bool IncludePrivate { get; set; } = false;
            public bool SummaryOnly { get; set; } = false;
            public string TodoFilter { get; set; } = "";
            public string[] Args { get; set; } = Array.Empty<string>();
        }
    }
}
