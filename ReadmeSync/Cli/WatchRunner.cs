using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReadmeSync.Models;
using ReadmeSync.Services;
using Spectre.Console;

namespace ReadmeSync.Cli
{
    public class WatchRunner
    {
        private readonly RepoRootFinder _repoFinder = new();
        private readonly CodeAnalyzer _analyzer = new();
        private readonly MarkdownGenerator _markdownGen = new();
        private readonly TelemetryService _telemetry = new();
        private readonly string _scanRoot;
        private readonly string _outputFile;
        private readonly string _language;
        private readonly string _fileExt;
        private readonly string[] _excludes;
        private readonly string _repoUrl;
        private readonly bool _useEmojis;
        private readonly bool _noTracking;

        public WatchRunner(
            string scanRoot,
            string outputFile,
            string language,
            string fileExt,
            string[] excludes,
            string repoUrl,
            bool useEmojis,
            bool noTracking)
        {
            _scanRoot = scanRoot;
            _outputFile = outputFile;
            _language = language;
            _fileExt = fileExt;
            _excludes = excludes;
            _repoUrl = repoUrl;
            _useEmojis = useEmojis;
            _noTracking = noTracking;
        }

        public async Task RunAsync()
        {
            string repoRoot = _repoFinder.FindRepoRoot(_scanRoot, out _) ?? _scanRoot;

            AnsiConsole.MarkupLine($"[cyan]Watching {_scanRoot} for changes... (Press Ctrl+C to stop)[/]\n");

            using var watcher = new FileSystemWatcher(_scanRoot)
            {
                Filter = $"*{_fileExt}",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                IncludeSubdirectories = true
            };

            var debounceTimer = new System.Timers.Timer(1000);
            debounceTimer.AutoReset = false;
            debounceTimer.Elapsed += async (s, e) => await RegenerateAsync(repoRoot);

            void OnChanged(object sender, FileSystemEventArgs e)
            {
                if (ShouldExclude(e.FullPath))
                    return;

                debounceTimer.Stop();
                debounceTimer.Start();
            }

            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.EnableRaisingEvents = true;

            await Task.Delay(Timeout.Infinite);
        }

        private async Task RegenerateAsync(string repoRoot)
        {
            try
            {
                AnsiConsole.MarkupLine($"[yellow]🔄 Regenerating... [{DateTime.Now:HH:mm:ss}][/]");

                var patterns = LanguagePatterns.For(_language);

                var codeFiles = Directory.GetFiles(_scanRoot, $"*{_fileExt}", SearchOption.AllDirectories)
                    .Where(f => !_excludes.Any(ex =>
                        f.Contains($"{Path.DirectorySeparatorChar}{ex}{Path.DirectorySeparatorChar}") ||
                        f.EndsWith($"{Path.DirectorySeparatorChar}{ex}")))
                    .ToArray();

                var files = codeFiles
                    .Select(f =>
                    {
                        string text = File.ReadAllText(f);
                        var info = _analyzer.AnalyzeCode(text, patterns, _language);
                        if (info == null) return null;

                        string rel = Path.GetRelativePath(repoRoot, f).Replace(Path.DirectorySeparatorChar, '/');
                        string? fileUrl = _repoUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? $"{_repoUrl}/{rel}"
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

                _markdownGen.GenerateMarkdown(_outputFile, files, _language, _fileExt, _useEmojis, _repoUrl);

                AnsiConsole.MarkupLine($"[green]✓ Updated [{DateTime.Now:HH:mm:ss}][/]");

                Task? telemetryTask = null;
                if (!_noTracking)
                {
                    telemetryTask = _telemetry.SendTelemetryAsync(_language);
                    if (telemetryTask != null)
                    {
                        await Task.WhenAny(telemetryTask, Task.Delay(500));
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Error: {ex.Message}[/]");
            }
        }

        private bool ShouldExclude(string filePath)
        {
            return _excludes.Any(ex =>
                filePath.Contains($"{Path.DirectorySeparatorChar}{ex}{Path.DirectorySeparatorChar}") ||
                filePath.EndsWith($"{Path.DirectorySeparatorChar}{ex}"));
        }
    }
}
