using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;
using ReadmeSync.Cli;
using ReadmeSync.Models;
using ReadmeSync.Services;

namespace ReadmeSync.Tests
{
    public class CliTests
    {
        private const string Source = "namespace App\n{\n    /// <summary>The API.</summary>\n    public class Api\n    {\n        public void Run() { }\n        // TODO: Add paging\n    }\n}\n";

        private static int Run(TempDirectory dir, out string stdout, params string[] args)
        {
            var settings = RunSettings.Resolve(CliOptions.Parse(args), dir.Path);
            settings.NoTracking = true;
            var output = new StringWriter();
            int exitCode = Runner.Run(settings, output, TextWriter.Null);
            stdout = output.ToString();
            return exitCode;
        }

        // ------------------------------------------------------------------
        // Parsing
        // ------------------------------------------------------------------

        [Fact]
        public void Parse_LegacyPositionalArguments_StillWork()
        {
            var options = CliOptions.Parse(new[] { "--lang", "java", "./src", "ROADMAP.md", "https://github.com/u/r", "--use-emojis", "--no-tracking" });

            Assert.Equal("java", options.Language);
            Assert.Equal(new[] { "./src", "ROADMAP.md" }, options.Positionals);
            Assert.Equal("https://github.com/u/r", options.RepoUrl);
            Assert.True(options.UseEmojis);
            Assert.True(options.NoTracking);
            Assert.False(options.Json);
        }

        [Fact]
        public void Parse_NewOptions_InBothSpellings()
        {
            var options = CliOptions.Parse(new[]
            {
                "-l", "auto", "--output=docs/API.md", "--exclude", "a, b;c", "-e", "d", "--branch=develop",
                "--toc", "--no-timestamp", "--check", "-q", "--no-default-excludes"
            });

            Assert.Equal("auto", options.Language);
            Assert.Equal("docs/API.md", options.Output);
            Assert.Equal(new[] { "a", "b", "c", "d" }, options.Excludes);
            Assert.Equal("develop", options.Branch);
            Assert.True(options.Toc && options.NoTimestamp && options.Check && options.Quiet && options.NoDefaultExcludes);
        }

        [Theory]
        [InlineData("--nope")]
        [InlineData("--lang")]
        [InlineData("--json=yes")]
        [InlineData("--check", "--dry-run")]
        [InlineData("a", "b", "c")]
        public void Parse_InvalidInput_Throws(params string[] args)
        {
            Assert.Throws<CliException>(() => CliOptions.Parse(args));
        }

        [Fact]
        public void Usage_MentionsEveryLanguage()
        {
            string usage = CliOptions.Usage();
            Assert.All(LanguagePatterns.SupportedLanguages, language => Assert.Contains(language, usage));
        }

        // ------------------------------------------------------------------
        // Settings
        // ------------------------------------------------------------------

        [Fact]
        public void Resolve_Defaults()
        {
            using var dir = new TempDirectory();

            var settings = RunSettings.Resolve(CliOptions.Parse(System.Array.Empty<string>()), dir.Path);

            Assert.Equal(Path.GetFullPath(dir.Path), settings.ScanRoot);
            Assert.Equal(new[] { "csharp" }, settings.Languages);
            Assert.Null(settings.Output);
            Assert.True(settings.Timestamp);
            Assert.Equal(ProjectScanner.DefaultExcludes, settings.Excludes);
            Assert.Null(settings.ConfigFile);
        }

        [Fact]
        public void Resolve_ExcludeAddsToTheDefaults()
        {
            using var dir = new TempDirectory();

            var added = RunSettings.Resolve(CliOptions.Parse(new[] { "--exclude", "legacy" }), dir.Path);
            var replaced = RunSettings.Resolve(CliOptions.Parse(new[] { "--exclude", "legacy", "--no-default-excludes" }), dir.Path);

            Assert.Contains("obj", added.Excludes);
            Assert.Contains("legacy", added.Excludes);
            Assert.Equal(new[] { "legacy" }, replaced.Excludes);
        }

        [Fact]
        public void Resolve_SettingsFileIsApplied_AndCommandLineWins()
        {
            using var dir = new TempDirectory();
            dir.Write("src/Api.cs", Source);
            dir.Write(AppSettings.FileName, """
                {
                  // comments are allowed
                  "language": "csharp, python",
                  "scanDirectory": "src",
                  "outputFile": "DOCS.md",
                  "repositoryUrl": "https://github.com/u/r/",
                  "exclude": ["generated"],
                  "toc": true,
                  "timestamp": false,
                  "noTracking": true,
                }
                """);

            var fromFile = RunSettings.Resolve(CliOptions.Parse(System.Array.Empty<string>()), dir.Path);

            Assert.Equal(Path.Combine(Path.GetFullPath(dir.Path), "src"), fromFile.ScanRoot);
            Assert.Equal(new[] { "csharp", "python" }, fromFile.Languages);
            Assert.Equal("DOCS.md", fromFile.Output);
            Assert.Equal("https://github.com/u/r", fromFile.RepoUrl);
            Assert.Contains("generated", fromFile.Excludes);
            Assert.True(fromFile.Toc);
            Assert.False(fromFile.Timestamp);
            Assert.True(fromFile.NoTracking);
            Assert.NotNull(fromFile.ConfigFile);

            var overridden = RunSettings.Resolve(CliOptions.Parse(new[] { "--lang", "go", "-o", "OTHER.md" }), dir.Path);

            Assert.Equal(new[] { "go" }, overridden.Languages);
            Assert.Equal("OTHER.md", overridden.Output);
        }

        [Fact]
        public void Resolve_BadInput_Throws()
        {
            using var dir = new TempDirectory();

            Assert.Throws<CliException>(() => RunSettings.Resolve(CliOptions.Parse(new[] { "missing-folder" }), dir.Path));
            Assert.Throws<CliException>(() => RunSettings.Resolve(CliOptions.Parse(new[] { "--lang", "cobol" }), dir.Path));
            Assert.Throws<CliException>(() => RunSettings.Resolve(CliOptions.Parse(new[] { "--config", "nope.json" }), dir.Path));

            dir.Write(AppSettings.FileName, "{ not json");
            Assert.Throws<InvalidDataException>(() => RunSettings.Resolve(CliOptions.Parse(System.Array.Empty<string>()), dir.Path));
        }

        [Fact]
        public void CreateTemplate_RoundTrips()
        {
            using var dir = new TempDirectory();
            string path = dir.Write(AppSettings.FileName, AppSettings.CreateTemplate());

            var settings = AppSettings.Load(path);

            Assert.Equal("auto", settings.Language);
            Assert.Equal("README.md", settings.OutputFile);
            Assert.True(settings.Timestamp);
        }

        // ------------------------------------------------------------------
        // Running
        // ------------------------------------------------------------------

        [Fact]
        public void Run_WritesReadme_AndKeepsManualContent()
        {
            using var dir = new TempDirectory();
            dir.Write("LICENSE", "MIT");
            dir.Write("README.md", "# My Tool\n\nWritten by hand.\n");
            dir.Write("src/Api.cs", Source);

            int exitCode = Run(dir, out _, ".", "README.md", "https://github.com/u/r");

            string readme = dir.Read("README.md");
            Assert.Equal(Runner.ExitOk, exitCode);
            Assert.StartsWith("# My Tool\n\nWritten by hand.\n\n<!-- AUTO-GENERATED BELOW", readme);
            Assert.Contains("### `Api` *(class)*", readme);
            Assert.Contains("(https://github.com/u/r/blob/HEAD/src/Api.cs)", readme);
            Assert.Contains("> The API.", readme);
            Assert.Contains("- [ ] Add paging", readme);
        }

        [Fact]
        public void Run_Check_ReportsWhetherTheFileIsUpToDate()
        {
            using var dir = new TempDirectory();
            dir.Write("LICENSE", "MIT");
            dir.Write("Api.cs", Source);

            Assert.Equal(Runner.ExitOutOfDate, Run(dir, out _, "--check"));
            Assert.False(File.Exists(Path.Combine(dir.Path, "README.md")));

            Assert.Equal(Runner.ExitOk, Run(dir, out _));
            Assert.Equal(Runner.ExitOk, Run(dir, out _, "--check"));

            dir.Write("Other.cs", Source.Replace("Api", "Other"));
            Assert.Equal(Runner.ExitOutOfDate, Run(dir, out _, "--check"));
        }

        [Fact]
        public void Run_DryRun_PrintsAndWritesNothing()
        {
            using var dir = new TempDirectory();
            dir.Write("LICENSE", "MIT");
            dir.Write("Api.cs", Source);

            int exitCode = Run(dir, out string stdout, "--dry-run", "--toc");

            Assert.Equal(Runner.ExitOk, exitCode);
            Assert.StartsWith(MarkdownGenerator.StartMarker, stdout);
            Assert.Contains("- [App](#app) (1 type)", stdout);
            Assert.False(File.Exists(Path.Combine(dir.Path, "README.md")));
        }

        [Fact]
        public void Run_Json_WritesValidJsonWithTodoDetails()
        {
            using var dir = new TempDirectory();
            dir.Write("LICENSE", "MIT");
            dir.Write("Api.cs", Source);

            Assert.Equal(Runner.ExitOk, Run(dir, out _, "--json"));

            using var json = JsonDocument.Parse(dir.Read("readmesync.json"));
            var entry = json.RootElement.EnumerateArray().Single();
            Assert.Equal("Api", entry.GetProperty("class").GetString());
            Assert.Equal("csharp", entry.GetProperty("language").GetString());
            Assert.Equal("Api.cs", entry.GetProperty("path").GetString());
            var todo = entry.GetProperty("todoItems").EnumerateArray().Single();
            Assert.Equal("TODO", todo.GetProperty("tag").GetString());
            Assert.Equal(7, todo.GetProperty("line").GetInt32());
        }

        [Fact]
        public void Run_Json_RefusesToOverwriteMarkdown()
        {
            using var dir = new TempDirectory();
            dir.Write("LICENSE", "MIT");
            dir.Write("README.md", "# Precious\n");
            dir.Write("Api.cs", Source);

            Assert.Throws<CliException>(() => Run(dir, out _, "--json", ".", "README.md"));
            Assert.Equal("# Precious\n", dir.Read("README.md"));
        }

        [Fact]
        public void Run_NoSourceFiles_LeavesTheOutputAlone()
        {
            using var dir = new TempDirectory();
            dir.Write("LICENSE", "MIT");
            dir.Write("README.md", "# Precious\n");

            Assert.Equal(Runner.ExitOk, Run(dir, out _, "--lang", "go"));
            Assert.Equal("# Precious\n", dir.Read("README.md"));
        }
    }
}
