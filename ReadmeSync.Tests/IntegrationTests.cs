using System.IO;
using System.Linq;
using Xunit;
using ReadmeSync.Models;
using ReadmeSync.Services;

namespace ReadmeSync.Tests
{
    public class IntegrationTests
    {
        [Fact]
        public void EndToEnd_ScanCSharpProject_GeneratesValidMarkdown()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), "test-csharp-project");
            Directory.CreateDirectory(tempDir);

            string testFile = Path.Combine(tempDir, "TestClass.cs");
            File.WriteAllText(testFile, @"
namespace TestApp
{
    /// <summary>
    /// Test class for integration test.
    /// </summary>
    public class TestClass
    {
        public void TestMethod() { }
        // TODO: Add more methods
    }
}");

            string outputFile = Path.Combine(tempDir, "README.md");

            try
            {
                // Act
                var analyzer = new CodeAnalyzer();
                var patterns = LanguagePatterns.For("csharp");
                var codeText = File.ReadAllText(testFile);
                var codeInfo = analyzer.AnalyzeCode(codeText, patterns, "csharp");

                Assert.NotNull(codeInfo);

                var markdownGen = new MarkdownGenerator();
                var fileGroups = new[] { codeInfo }.GroupBy(f => f.Namespace);

                markdownGen.GenerateMarkdown(outputFile, fileGroups, "csharp", ".cs", false, "");

                // Assert
                Assert.True(File.Exists(outputFile));
                string content = File.ReadAllText(outputFile);
                Assert.Contains("<!-- AUTO-GENERATED BELOW", content);
                Assert.Contains("TestApp", content);
                Assert.Contains("TestClass", content);
                Assert.Contains("TestMethod", content);
                Assert.Contains("TODO", content);
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void EndToEnd_PreservesManualContent()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), "test-preserve-content");
            Directory.CreateDirectory(tempDir);

            string outputFile = Path.Combine(tempDir, "README.md");
            string manualContent = "# My Project\n\nThis is my manual content.\n\n";
            File.WriteAllText(outputFile, manualContent + "<!-- AUTO-GENERATED BELOW -->\nOld generated content");

            try
            {
                // Act
                var markdownGen = new MarkdownGenerator();
                markdownGen.GenerateMarkdown(outputFile, Enumerable.Empty<IGrouping<string, CodeFileInfo>>(), "csharp", ".cs", false, "");

                // Assert
                string content = File.ReadAllText(outputFile);
                Assert.StartsWith(manualContent, content);
                Assert.Contains("<!-- AUTO-GENERATED BELOW", content);
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void EndToEnd_MultipleLanguages_ParseCorrectly()
        {
            // Arrange
            var analyzer = new CodeAnalyzer();

            string csharpCode = "namespace Test\n{\n public class MyClass { } \n}";
            string javaCode = "package test;\npublic class MyClass { }";
            string pythonCode = "# Package: test\n\nclass MyClass:\n    pass";

            // Act
            var csharpResult = analyzer.AnalyzeCode(csharpCode, LanguagePatterns.For("csharp"), "csharp");
            var javaResult = analyzer.AnalyzeCode(javaCode, LanguagePatterns.For("java"), "java");
            var pythonResult = analyzer.AnalyzeCode(pythonCode, LanguagePatterns.For("python"), "python");

            // Assert
            Assert.NotNull(csharpResult);
            Assert.Equal("Test", csharpResult.Namespace);
            Assert.Equal("MyClass", csharpResult.Class);

            Assert.NotNull(javaResult);
            Assert.Equal("test", javaResult.Namespace);
            Assert.Equal("MyClass", javaResult.Class);

            Assert.NotNull(pythonResult);
            Assert.Equal("test", pythonResult.Namespace);
            Assert.Equal("MyClass", pythonResult.Class);
        }
    }
}
