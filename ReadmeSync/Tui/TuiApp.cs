using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private string _selectedLanguage = "csharp";
        private string _selectedScanRoot = ".";
        private string _selectedOutputFile = "README.md";
        private string _selectedRepoUrl = "";
        private bool _useEmojis = false;
        private bool _noTracking = false;

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
                            "⚙️ Settings",
                            "❌ Exit"
                        }));

                switch (choice)
                {
                    case "📖 Scan & Generate README":
                        await ScanAndGenerate("README.md");
                        break;
                    case "🗺️ Scan & Generate ROADMAP":
                        await ScanAndGenerate("ROADMAP.md");
                        break;
                    case "📊 Scan & Export JSON":
                        await ScanAndExportJson();
                        break;
                    case "👁️ Dry Run (Preview)":
                        await DryRunPreview();
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

            AnsiConsole.MarkupLine("[cyan]v2.0.0 - Made by Tombomeke Studios[/]\n");
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

        private async Task ScanAndGenerate(string defaultOutputFile)
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

            await PerformScan(scanRoot, outputFile, repoUrl, excludes, isJson: false);
        }

        private async Task ScanAndExportJson()
        {
            AnsiConsole.Clear();
            SelectLanguage();

            string scanRoot = AnsiConsole.Ask<string>("[cyan]Enter scan root directory:[/]", _selectedScanRoot);
            string outputFile = AnsiConsole.Ask<string>("[cyan]Enter output filename (JSON):[/]", "roadmap.json");

            var excludeInput = AnsiConsole.Ask<string>("[cyan]Exclude folders (comma-separated, or press Enter for defaults):[/]", "");
            var excludes = string.IsNullOrWhiteSpace(excludeInput)
                ? new[] { "bin", "obj", "node_modules", ".git", ".vs", "__pycache__", "dist", "build" }
                : excludeInput.Split(',').Select(x => x.Trim()).ToArray();

            await PerformScan(scanRoot, outputFile, "", excludes, isJson: true);
        }

        private async Task DryRunPreview()
        {
            AnsiConsole.Clear();
            SelectLanguage();

            string scanRoot = AnsiConsole.Ask<string>("[cyan]Enter scan root directory:[/]", _selectedScanRoot);
            string repoUrl = AnsiConsole.Ask<string>("[cyan]Enter GitHub URL (optional):[/]", _selectedRepoUrl);

            var excludeInput = AnsiConsole.Ask<string>("[cyan]Exclude folders (comma-separated, or press Enter for defaults):[/]", "");
            var excludes = string.IsNullOrWhiteSpace(excludeInput)
                ? new[] { "bin", "obj", "node_modules", ".git", ".vs", "__pycache__", "dist", "build" }
                : excludeInput.Split(',').Select(x => x.Trim()).ToArray();

            await PerformDryRun(scanRoot, repoUrl, excludes);
        }

        private async Task PerformScan(string scanRoot, string outputFile, string repoUrl, string[] excludes, bool isJson)
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

                string repoRoot = _repoFinder.FindRepoRoot(scanRoot, out var reason) ?? scanRoot;
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
                        task3.Increment(1);

                        var task4 = ctx.AddTask("[cyan]Generating output...[/]", maxValue: 1);

                        var files = codeFiles
                            .Select(f =>
                            {
                                string text = File.ReadAllText(f);
                                var info = _analyzer.AnalyzeCode(text, patterns, _selectedLanguage);
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

                        if (isJson)
                        {
                            _jsonGen.GenerateJson(outputFile, files);
                        }
                        else
                        {
                            _markdownGen.GenerateMarkdown(outputFile, files, _selectedLanguage, fileExt, _useEmojis, repoUrl);
                        }

                        task4.Increment(1);
                    });

                AnsiConsole.MarkupLine("[green]✓ Scan completed successfully![/]\n");

                var table = new Table();
                table.AddColumn("[cyan]Metric[/]");
                table.AddColumn("[cyan]Count[/]");
                table.AddRow("Total Files", codeFiles.Length.ToString());
                AnsiConsole.Write(table);

                AnsiConsole.MarkupLine($"\n[green]Output file:[/] {Path.GetFullPath(outputFile)}");

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

        private async Task PerformDryRun(string scanRoot, string repoUrl, string[] excludes)
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
                        var info = _analyzer.AnalyzeCode(text, patterns, _selectedLanguage);
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

                string preview = _markdownGen.PreviewMarkdown(files, _selectedLanguage, fileExt, _useEmojis, repoUrl);

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
    }
}
