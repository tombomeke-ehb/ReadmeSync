using ReadmeSync.Models;
using ReadmeSync.Services;

namespace ReadmeSync.Cli
{
    /// <summary>
    /// The effective settings of one run: command line, settings file and defaults combined.
    /// </summary>
    public class RunSettings
    {
        public string ScanRoot { get; set; } = ".";
        public List<string> Languages { get; set; } = new() { "csharp" };

        /// <summary>Output file as given; a relative path is resolved against the repository root.</summary>
        public string? Output { get; set; }

        public string RepoUrl { get; set; } = "";
        public string? Branch { get; set; }
        public List<string> Excludes { get; set; } = new(ProjectScanner.DefaultExcludes);
        public bool UseEmojis { get; set; }
        public bool Json { get; set; }
        public bool Toc { get; set; }
        public bool Timestamp { get; set; } = true;
        public bool Check { get; set; }
        public bool DryRun { get; set; }
        public bool Quiet { get; set; }
        public bool NoTracking { get; set; }

        /// <summary>Settings file that was applied, if any.</summary>
        public string? ConfigFile { get; set; }

        /// <summary>
        /// Combines the command line with the settings file. Command-line values win.
        /// </summary>
        public static RunSettings Resolve(CliOptions cli, string workingDirectory)
        {
            string? scanArg = cli.Positionals.ElementAtOrDefault(0);
            string? configFile = FindConfig(cli, scanArg, workingDirectory);
            var file = configFile != null ? AppSettings.Load(configFile) : new AppSettings();
            string configDirectory = configFile != null ? Path.GetDirectoryName(configFile)! : workingDirectory;

            string scanRoot =
                scanArg != null ? Path.GetFullPath(scanArg, workingDirectory)
                : !string.IsNullOrWhiteSpace(file.ScanDirectory) ? Path.GetFullPath(file.ScanDirectory, configDirectory)
                : Path.GetFullPath(workingDirectory);

            if (!Directory.Exists(scanRoot))
                throw new CliException($"Scan directory not found: {scanRoot}");

            var excludes = new List<string>();
            if (!cli.NoDefaultExcludes)
                excludes.AddRange(ProjectScanner.DefaultExcludes);
            if (file.Exclude != null)
                excludes.AddRange(file.Exclude.Where(e => !string.IsNullOrWhiteSpace(e)));
            excludes.AddRange(cli.Excludes);

            return new RunSettings
            {
                ScanRoot = scanRoot,
                Languages = ParseLanguages(cli.Language ?? file.Language),
                Output = cli.Output ?? cli.Positionals.ElementAtOrDefault(1) ?? NullIfEmpty(file.OutputFile),
                RepoUrl = (cli.RepoUrl ?? file.RepositoryUrl ?? "").Trim().TrimEnd('/'),
                Branch = NullIfEmpty(cli.Branch ?? file.Branch),
                Excludes = excludes.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                UseEmojis = cli.UseEmojis || file.UseEmojis == true,
                Json = cli.Json || file.OutputJson == true,
                Toc = cli.Toc || file.Toc == true,
                Timestamp = !cli.NoTimestamp && file.Timestamp != false,
                Check = cli.Check,
                DryRun = cli.DryRun,
                Quiet = cli.Quiet,
                NoTracking = cli.NoTracking || file.NoTracking == true,
                ConfigFile = configFile
            };
        }

        /// <summary>
        /// Turns "csharp,py" or "auto" into language keys. Nothing given means C#.
        /// </summary>
        public static List<string> ParseLanguages(string? value)
        {
            var languages = new List<string>();
            var parts = (value ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                string key = part.Equals("auto", StringComparison.OrdinalIgnoreCase)
                    ? "auto"
                    : LanguagePatterns.Normalize(part)
                      ?? throw new CliException(
                          $"Unsupported language '{part}'. Supported: auto, {string.Join(", ", LanguagePatterns.SupportedLanguages)}.");

                if (!languages.Contains(key))
                    languages.Add(key);
            }

            if (languages.Count == 0)
                languages.Add("csharp");
            return languages;
        }

        private static string? FindConfig(CliOptions cli, string? scanArg, string workingDirectory)
        {
            if (cli.ConfigPath != null)
            {
                string path = Path.GetFullPath(cli.ConfigPath, workingDirectory);
                if (!File.Exists(path))
                    throw new CliException($"Settings file not found: {path}");
                return path;
            }

            var candidates = new List<string>();
            if (scanArg != null)
                candidates.Add(Path.Combine(Path.GetFullPath(scanArg, workingDirectory), AppSettings.FileName));
            candidates.Add(Path.Combine(Path.GetFullPath(workingDirectory), AppSettings.FileName));

            return candidates.FirstOrDefault(File.Exists);
        }

        private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
