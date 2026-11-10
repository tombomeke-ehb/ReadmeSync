using System.Text.RegularExpressions;
using ReadmeSync.Models;

namespace ReadmeSync.Services
{
    /// <summary>
    /// What to scan and how.
    /// </summary>
    public class ScanOptions
    {
        /// <summary>Directory to scan recursively.</summary>
        public string ScanRoot { get; set; } = ".";

        /// <summary>Directory that file paths are made relative to. Defaults to the scan root.</summary>
        public string? RepoRoot { get; set; }

        /// <summary>Language keys to scan, or "auto" to scan every supported language that is present.</summary>
        public IReadOnlyCollection<string> Languages { get; set; } = new[] { "csharp" };

        /// <summary>Directory or file names (wildcards allowed) and relative paths to skip.</summary>
        public IReadOnlyCollection<string> Excludes { get; set; } = ProjectScanner.DefaultExcludes;

        /// <summary>Repository URL used to link each type to its source file. Optional.</summary>
        public string RepoUrl { get; set; } = "";

        /// <summary>Branch, tag or commit used in source links. Defaults to HEAD.</summary>
        public string? Branch { get; set; }
    }

    /// <summary>
    /// Outcome of a scan.
    /// </summary>
    public class ScanResult
    {
        /// <summary>Every type found, sorted by namespace, file and position.</summary>
        public List<CodeFileInfo> Types { get; } = new();

        /// <summary>Number of source files read.</summary>
        public int FilesScanned { get; set; }

        /// <summary>Files in which no documentable type was found.</summary>
        public List<string> FilesWithoutTypes { get; } = new();

        /// <summary>Language keys that had at least one source file, in display order.</summary>
        public List<string> Languages { get; } = new();

        /// <summary>Problems that did not stop the scan, such as unreadable files.</summary>
        public List<string> Warnings { get; } = new();

        /// <summary>The types grouped by namespace, in output order.</summary>
        public List<IGrouping<string, CodeFileInfo>> GroupByNamespace() =>
            Types.GroupBy(t => t.Namespace).ToList();
    }

    /// <summary>
    /// Service responsible for walking a directory tree and analyzing every source file in it.
    /// </summary>
    public class ProjectScanner
    {
        /// <summary>Folders that are skipped unless the caller says otherwise.</summary>
        public static readonly IReadOnlyList<string> DefaultExcludes = new[]
        {
            "bin", "obj", "node_modules", ".git", ".vs", ".idea", ".vscode", "__pycache__",
            "dist", "build", "out", "target", "vendor", "venv", ".venv", "coverage", "TestResults"
        };

        private readonly CodeAnalyzer _analyzer;

        public ProjectScanner() : this(new CodeAnalyzer()) { }

        public ProjectScanner(CodeAnalyzer analyzer)
        {
            _analyzer = analyzer;
        }

        /// <summary>
        /// Scans the directory tree and returns every type found in it.
        /// </summary>
        public ScanResult Scan(ScanOptions options)
        {
            string scanRoot = Path.GetFullPath(options.ScanRoot);
            if (!Directory.Exists(scanRoot))
                throw new DirectoryNotFoundException($"Scan directory not found: {scanRoot}");

            string repoRoot = Path.GetFullPath(options.RepoRoot ?? scanRoot);
            var wanted = ResolveLanguages(options.Languages);
            var excludes = options.Excludes.Select(e => new ExcludeRule(e)).ToList();
            var result = new ScanResult();
            var seenLanguages = new HashSet<string>();

            foreach (string file in EnumerateFiles(scanRoot, scanRoot, excludes, result))
            {
                string? language = LanguagePatterns.FromFileName(Path.GetFileName(file));
                if (language == null || !wanted.Contains(language))
                    continue;

                string text;
                try
                {
                    text = File.ReadAllText(file);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    result.Warnings.Add($"Could not read {file}: {ex.Message}");
                    continue;
                }

                result.FilesScanned++;
                seenLanguages.Add(language);

                var patterns = LanguagePatterns.For(language);
                var types = _analyzer.AnalyzeAll(text, patterns, language);
                string relative = ToSlashes(Path.GetRelativePath(repoRoot, file));

                if (types.Count == 0)
                {
                    result.FilesWithoutTypes.Add(relative);
                    continue;
                }

                string? link = RepoLinkBuilder.BuildFileUrl(options.RepoUrl, relative, options.Branch);
                string folder = ToSlashes(Path.GetRelativePath(scanRoot, Path.GetDirectoryName(file)!));

                foreach (var type in types)
                {
                    type.Path = relative;
                    type.Link = link;

                    // Languages without namespaces are grouped by folder instead of one big bucket.
                    if (patterns.FallbackNamespace.Length > 0 && type.Namespace == patterns.FallbackNamespace && folder != ".")
                        type.Namespace = folder;

                    result.Types.Add(type);
                }
            }

            result.Languages.AddRange(LanguagePatterns.SupportedLanguages.Where(seenLanguages.Contains));

            var sorted = result.Types
                .OrderBy(t => t.Namespace, StringComparer.OrdinalIgnoreCase)
                .ThenBy(t => t.Namespace, StringComparer.Ordinal)
                .ThenBy(t => t.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(t => t.Line)
                .ToList();
            result.Types.Clear();
            result.Types.AddRange(sorted);

            return result;
        }

        private static HashSet<string> ResolveLanguages(IReadOnlyCollection<string> languages)
        {
            var wanted = new HashSet<string>();
            foreach (string language in languages)
            {
                if (string.Equals(language?.Trim(), "auto", StringComparison.OrdinalIgnoreCase))
                {
                    wanted.UnionWith(LanguagePatterns.SupportedLanguages);
                    continue;
                }

                string key = LanguagePatterns.Normalize(language)
                    ?? throw new ArgumentException(
                        $"Unsupported language '{language}'. Supported: auto, {string.Join(", ", LanguagePatterns.SupportedLanguages)}.");
                wanted.Add(key);
            }

            if (wanted.Count == 0)
                wanted.Add("csharp");
            return wanted;
        }

        private static IEnumerable<string> EnumerateFiles(string directory, string scanRoot, List<ExcludeRule> excludes, ScanResult result)
        {
            string[] files, directories;
            try
            {
                files = Directory.GetFiles(directory);
                directories = Directory.GetDirectories(directory);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                result.Warnings.Add($"Could not read directory {directory}: {ex.Message}");
                yield break;
            }

            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            Array.Sort(directories, StringComparer.OrdinalIgnoreCase);

            foreach (string file in files)
            {
                if (!IsExcluded(file, scanRoot, excludes))
                    yield return file;
            }

            foreach (string sub in directories)
            {
                if (IsExcluded(sub, scanRoot, excludes))
                    continue;

                foreach (string file in EnumerateFiles(sub, scanRoot, excludes, result))
                    yield return file;
            }
        }

        private static bool IsExcluded(string path, string scanRoot, List<ExcludeRule> excludes)
        {
            string name = Path.GetFileName(path);
            string relative = ToSlashes(Path.GetRelativePath(scanRoot, path));
            return excludes.Any(rule => rule.Matches(name, relative));
        }

        private static string ToSlashes(string path) => path.Replace('\\', '/');

        private sealed class ExcludeRule
        {
            private readonly string _text;
            private readonly Regex? _glob;
            private readonly bool _isPath;

            public ExcludeRule(string pattern)
            {
                _text = ToSlashes(pattern.Trim()).Trim('/');
                if (_text.StartsWith("./")) _text = _text[2..];
                _isPath = _text.Contains('/');

                if (_text.Contains('*') || _text.Contains('?'))
                {
                    string regex = "^" + Regex.Escape(_text).Replace(@"\*", _isPath ? ".*" : "[^/]*").Replace(@"\?", "[^/]") + "$";
                    _glob = new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                }
            }

            public bool Matches(string name, string relativePath)
            {
                if (_text.Length == 0)
                    return false;

                string subject = _isPath ? relativePath : name;
                if (_glob != null)
                    return _glob.IsMatch(subject);

                return string.Equals(subject, _text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
