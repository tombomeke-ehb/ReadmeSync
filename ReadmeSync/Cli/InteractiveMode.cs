using ReadmeSync.Models;
using ReadmeSync.Services;
using Spectre.Console;

namespace ReadmeSync.Cli
{
    /// <summary>
    /// The menu-driven interactive mode (readmesync --tui, or readmesync without arguments).
    /// </summary>
    public static class InteractiveMode
    {
        private const string Generate = "Generate documentation";
        private const string Settings = "Settings";
        private const string About = "About";
        private const string Exit = "Exit";

        public static void Run()
        {
            while (true)
            {
                AnsiConsole.Clear();
                DisplayHeader();

                string choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold cyan]What would you like to do?[/]")
                        .AddChoices(Generate, Settings, About, Exit)
                        .HighlightStyle("bold green"));

                switch (choice)
                {
                    case Generate: RunGenerationFlow(); break;
                    case Settings: ShowSettings(); break;
                    case About: ShowAbout(); break;
                    default:
                        AnsiConsole.MarkupLine("[yellow]\nThanks for using ReadmeSync!\n[/]");
                        return;
                }

                AnsiConsole.MarkupLine("\n[yellow]Press Enter to continue...[/]");
                Console.ReadLine();
            }
        }

        private static void DisplayHeader()
        {
            AnsiConsole.MarkupLine("[bold cyan]  ╔═══════════════════════════════════════════════════════════╗[/]");
            AnsiConsole.MarkupLine("[bold cyan]  ║                                                           ║[/]");
            AnsiConsole.MarkupLine("[bold cyan]  ║     ██████╗ ███████╗ █████╗ ██████╗ ███╗   ███╗███████╗   ║[/]");
            AnsiConsole.MarkupLine("[bold cyan]  ║     ██╔══██╗██╔════╝██╔══██╗██╔══██╗████╗ ████║██╔════╝   ║[/]");
            AnsiConsole.MarkupLine("[bold cyan]  ║     ██████╔╝█████╗  ███████║██║  ██║██╔████╔██║█████╗     ║[/]");
            AnsiConsole.MarkupLine("[bold cyan]  ║     ██╔══██╗██╔══╝  ██╔══██║██║  ██║██║╚██╔╝██║██╔══╝     ║[/]");
            AnsiConsole.MarkupLine("[bold cyan]  ║     ██║  ██║███████╗██║  ██║██████╔╝██║ ╚═╝ ██║███████╗   ║[/]");
            AnsiConsole.MarkupLine("[bold cyan]  ║     ╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝╚═════╝ ╚═╝     ╚═╝╚══════╝   ║[/]");
            AnsiConsole.MarkupLine("[bold cyan]  ║           Auto-Generate README & ROADMAP Files            ║[/]");
            AnsiConsole.MarkupLine("[bold cyan]  ║                 Made by Tombomeke Studios                 ║[/]");
            AnsiConsole.MarkupLine("[bold cyan]  ║                                                           ║[/]");
            AnsiConsole.MarkupLine("[bold cyan]  ╚═══════════════════════════════════════════════════════════╝[/]\n");
        }

        private static void RunGenerationFlow()
        {
            AnsiConsole.MarkupLine("[bold]Configuration[/]\n");

            var languageChoices = new List<string> { "AUTO (detect)" };
            languageChoices.AddRange(LanguagePatterns.SupportedLanguages.Select(l => LanguagePatterns.For(l).DisplayName));

            string languageChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold cyan]Programming language[/]")
                    .AddChoices(languageChoices)
                    .HighlightStyle("bold green"));

            string language = languageChoice.StartsWith("AUTO")
                ? "auto"
                : LanguagePatterns.SupportedLanguages.First(l => LanguagePatterns.For(l).DisplayName == languageChoice);

            string scanRoot = AnsiConsole.Prompt(
                new TextPrompt<string>("[bold cyan]Scan directory[/]")
                    .DefaultValue(".")
                    .Validate(input => Directory.Exists(input)
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]Directory not found[/]")));

            bool json = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold cyan]Output format[/]")
                    .AddChoices("Markdown", "JSON")
                    .HighlightStyle("bold green")) == "JSON";

            string outputFile = AnsiConsole.Prompt(
                new TextPrompt<string>("[bold cyan]Output file[/]")
                    .DefaultValue(json ? "readmesync.json" : "README.md"));

            string repoUrl = AnsiConsole.Prompt(
                new TextPrompt<string>("[bold cyan]Repository URL[/] [dim](press Enter to skip)[/]")
                    .AllowEmpty());

            bool useEmojis = false, toc = false;
            if (!json)
            {
                toc = AnsiConsole.Confirm("[bold cyan]Add a table of contents?[/]", false);
                useEmojis = AnsiConsole.Confirm("[bold cyan]Use emojis?[/]", false);
            }

            var summary = new Table().Border(TableBorder.Rounded).HideHeaders().AddColumn("").AddColumn("");
            summary.AddRow("Language", $"[green]{Markup.Escape(languageChoice)}[/]");
            summary.AddRow("Directory", $"[green]{Markup.Escape(Path.GetFullPath(scanRoot))}[/]");
            summary.AddRow("Output", $"[green]{Markup.Escape(outputFile)}[/]");
            if (!string.IsNullOrWhiteSpace(repoUrl))
                summary.AddRow("Repository", $"[green]{Markup.Escape(repoUrl)}[/]");
            summary.AddRow("Format", $"[green]{(json ? "JSON" : "Markdown")}[/]");
            if (!json)
            {
                summary.AddRow("Contents", $"[green]{(toc ? "Yes" : "No")}[/]");
                summary.AddRow("Emojis", $"[green]{(useEmojis ? "Yes" : "No")}[/]");
            }
            AnsiConsole.Write(summary);

            if (!AnsiConsole.Confirm("[bold yellow]Proceed with generation?[/]", true))
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
                return;
            }

            var options = new CliOptions
            {
                Language = language,
                Output = outputFile,
                RepoUrl = string.IsNullOrWhiteSpace(repoUrl) ? null : repoUrl.Trim(),
                Json = json,
                Toc = toc,
                UseEmojis = useEmojis
            };
            options.Positionals.Add(scanRoot);

            AnsiConsole.WriteLine();
            int exitCode = Program.Execute(options, showBanner: false);
            AnsiConsole.MarkupLine(exitCode == Runner.ExitOk
                ? "[green]Generation complete.[/]"
                : "[red]Generation failed.[/]");
        }

        private static void ShowSettings()
        {
            AnsiConsole.MarkupLine("[bold]Settings & Configuration[/]\n");

            string languages = string.Join(", ", LanguagePatterns.SupportedLanguages.Select(l => LanguagePatterns.For(l).DisplayName));
            string configFile = Path.Combine(Directory.GetCurrentDirectory(), AppSettings.FileName);

            var settings = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[bold cyan]Setting[/]")
                .AddColumn("[bold cyan]Value[/]");

            settings.AddRow("[yellow]Language support[/]", $"[green]{Markup.Escape(languages)}[/]");
            settings.AddRow("[yellow]Output formats[/]", "[green]Markdown, JSON[/]");
            settings.AddRow("[yellow]Default excludes[/]", $"[green]{Markup.Escape(string.Join(", ", ProjectScanner.DefaultExcludes))}[/]");
            settings.AddRow("[yellow]Settings file[/]", File.Exists(configFile)
                ? $"[green]{Markup.Escape(configFile)}[/]"
                : $"[dim]none (create one with: readmesync --init)[/]");
            settings.AddRow("[yellow]Version[/]", $"[green]{Markup.Escape(TelemetryService.GetToolVersion())}[/]");
            settings.AddRow("[yellow]License[/]", "[green]MIT[/]");

            AnsiConsole.Write(settings);
        }

        private static void ShowAbout()
        {
            AnsiConsole.MarkupLine("[bold]About ReadmeSync[/]\n");

            string languages = string.Join(", ", LanguagePatterns.SupportedLanguages.Select(l => LanguagePatterns.For(l).DisplayName));

            var about = new Panel(
                new Text(
                    "ReadmeSync is a CLI tool that generates README and ROADMAP\n" +
                    "documentation from your source code.\n\n" +
                    "Features:\n" +
                    $"  • Languages: {languages}\n" +
                    "  • Namespaces, types, public methods and doc comments\n" +
                    "  • TODO, FIXME and HACK extraction\n" +
                    "  • Links to the source files in your repository\n" +
                    "  • JSON export for programmatic access\n" +
                    "  • --check mode for CI\n\n" +
                    "Author: Tombomeke Studios\n" +
                    "Repository: github.com/tombomeke-ehb/ReadmeSync\n" +
                    "License: MIT"))
            {
                Border = BoxBorder.Rounded,
                Padding = new Padding(2, 1, 2, 1)
            };

            AnsiConsole.Write(about);
        }
    }
}
