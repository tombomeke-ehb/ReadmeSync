using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using ReadmeSync.Models;
using ReadmeSync.Services;

namespace ReadmeSync.Tests
{
    public class MarkdownGeneratorTests
    {
        private readonly MarkdownGenerator _generator = new();

        private static List<IGrouping<string, CodeFileInfo>> Groups(params CodeFileInfo[] types) =>
            types.GroupBy(t => t.Namespace).ToList();

        private static CodeFileInfo Type(string ns, string name, params string[] methods) => new()
        {
            Namespace = ns,
            TypeKeyword = "class",
            Class = name,
            Methods = methods.ToList(),
            Path = $"src/{name}.cs"
        };

        private static MarkdownOptions Options(int minute = 0) => new()
        {
            LanguageLabel = "C#",
            Timestamp = new DateTime(2026, 1, 1, 12, minute, 0)
        };

        [Fact]
        public void BuildDocument_FileWithoutMarker_KeepsAllExistingContent()
        {
            string existing = "# My Project\n\nHand-written introduction.\n\n## Install\n\nSteps.\n";

            string document = _generator.BuildDocument(existing, Groups(Type("App", "Api", "Run")), Options());

            Assert.StartsWith("# My Project\n\nHand-written introduction.\n\n## Install\n\nSteps.\n\n" + MarkdownGenerator.StartMarker, document);
            Assert.Contains("### `Api` *(class)*", document);
        }

        [Fact]
        public void BuildDocument_ContentAfterEndMarker_IsPreserved()
        {
            string first = _generator.BuildDocument("# Title\n", Groups(Type("App", "Old")), Options());
            string edited = first + "\n## License\n\nMIT\n";

            string second = _generator.BuildDocument(edited, Groups(Type("App", "New")), Options());

            Assert.StartsWith("# Title\n\n" + MarkdownGenerator.StartMarker, second);
            Assert.DoesNotContain("`Old`", second);
            Assert.Contains("`New`", second);
            Assert.EndsWith(MarkdownGenerator.EndMarker + "\n\n## License\n\nMIT\n", second);
            // Running again changes nothing.
            Assert.Equal(second, _generator.BuildDocument(second, Groups(Type("App", "New")), Options()));
        }

        [Fact]
        public void BuildDocument_LegacyFileWithoutEndMarker_ReplacesEverythingBelowTheMarker()
        {
            string existing = "# Title\n\n<!-- AUTO-GENERATED BELOW – DO NOT EDIT -->\n\nold stuff\n";

            string document = _generator.BuildDocument(existing, Groups(Type("App", "Api")), Options());

            Assert.DoesNotContain("old stuff", document);
            Assert.Single(document.Split(new[] { "AUTO-GENERATED BELOW" }, StringSplitOptions.None).Skip(1));
        }

        [Fact]
        public void BuildDocument_KeepsWindowsLineEndings()
        {
            string document = _generator.BuildDocument("# Title\r\n\r\nText\r\n", Groups(Type("App", "Api")), Options());

            Assert.DoesNotContain("\n", document.Replace("\r\n", ""));
        }

        [Fact]
        public void GenerateMarkdown_UnchangedCode_DoesNotRewriteTheFile()
        {
            using var dir = new TempDirectory();
            string file = Path.Combine(dir.Path, "README.md");
            var groups = Groups(Type("App", "Api", "Run"));

            Assert.True(_generator.GenerateMarkdown(file, groups, Options(minute: 1)));
            string firstRun = File.ReadAllText(file);

            Assert.False(_generator.GenerateMarkdown(file, groups, Options(minute: 30)));
            Assert.Equal(firstRun, File.ReadAllText(file));

            Assert.True(_generator.GenerateMarkdown(file, Groups(Type("App", "Api", "Run", "Stop")), Options(minute: 45)));
            Assert.Contains("12:45", File.ReadAllText(file));
        }

        [Fact]
        public void BuildSection_WritesStatisticsFilesLinksAndTodos()
        {
            var api = Type("App.Web", "Api", "Get", "Post");
            api.Link = "https://github.com/u/r/blob/HEAD/src/Api.cs";
            api.Inheritance = "ControllerBase";
            api.Summary = "The public API.";
            api.Todos.Add("Add paging");
            var model = Type("App.Models", "User");

            string section = _generator.BuildSection(Groups(api, model), Options());

            Assert.Contains("**2 Packages · 2 Types · 2 Methods · 1 TODOs**", section);
            Assert.Contains("_Language: **C#**_", section);
            Assert.Contains("_Last updated: **2026-01-01 12:00**_", section);
            Assert.Contains("File: [`src/Api.cs`](https://github.com/u/r/blob/HEAD/src/Api.cs)", section);
            Assert.Contains("File: `src/User.cs`", section);
            Assert.Contains("**Inherits:** `ControllerBase`", section);
            Assert.Contains("> The public API.", section);
            Assert.Contains("- `Get()`", section);
            Assert.Contains("- [ ] Add paging", section);
            Assert.Contains("_No public methods found._", section);
            Assert.DoesNotContain("Contents", section);
            Assert.EndsWith(MarkdownGenerator.EndMarker + "\n", section);
        }

        [Fact]
        public void BuildSection_OptionalParts_CanBeSwitched()
        {
            var options = Options();
            options.IncludeToc = true;
            options.IncludeTimestamp = false;

            string section = _generator.BuildSection(Groups(Type("App.Web", "Api"), Type("App.Web", "Home"), Type("App.Models", "User")), options);

            Assert.Contains("- [App.Web](#appweb) (2 types)", section);
            Assert.Contains("- [App.Models](#appmodels) (1 type)", section);
            Assert.DoesNotContain("Last updated", section);
        }

        [Fact]
        public void BuildSection_Emojis_AreOptIn()
        {
            var options = Options();
            Assert.DoesNotContain("🧮", _generator.BuildSection(Groups(Type("App", "Api")), options));

            options.UseEmojis = true;
            options.IncludeToc = true;
            string section = _generator.BuildSection(Groups(Type("App", "Api")), options);

            Assert.Contains("# 🧮 Code Overview", section);
            Assert.Contains("## 🧱 App", section);
            Assert.Contains("(#-app)", section);
        }
    }
}
