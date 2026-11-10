using ReadmeSync.Models;

namespace ReadmeSync.Cli
{
    /// <summary>
    /// Raised for mistakes in the command line or settings; the message is shown to the user as is.
    /// </summary>
    public class CliException : Exception
    {
        public CliException(string message) : base(message) { }
    }

    /// <summary>
    /// The command line as typed, before defaults and the settings file are applied.
    /// </summary>
    public class CliOptions
    {
        public List<string> Positionals { get; } = new();
        public List<string> Excludes { get; } = new();

        public string? Language { get; set; }
        public string? Output { get; set; }
        public string? RepoUrl { get; set; }
        public string? Branch { get; set; }
        public string? ConfigPath { get; set; }

        public bool UseEmojis { get; set; }
        public bool Json { get; set; }
        public bool Toc { get; set; }
        public bool NoTimestamp { get; set; }
        public bool NoDefaultExcludes { get; set; }
        public bool Check { get; set; }
        public bool DryRun { get; set; }
        public bool Quiet { get; set; }
        public bool NoTracking { get; set; }
        public bool Tui { get; set; }
        public bool Init { get; set; }
        public bool Help { get; set; }
        public bool Version { get; set; }

        /// <summary>
        /// Parses command-line arguments. Options may appear anywhere and accept both
        /// "--name value" and "--name=value".
        /// </summary>
        public static CliOptions Parse(string[] args)
        {
            var options = new CliOptions();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (arg == "--")
                {
                    options.Positionals.AddRange(args.Skip(i + 1));
                    break;
                }

                if (!arg.StartsWith('-') || arg == "-")
                {
                    options.AddPositional(arg);
                    continue;
                }

                string name = arg;
                string? inlineValue = null;
                int equals = arg.IndexOf('=');
                if (arg.StartsWith("--") && equals > 0)
                {
                    name = arg[..equals];
                    inlineValue = arg[(equals + 1)..];
                }

                string Value()
                {
                    if (inlineValue != null) return inlineValue;
                    if (i + 1 >= args.Length)
                        throw new CliException($"Option {name} needs a value.");
                    return args[++i];
                }

                switch (name)
                {
                    case "--lang" or "--language" or "-l": options.Language = Value(); break;
                    case "--output" or "-o": options.Output = Value(); break;
                    case "--repo-url" or "--repo": options.RepoUrl = Value(); break;
                    case "--branch": options.Branch = Value(); break;
                    case "--config" or "-c": options.ConfigPath = Value(); break;
                    case "--exclude" or "-e":
                        options.Excludes.AddRange(Value().Split(new[] { ',', ';' },
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        break;

                    case "--use-emojis" or "--emojis": options.UseEmojis = true; break;
                    case "--json": options.Json = true; break;
                    case "--toc": options.Toc = true; break;
                    case "--no-timestamp": options.NoTimestamp = true; break;
                    case "--no-default-excludes": options.NoDefaultExcludes = true; break;
                    case "--check": options.Check = true; break;
                    case "--dry-run" or "--stdout": options.DryRun = true; break;
                    case "--quiet" or "-q": options.Quiet = true; break;
                    case "--no-tracking": options.NoTracking = true; break;
                    case "--tui": options.Tui = true; break;
                    case "--init": options.Init = true; break;
                    case "--help" or "-h" or "-?": options.Help = true; break;
                    case "--version" or "-v": options.Version = true; break;

                    default:
                        throw new CliException($"Unknown option: {name}");
                }

                if (inlineValue != null && IsFlag(name))
                    throw new CliException($"Option {name} does not take a value.");
            }

            if (options.Positionals.Count > 2)
                throw new CliException($"Unexpected argument: {options.Positionals[2]}");
            if (options.Check && options.DryRun)
                throw new CliException("--check and --dry-run cannot be combined.");

            return options;
        }

        private void AddPositional(string arg)
        {
            // A URL anywhere among the arguments is the repository URL.
            if (arg.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                arg.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                RepoUrl ??= arg;
                return;
            }
            Positionals.Add(arg);
        }

        private static bool IsFlag(string name) => name is not (
            "--lang" or "--language" or "--output" or "--repo-url" or "--repo" or "--branch" or "--config" or "--exclude");

        /// <summary>The help text.</summary>
        public static string Usage()
        {
            string languages = string.Join("|", LanguagePatterns.SupportedLanguages);
            return $"""
                Usage:
                  readmesync [options] [scan-root] [output-file] [repo-url]

                Arguments:
                  scan-root               Directory to scan (default: current directory)
                  output-file             File to update, relative to the repository root (default: README.md)
                  repo-url                Repository URL, used to link every type to its source file

                Options:
                  -l, --lang <list>       {languages}
                                          Comma-separated for several, or 'auto' to detect (default: csharp)
                  -o, --output <file>     Same as the output-file argument
                      --repo-url <url>    Same as the repo-url argument
                      --branch <name>     Branch, tag or commit used in source links (default: HEAD)
                  -e, --exclude <list>    Extra folders or files to skip; wildcards and relative paths allowed
                      --no-default-excludes
                                          Do not skip bin, obj, node_modules and the other default folders
                      --json              Write the analysis as JSON instead of markdown
                      --toc               Add a table of contents to the generated section
                      --use-emojis        Use emoji icons in the generated section
                      --no-timestamp      Leave out the 'Last updated' line
                      --check             Write nothing; exit with code 1 when the output file is out of date
                      --dry-run           Write nothing; print the result to standard output
                  -c, --config <file>     Settings file (default: {AppSettings.FileName} in the scan root or current directory)
                      --init              Create a {AppSettings.FileName} with the default settings
                      --tui               Start the interactive mode
                  -q, --quiet             Only print errors
                      --no-tracking       Disable anonymous usage statistics
                  -v, --version           Show the version
                  -h, --help              Show this help

                Examples:
                  readmesync . README.md
                  readmesync --lang auto --toc . README.md https://github.com/USER/REPO
                  readmesync --lang java,kotlin ./src ROADMAP.md
                  readmesync --json . roadmap.json
                  readmesync --check            (in CI: fail when the README is out of date)

                Exit codes:
                  0  success    1  --check found the output out of date    2  error
                """;
        }
    }
}
