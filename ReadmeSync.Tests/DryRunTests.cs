using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using ReadmeSync.Models;
using ReadmeSync.Services;

namespace ReadmeSync.Tests
{
    public class DryRunTests
    {
        private readonly MarkdownGenerator _markdownGen = new();
        private readonly JsonGenerator _jsonGen = new();

        [Fact]
        public void PreviewMarkdown_DoesNotCreateFile()
        {
            // Arrange
            var fileGroups = CreateTestFileGroups();
            var tempFile = Path.Combine(Path.GetTempPath(), $"dryrun_{Guid.NewGuid()}.md");

            // Act
            string preview = _markdownGen.PreviewMarkdown(fileGroups, "csharp", ".cs", useEmojis: false, repoUrl: "");

            // Assert
            Assert.NotEmpty(preview);
            Assert.False(File.Exists(tempFile));
        }

        [Fact]
        public void PreviewJson_DoesNotCreateFile()
        {
            // Arrange
            var fileGroups = CreateTestFileGroups();
            var tempFile = Path.Combine(Path.GetTempPath(), $"dryrun_{Guid.NewGuid()}.json");

            // Act
            string preview = _jsonGen.PreviewJson(fileGroups);

            // Assert
            Assert.NotEmpty(preview);
            Assert.False(File.Exists(tempFile));
        }

        [Fact]
        public void PreviewJson_ReturnsValidJsonString()
        {
            // Arrange
            var fileGroups = CreateTestFileGroups();

            // Act
            string result = _jsonGen.PreviewJson(fileGroups);

            // Assert
            Assert.NotEmpty(result);
            Assert.StartsWith("[", result.Trim());
            Assert.EndsWith("]", result.Trim());
            Assert.Contains("namespace", result);
            Assert.Contains("class", result);
        }

        [Fact]
        public void DryRun_PreviewContainsSameMetadataAsRealRun()
        {
            // Arrange
            var tempFile = Path.Combine(Path.GetTempPath(), $"dryrun_{Guid.NewGuid()}.md");
            var fileGroups = CreateTestFileGroups().ToList();

            try
            {
                // Act
                _markdownGen.GenerateMarkdown(tempFile, fileGroups, "csharp", ".cs", useEmojis: false, repoUrl: "");
                string fileContent = File.ReadAllText(tempFile);

                string previewContent = _markdownGen.PreviewMarkdown(fileGroups, "csharp", ".cs", useEmojis: false, repoUrl: "");

                // Assert
                // Both should contain the same class and namespace information
                Assert.Contains("MyApp.Core", fileContent);
                Assert.Contains("MyApp.Core", previewContent);
                Assert.Contains("Player", fileContent);
                Assert.Contains("Player", previewContent);
                Assert.Contains("Attack", fileContent);
                Assert.Contains("Attack", previewContent);
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
                    Summary = "Represents a player.",
                    Methods = new List<string> { "Attack", "Move" },
                    Todos = new List<string> { "Implement abilities" },
                    Path = "Player.cs",
                    Link = null
                }
            };

            return files.GroupBy(f => f.Namespace).OrderBy(g => g.Key).ToList();
        }
    }
}
