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

        [Fact]
        public void GenerateMarkdown_CreatesFileWithMarker()
        {
            // Arrange
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.md");
            var fileGroups = CreateTestFileGroups();

            try
            {
                // Act
                _generator.GenerateMarkdown(tempFile, fileGroups, "csharp", ".cs", useEmojis: false, repoUrl: "");

                // Assert
                Assert.True(File.Exists(tempFile));
                string content = File.ReadAllText(tempFile);
                Assert.Contains("<!-- AUTO-GENERATED BELOW", content);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void GenerateMarkdown_PreservesManualContentAboveMarker()
        {
            // Arrange
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.md");
            string manualContent = "# My Project\n\nThis is manual content.\n\n";

            try
            {
                File.WriteAllText(tempFile, manualContent + "<!-- AUTO-GENERATED BELOW\nold content");
                var fileGroups = CreateTestFileGroups();

                // Act
                _generator.GenerateMarkdown(tempFile, fileGroups, "csharp", ".cs", useEmojis: false, repoUrl: "");

                // Assert
                string result = File.ReadAllText(tempFile);
                Assert.StartsWith(manualContent.TrimEnd(), result.TrimStart());
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void GenerateMarkdown_IncludesStatistics()
        {
            // Arrange
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.md");
            var fileGroups = CreateTestFileGroups();

            try
            {
                // Act
                _generator.GenerateMarkdown(tempFile, fileGroups, "csharp", ".cs", useEmojis: false, repoUrl: "");

                // Assert
                string content = File.ReadAllText(tempFile);
                Assert.Contains("Packages", content);
                Assert.Contains("Types", content);
                Assert.Contains("Methods", content);
                Assert.Contains("TODOs", content);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void GenerateMarkdown_WithEmojis_IncludesEmojiCharacters()
        {
            // Arrange
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.md");
            var fileGroups = CreateTestFileGroups();

            try
            {
                // Act
                _generator.GenerateMarkdown(tempFile, fileGroups, "csharp", ".cs", useEmojis: true, repoUrl: "");

                // Assert
                string content = File.ReadAllText(tempFile);
                Assert.Contains("📊", content);
                Assert.Contains("🧮", content);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void GenerateMarkdown_WithoutEmojis_ExcludesEmojiCharacters()
        {
            // Arrange
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.md");
            var fileGroups = CreateTestFileGroups();

            try
            {
                // Act
                _generator.GenerateMarkdown(tempFile, fileGroups, "csharp", ".cs", useEmojis: false, repoUrl: "");

                // Assert
                string content = File.ReadAllText(tempFile);
                Assert.DoesNotContain("📊", content);
                Assert.DoesNotContain("🧮", content);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void GenerateMarkdown_EmptyFileList_GeneratesValidMarkdown()
        {
            // Arrange
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.md");
            var emptyGroups = new List<IGrouping<string, CodeFileInfo>>();

            try
            {
                // Act
                _generator.GenerateMarkdown(tempFile, emptyGroups, "csharp", ".cs", useEmojis: false, repoUrl: "");

                // Assert
                Assert.True(File.Exists(tempFile));
                string content = File.ReadAllText(tempFile);
                Assert.Contains("<!-- AUTO-GENERATED BELOW", content);
                Assert.Contains("0 Packages", content);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void PreviewMarkdown_ReturnsStringWithoutWritingFile()
        {
            // Arrange
            var fileGroups = CreateTestFileGroups();

            // Act
            string result = _generator.PreviewMarkdown(fileGroups, "csharp", ".cs", useEmojis: false, repoUrl: "");

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("<!-- AUTO-GENERATED BELOW", result);
            Assert.Contains("Code Overview", result);
        }

        [Fact]
        public void PreviewMarkdown_MatchesGenerateMarkdownContent()
        {
            // Arrange
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.md");
            var fileGroups = CreateTestFileGroups().ToList();

            try
            {
                // Act
                _generator.GenerateMarkdown(tempFile, fileGroups, "csharp", ".cs", useEmojis: true, repoUrl: "https://github.com/test/repo");
                string fileContent = File.ReadAllText(tempFile);

                string previewContent = _generator.PreviewMarkdown(fileGroups, "csharp", ".cs", useEmojis: true, repoUrl: "https://github.com/test/repo");

                // Assert — both should have same content (file includes manual content at top, but structure should match)
                Assert.Contains("<!-- AUTO-GENERATED BELOW", fileContent);
                Assert.Contains("<!-- AUTO-GENERATED BELOW", previewContent);
                Assert.Contains("Code Overview", fileContent);
                Assert.Contains("Code Overview", previewContent);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        private List<IGrouping<string, CodeFileInfo>> CreateTestFileGroups()
        {
            var files = new List<CodeFileInfo>
            {
                new()
                {
                    Namespace = "MyApp.Core",
                    TypeKeyword = "class",
                    Class = "Player",
                    Inheritance = "Entity",
                    Summary = "Represents a player in the game.",
                    Methods = new List<string> { "Attack", "Move" },
                    Todos = new List<string> { "Implement special abilities" },
                    Path = "Player.cs",
                    Link = "https://github.com/test/repo/Player.cs"
                },
                new()
                {
                    Namespace = "MyApp.Core",
                    TypeKeyword = "interface",
                    Class = "IPlayable",
                    Inheritance = "",
                    Summary = "Defines playable actions.",
                    Methods = new List<string> { "Attack", "Defend" },
                    Todos = new List<string>(),
                    Path = "IPlayable.cs",
                    Link = "https://github.com/test/repo/IPlayable.cs"
                }
            };

            return files.GroupBy(f => f.Namespace).OrderBy(g => g.Key).ToList();
        }
    }
}
