using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ReadmeSync.Models;
using ReadmeSync.Services;
using Spectre.Console;

#nullable enable

namespace ReadmeSync.Tui
{
    public class TuiApp
    {
        private readonly RepoRootFinder _repoFinder = new();
        private readonly CodeAnalyzer _analyzer = new();
        private readonly MarkdownGenerator _markdownGen = new();
        private readonly JsonGenerator _jsonGen = new();
        private readonly TelemetryService _telemetry = new();
        private readonly DiffService _diffService = new();
        private readonly StatsService _statsService = new();
        private readonly ValidatorService _validatorService = new();

        private string _selectedLanguage = "csharp";
        private string _selectedScanRoot = ".";
        private string _selectedOutputFile = "README.md";
        private string _selectedRepoUrl = "";
        private bool _useEmojis = false;
        private bool _noTracking = false;
        private bool _includePrivate = false;
        private bool _updateOnlyMode = false;

        public async Task RunAsync()
        {
            ShowWelcome();

            while (true)
            {
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[cyan bold]Main Menu[/]")
                        .AddChoices(new[]
                        {
                            "📖 Scan & Generate README",
                            "🗺️ Scan & Generate ROADMAP",
                            "📊 Scan & Export JSON",
                            "👁️ Dry Run (Preview)",
                            "🔍 Compare (Diff)",
                            "📈 Show Statistics",
                            "✅ Validate README",
                            "👀 Watch Mode",
                            "⚙️ Settings",
                            "❌ Exit"
                        }));

                switch (choice)
                {
                    case "📖 Scan & Generate README":
                        await ScanAndGenerateAsync("README.md");
                        break;
                    case "🗺️ Scan & Generate ROADMAP":
                        await ScanAndGenerateAsync("ROADMAP.md");
                        break;
                    case "📊 Scan & Export JSON":
                        await ScanAndExportJsonAsync();
                        break;
                    case "👁️ Dry Run (Preview)":
                        await DryRunPreviewAsync();
                        break;
                    case "🔍 Compare (Diff)":
                        await CompareAsync();
                        break;
                    case "📈 Show Statistics":
                        await ShowStatsAsync();
                        break;
                    case "✅ Validate README":
                        await ValidateAsync();
                        break;
                    case "👀 Watch Mode":
                        await WatchModeAsync();
                        break;
                    case "⚙️ Settings":
                        ShowSettings();
                        break;
                    case "❌ Exit":
                        AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
                        return;
                }
            }
        }

        private void ShowWelcome()
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(
                new FigletText("ReadmeSync") { Color = new Color(0, 255, 255) });

            AnsiConsole.MarkupLine("[cyan]v2.1.0 - Made by Tombomeke Studios[/]\n");
        }

        private void ShowSettings()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[cyan bold]Settings[/]\n");

            while (true)
            {
                var settingChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[cyan]Select Setting[/]")
                        .AddChoices(new[]
                        {
                            "🌐 Default Language",
                            "📁 Default Scan Root",
                            "📄 Default Output File",
                            "🔗 GitHub Repository URL",
                            "😀 Emojis",
                            "📡 Telemetry",
                            "🔒 Include Private Members",
                            "🔄 Update-Only Mode",
                            "⬅️ Back to Menu"
                        }));

                switch (settingChoice)
                {
                    case "🌐 Default Language":
                        SelectLanguage();
                        break;
                    case "📁 Default Scan Root":
                        _selectedScanRoot = AnsiConsole.Ask<string>("[cyan]Enter scan root directory:[/]", ".");
                        break;
                    case "📄 Default Output File":
                        _selectedOutputFile = AnsiConsole.Ask<string>("[cyan]Enter output filename:[/]", "README.md");
                        break;
                    case "🔗 GitHub Repository URL":
                        _selectedRepoUrl = AnsiConsole.Ask<string>("[cyan]Enter GitHub URL (optional):[/]", "");
                        break;
                    case "😀 Emojis":
                        _useEmojis = !_useEmojis;
                        AnsiConsole.MarkupLine($"[green]Emojis: {(_useEmojis ? "Enabled" : "Disabled")}[/]");
                        break;
                    case "📡 Telemetry":
                        _noTracking = !_noTracking;
                        AnsiConsole.MarkupLine($"[green]Telemetry: {(_noTracking ? "Disabled" : "Enabled")}[/]");
                        break;
                    case "🔒 Include Private Members":
                        _includePrivate = !_includePrivate;
                        AnsiConsole.MarkupLine($"[green]Include Private: {(_includePrivate ? "Enabled" : "Disabled")}[/]");
                        break;
                    case "🔄 Update-Only Mode":
                        _updateOnlyMode = !_updateOnlyMode;
                        AnsiConsole.MarkupLine($"[green]Update-Only: {(_updateOnlyMode ? "Enabled" : "Disabled")}[/]");
                        break;
                    case "⬅️ Back to Menu":
                        return;
                }
            }
        }

        private void SelectLanguage()
        {
            var languages = new[] { "csharp", "java", "python", "typescript", "javascript", "php", "go", "rust", "ruby", "kotlin", "swift", "cpp" };
            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]Select Language[/]")
                    .AddChoices(languages));
            _selectedLanguage = selected;
            AnsiConsole.MarkupLine($"[green]Language set to: {_selectedLanguage}[/]");
        }

        private async Task ScanAndGenerateAsync(string defaultOutputFile)
        {
            AnsiConsole.Clear();
            SelectLanguage();

            string scanRoot = AnsiConsole.Ask<string>("[cyan]Enter scan root directory:[/]", _selectedScanRoot);
            string outputFile = AnsiConsole.Ask<string>($"[cyan]Enter output filename:[/]", defaultOutputFile);
            string repoUrl = AnsiConsole.Ask<string>("[cyan]Enter GitHub URL (optional):[/]", _selectedRepoUrl);

            var excludeInput = AnsiConsole.Ask<string>("[cyan]Exclude folders (comma-separated, or press Enter for defaults):[/]", "");
            var excludes = string.IsNullOrWhiteSpace(excludeInput)
                ? new[] { "bin", "obj", "node_modules", ".git", ".vs", "__pycache__", "dist", "build" }
                : excludeInput.Split(',').Select(x => x.Trim()).ToArray();

            string? todoFilter = AnsiConsole.Ask<string>("[cyan]Filter TODOs (regex, or press Enter for all):[/]", "");
            bool summaryOnly = AnsiConsole.Confirm("[cyan]Summary only (skip namespace tree)?[/]", false);

            await PerformScanAsync(scanRoot, outputFile, repoUrl, excludes, isJson: false, todoFilter, summaryOnly);
        }

        private async Task ScanAndExportJsonAsync()
        {
            AnsiConsole.Clear();
            SelectLanguage();

            string scanRoot = AnsiConsole.Ask<string>("[cyan]Enter scan root directory:[/]", _selectedScanRoot);
            string outputFile = AnsiConsole.Ask<string>("[cyan]Enter output filename (JSON):[/]", "roadmap.json");

            var excludeInput = AnsiConsole.Ask<string>("[cyan]Exclude folders (comma-separated, or press Enter for defaults):[/]", "");
            var excludes = string.IsNullOrWhiteSpace(excludeInput)
                ? new[] { "bin", "obj", "node_modules", ".git", ".vs", "__pycache__", "dist", "build" }
                : excludeInput.Split(',').Select(x => x.Trim()).ToArray();

            string? todoFilter = AnsiConsole.Ask<string>("[cyan]Filter TODOs (regex, or press Enter for all):[/]", "");

            await PerformScanAsync(scanRoot, outputFile, "", excludes, isJson: true, todoFilter, false);
        }

        private async Task DryRunPreviewAsync()
        {
            AnsiConsole.Clear();
            SelectLanguage();

            string scanRoot = AnsiConsole.Ask<string>("[cyan]Enter scan root directory:[/]", _selectedScanRoot);
            string repoUrl = AnsiConsole.Ask<string>("[cyan]Enter GitHub URL (optional):[/]", _selectedRepoUrl);

            var excludeInput = AnsiConsole.Ask<string>("[cyan]Exclude folders (comma-separated, or press Enter for defaults):[/]", "");
            var excludes = string.IsNullOrWhiteSpace(excludeInput)
                ? new[] { "bin", "obj", "node_modules", ".git", ".vs", "__pycache__", "dist", "build" }
                : excludeInput.Split(',').Select(x => x.Trim()).ToArray();

            bool summaryOnly = AnsiConsole.Confirm("[cyan]Summary only (skip namespace tree)?[/]", false);

            await PerformDryRunAsync(scanRoot, repoUrl, excludes, summaryOnly);
        }

        private async Task CompareAsync()
        {
            AnsiConsole.Clear();
            SelectLanguage();

            string scanRoot = AnsiConsole.Ask<string>("[cyan]Enter scan root directory:[/]", _selectedScanRoot);
            string outputFile = AnsiConsole.Ask<string>("[cyan]Enter markdown file to compare:[/]", _selectedOutputFile);
            string repoUrl = AnsiConsole.Ask<string>("[cyan]Enter GitHub URL (optional):[/]", _selectedRepoUrl);

            var excludeInput = AnsiConsole.Ask<string>("[cyan]Exclude folders (comma-separated, or press Enter for defaults):[/]", "");
            var excludes = string.IsNullOrWhiteSpace(excludeInput)
                ? new[] { "bin", "obj", "node_modules", ".git", ".vs", "__pycache__", "dist", "build" }
                : excludeInput.Split(',').Select(x => x.Trim()).ToArray();

            await PerformCompareAsync(scanRoot, outputFile, repoUrl, excludes);
        }

        private async Task ShowStatsAsync()
        {
            AnsiConsole.Clear();
            SelectLanguage();

            string scanRoot = AnsiConsole.Ask<string>("[cyan]Enter scan root directory:[/]", _selectedScanRoot);

            var excludeInput = AnsiConsole.Ask<string>("[cyan]Exclude folders (comma-separated, or press Enter for defaults):[/]", "");
            var excludes = string.IsNullOrWhiteSpace(excludeInput)
                ? new[] { "bin", "obj", "node_modules", ".git", ".vs", "__pycache__", "dist", "build" }
                : excludeInput.Split(',').Select(x => x.Trim()).ToArray();

            await PerformStatsAsync(scanRoot, excludes);
        }

        private async Task ValidateAsync()
        {
            AnsiConsole.Clear();
            string outputFile = AnsiConsole.Ask<string>("[cyan]Enter markdown file to validate:[/]", _selectedOutputFile);
            string repoUrl = AnsiConsole.Ask<string>("[cyan]Enter GitHub URL (optional):[/]", _selectedRepoUrl);

            await PerformValidateAsync(outputFile, repoUrl);
        }

        private async Task WatchModeAsync()
        {
            AnsiConsole.Clear();
            SelectLanguage();

            string scanRoot = AnsiConsole.Ask<string>("[cyan]Enter scan root directory:[/]", _selectedScanRoot);
            string outputFile = AnsiConsole.Ask<string>("[cyan]Enter output filename:[/]", _selectedOutputFile);
            string repoUrl = AnsiConsole.Ask<string>("[cyan]Enter GitHub URL (optional):[/]", _selectedRepoUrl);

            var patterns = LanguagePatterns.For(_selectedLanguage);
            var watcher = new WatchRunner(
                scanRoot, outputFile, _selectedLanguage, patterns.Extension,
                new[] { "bin", "obj", "node_modules", ".git", ".vs", "__pycache__", "dist", "build" },
                repoUrl, _useEmojis, _noTracking);

            await watcher.RunAsync();
        }

        private async Task PerformScanAsync(string scanRoot, string outputFile, string repoUrl, string[] excludes, bool isJson, string? todoFilter, bool summaryOnly)
        {
            AnsiConsole.Clear();

            try
            {
                scanRoot = Path.GetFullPath(scanRoot);
                if (!Directory.Exists(scanRoot))
                {
                    AnsiConsole.MarkupLine("[red]Error: Scan directory does not exist.[/]");
                    AnsiConsole.Ask<string>("[yellow]Press Enter to continue...[/]", "");
                    return;
                }

                if (_updateOnlyMode && !File.Exists(outputFile))
                {
                    AnsiConsole.MarkupLine("[red]Error: --update-only mode but file does not exist.[/]");
                    AnsiConsole.Ask<string>("[yellow]Press Enter to continue...[/]", "");
                    return;
                }

                string repoRoot = _repoFinder.FindRepoRoot(scanRoot, out _) ?? scanRoot;
                outputFile = Path.IsPathRooted(outputFile) ? outputFile : Path.Combine(repoRoot, outputFile);

                Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

                var patterns = LanguagePatterns.For(_selectedLanguage);
                string fileExt = patterns.Extension;

                var codeFiles = Directory.GetFiles(scanRoot, $"*{fileExt}", SearchOption.AllDirectories)
                    .Where(f => !excludes.Any(ex =>
                        f.Contains($"{Path.DirectorySeparatorChar}{ex}{Path.DirectorySeparatorChar}") ||
                        f.EndsWith($"{Path.DirectorySeparatorChar}{ex}")))
                    .ToArray();

                AnsiConsole.Progress()
                    .Start(ctx =>
                    {
                        var task1 = ctx.AddTask("[cyan]Finding repo root...[/]", maxValue: 1);
                        task1.Increment(1);

                        var task2 = ctx.AddTask("[cyan]Scanning files...[/]", maxValue: codeFiles.Length);
                        task2.Increment(codeFiles.Length);

                        var task3 = ctx.AddTask("[cyan]Analyzing code...[/]", maxValue: 1);

                        var files = codeFiles
                            .Select(f =>
                            {
                                string text = File.ReadAllText(f);
                                var info = _analyzer.AnalyzeCode(text, patterns, _selectedLanguage, _includePrivate);
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

                        task3.Increment(1);

                        var task4 = ctx.AddTask("[cyan]Generating output...[/]", maxValue: 1);

                        if (!string.IsNullOrEmpty(todoFilter))
                        {
                            var todoRegex = new Regex(todoFilter, RegexOptions.IgnoreCase);
                            foreach (var file in files.SelectMany(g => g))
                            {
                                file.Todos = file.Todos.Where(t => todoRegex.IsMatch(t)).ToList();
                            }
                        }

                        if (isJson)
                        {
                            _jsonGen.GenerateJson(outputFile, files);
                        }
                        else
                        {
                            _markdownGen.GenerateMarkdown(outputFile, files, _selectedLanguage, fileExt, _useEmojis, repoUrl, summaryOnly);
                        }

                        task4.Increment(1);
                    });

                AnsiConsole.MarkupLine("[green]✓ Scan completed successfully![/]\n");
                AnsiConsole.MarkupLine($"[green]Output file:[/] {Path.GetFullPath(outputFile)}");

                Task? telemetryTask = null;
                if (!_noTracking)
                {
                    telemetryTask = _telemetry.SendTelemetryAsync(_selectedLanguage);
                    if (telemetryTask != null)
                    {
                        await Task.WhenAny(telemetryTask, Task.Delay(1500));
                    }
                }

                AnsiConsole.Ask<string>("[yellow]Press Enter to continue...[/]", "");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                AnsiConsole.Ask<string>("[yellow]Press Enter to continue...[/]", "");
            }
        }

        private async Task PerformDryRunAsync(string scanRoot, string repoUrl, string[] excludes, bool summaryOnly)
        {
            AnsiConsole.Clear();

            try
            {
                scanRoot = Path.GetFullPath(scanRoot);
                if (!Directory.Exists(scanRoot))
                {
                    AnsiConsole.MarkupLine("[red]Error: Scan directory does not exist.[/]");
                    AnsiConsole.Ask<string>("[yellow]Press Enter to continue...[/]", "");
                    return;
                }

                string repoRoot = _repoFinder.FindRepoRoot(scanRoot, out _) ?? scanRoot;

                var patterns = LanguagePatterns.For(_selectedLanguage);
                string fileExt = patterns.Extension;

                AnsiConsole.MarkupLine("[yellow]DRY RUN MODE - No files will be written[/]\n");

                var codeFiles = Directory.GetFiles(scanRoot, $"*{fileExt}", SearchOption.AllDirectories)
                    .Where(f => !excludes.Any(ex =>
                        f.Contains($"{Path.DirectorySeparatorChar}{ex}{Path.DirectorySeparatorChar}") ||
                        f.EndsWith($"{Path.DirectorySeparatorChar}{ex}")))
                    .ToArray();

                var files = codeFiles
                    .Select(f =>
                    {
                        string text = File.ReadAllText(f);
                        var info = _analyzer.AnalyzeCode(text, patterns, _selectedLanguage, _includePrivate);
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

                string preview = _markdownGen.PreviewMarkdown(files, _selectedLanguage, fileExt, _useEmojis, repoUrl, summaryOnly);

                var panel = new Panel(preview)
                {
                    Border = BoxBorder.Rounded,
                    Expand = false,
                    Header = new PanelHeader("Markdown Preview")
                };
                AnsiConsole.Write(panel);

                AnsiConsole.Ask<string>("[yellow]Press Enter to continue...[/]", "");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                AnsiConsole.Ask<string>("[yellow]Press Enter to continue...[/]", "");
            }
        }

        private async Task PerformCompareAsync(string scanRoot, string outputFile, string repoUrl, string[] excludes)
        {
            AnsiConsole.Clear();

            try
            {
                scanRoot = Path.GetFullPath(scanRoot);
                if (!File.Exists(outputFile))
                {
                    AnsiConsole.MarkupLine("[red]Error: File not found.[/]");
                    AnsiConsole.Ask<string>("[yellow]Press Enter to continue...[/]", "");
                    return;
                }

                string repoRoot = _repoFinder.FindRepoRoot(scanRoot, out _) ?? scanRoot;
                var patterns = LanguagePatterns.For(_selectedLanguage);
                string fileExt = patterns.Extension;

                var codeFiles = Directory.GetFiles(scanRoot, $"*{fileExt}", SearchOption.AllDirectories)
                    .Where(f => !excludes.Any(ex =>
                        f.Contains($"{Path.DirectorySeparatorChar}{ex}{Path.DirectorySeparatorChar}") ||
                        f.EndsWith($"{Path.DirectorySeparatorChar}{ex}")))
                    .ToArray();

                var files = codeFiles
                    .Select(f =>
                    {
                        var info = _analyzer.AnalyzeCode(File.ReadAllText(f), patterns, _selectedLanguage);
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

                string newContent = _markdownGen.PreviewMarkdown(files, _selectedLanguage, fileExt, _useEmojis, repoUrl);
                string oldContent = File.ReadAllText(outputFile);

                var diffLines = _diffService.GetDiffLines(oldContent, newContent);

                AnsiConsole.MarkupLine("[cyan bold]📝 Diff: Current vs Generated[/]\n");

                foreach (var line in diffLines.Take(50))
                {
                    string color = line.Type == '+' ? "green" : (line.Type == '-' ? "red" : "white");
                    char symbol = line.Type == '+' ? '+' : (line.Type == '-' ? '-' : ' ');
                    AnsiConsole.MarkupLine($"[{color}]{symbol} {line.Content}[/]");
                }

                if (diffLines.Count > 50)
                {
                    AnsiConsole.MarkupLine($"[yellow]... and {diffLines.Count - 50} more lines[/]");
                }

                AnsiConsole.Ask<string>("[yellow]Press Enter to continue...[/]", "");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                AnsiConsole.Ask<string>("[yellow]Press Enter to continue...[/]", "");
            }
        }

        private async Task PerformStatsAsync(string scanRoot, string[] excludes)
        {
            AnsiConsole.Clear();

            try
            {
                scanRoot = Path.GetFullPath(scanRoot);
                if (!Directory.Exists(scanRoot))
                {
                    AnsiConsole.MarkupLine("[red]Error: Scan directory does not exist.[/]");
                    AnsiConsole.Ask<string>("[yellow]Press Enter to continue...[/]", "");
                    return;
                }

                var patterns = LanguagePatterns.For(_selectedLanguage);
                string fileExt = patterns.Extension;

                var codeFiles = Directory.GetFiles(scanRoot, $"*{fileExt}", SearchOption.AllDirectories)
                    .Where(f => !excludes.Any(ex =>
                        f.Contains($"{Path.DirectorySeparatorChar}{ex}{Path.DirectorySeparatorChar}") ||
                        f.EndsWith($"{Path.DirectorySeparatorChar}{ex}")))
                    .ToArray();

                var files = codeFiles
                    .Select(f => _analyzer.AnalyzeCode(File.ReadAllText(f), patterns, _selectedLanguage))
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

                AnsiConsole.Ask<string>("[yellow]Press Enter to continue...[/]", "");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                AnsiConsole.Ask<string>("[yellow]Press Enter to continue...[/]", "");
            }
        }

        private async Task PerformValidateAsync(string outputFile, string repoUrl)
        {
            AnsiConsole.Clear();

            try
            {
                if (!File.Exists(outputFile))
                {
                    AnsiConsole.MarkupLine($"[red]Error: File not found: {outputFile}[/]");
                    AnsiConsole.Ask<string>("[yellow]Press Enter to continue...[/]", "");
                    return;
                }

                string content = File.ReadAllText(outputFile);
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

                AnsiConsole.Ask<string>("[yellow]Press Enter to continue...[/]", "");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                AnsiConsole.Ask<string>("[yellow]Press Enter to continue...[/]", "");
            }
        }
    }
}
