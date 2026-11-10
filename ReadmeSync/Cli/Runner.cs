using System.Text;
using ReadmeSync.Models;
using ReadmeSync.Services;

namespace ReadmeSync.Cli
{
    /// <summary>
    /// Runs one scan-and-generate cycle.
    /// </summary>
    public static class Runner
    {
        public const int ExitOk = 0;
        public const int ExitOutOfDate = 1;
        public const int ExitError = 2;

        /// <summary>
        /// Scans the code base and writes, checks or prints the result.
        /// </summary>
        /// <param name="settings">The effective settings.</param>
        /// <param name="output">Receives the generated document in dry-run mode.</param>
        /// <param name="log">Receives progress messages.</param>
        /// <returns>0 on success, 1 when --check found the output file out of date.</returns>
        public static int Run(RunSettings settings, TextWriter output, TextWriter log)
        {
            if (settings.Quiet)
                log = TextWriter.Null;

            string scanRoot = Path.GetFullPath(settings.ScanRoot);
            string repoRoot = new RepoRootFinder().FindRepoRoot(scanRoot, out string? reason) ?? scanRoot;
            string outputFile = ResolveOutputFile(settings, repoRoot);

            log.WriteLine($"Language:           {string.Join(", ", settings.Languages)}");
            log.WriteLine($"Scanning directory: {scanRoot}");
            log.WriteLine($"Repo root:          {repoRoot}");
            log.WriteLine($"Detected by:        {reason ?? "(no marker; using scan directory)"}");
            if (settings.ConfigFile != null)
                log.WriteLine($"Settings file:      {settings.ConfigFile}");
            log.WriteLine($"Output file:        {outputFile}");
            log.WriteLine();

            var result = new ProjectScanner().Scan(new ScanOptions
            {
                ScanRoot = scanRoot,
                RepoRoot = repoRoot,
                Languages = settings.Languages,
                Excludes = settings.Excludes,
                RepoUrl = settings.RepoUrl,
                Branch = settings.Branch
            });

            foreach (string warning in result.Warnings)
                log.WriteLine($"Warning: {warning}");

            if (result.FilesScanned == 0)
            {
                log.WriteLine($"No source files found for: {string.Join(", ", settings.Languages)}. Nothing to do.");
                return ExitOk;
            }

            var groups = result.GroupByNamespace();
            log.WriteLine(
                $"Scanned {result.FilesScanned} file(s): {result.Types.Count} types in {groups.Count} packages, " +
                $"{result.Types.Sum(t => t.Methods.Count)} methods, {result.Types.Sum(t => t.Todos.Count)} TODOs.");
            if (result.FilesWithoutTypes.Count > 0)
                log.WriteLine($"{result.FilesWithoutTypes.Count} file(s) had no namespace or type and were left out.");

            string? existing = File.Exists(outputFile) ? File.ReadAllText(outputFile) : null;
            string document;
            bool upToDate;

            if (settings.Json)
            {
                document = new JsonGenerator().BuildJson(groups);
                upToDate = existing != null && Normalize(existing) == Normalize(document);
            }
            else
            {
                var options = new MarkdownOptions
                {
                    LanguageLabel = string.Join(", ", result.Languages.Select(l => LanguagePatterns.For(l).DisplayName)),
                    UseEmojis = settings.UseEmojis,
                    IncludeToc = settings.Toc,
                    IncludeTimestamp = settings.Timestamp
                };
                document = new MarkdownGenerator().BuildDocument(existing, groups, options);
                upToDate = existing != null && MarkdownGenerator.IsEquivalent(existing, document);
            }

            if (settings.DryRun)
            {
                output.Write(document);
                if (!document.EndsWith('\n'))
                    output.WriteLine();
                return ExitOk;
            }

            if (settings.Check)
            {
                log.WriteLine(upToDate
                    ? $"{Path.GetFileName(outputFile)} is up to date."
                    : $"{Path.GetFileName(outputFile)} is out of date. Run readmesync to update it.");
                return upToDate ? ExitOk : ExitOutOfDate;
            }

            if (upToDate)
            {
                log.WriteLine($"{Path.GetFileName(outputFile)} is already up to date; nothing written.");
                return ExitOk;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);
            File.WriteAllText(outputFile, document, new UTF8Encoding(false));
            log.WriteLine($"ReadmeSync completed successfully! Updated {outputFile}");
            return ExitOk;
        }

        private static string ResolveOutputFile(RunSettings settings, string repoRoot)
        {
            string name = settings.Output ?? (settings.Json ? "readmesync.json" : "README.md");
            string path = Path.GetFullPath(Path.IsPathRooted(name) ? name : Path.Combine(repoRoot, name));

            if (Directory.Exists(path))
                throw new CliException($"Output file is a directory: {path}");

            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (settings.Json && extension is ".md" or ".markdown")
                throw new CliException(
                    $"Refusing to write JSON into {Path.GetFileName(path)}. Name a .json output file, for example: readmesync --json . roadmap.json");

            return path;
        }

        private static string Normalize(string text) => text.Replace("\r\n", "\n").TrimEnd();
    }
}
