using ReadmeSync.Models;
using Xunit;

namespace ReadmeSync.Tests
{
    public class LanguagePatternsTests
    {
        [Theory]
        [InlineData("csharp", ".cs")]
        [InlineData("java", ".java")]
        [InlineData("python", ".py")]
        [InlineData("typescript", ".ts")]
        [InlineData("javascript", ".js")]
        [InlineData("php", ".php")]
        [InlineData("go", ".go")]
        [InlineData("rust", ".rs")]
        [InlineData("ruby", ".rb")]
        [InlineData("kotlin", ".kt")]
        [InlineData("swift", ".swift")]
        [InlineData("cpp", ".cpp")]
        public void LanguagePatterns_For_ReturnsCorrectExtension(string language, string expectedExtension)
        {
            var patterns = LanguagePatterns.For(language);

            Assert.Equal(expectedExtension, patterns.Extension);
        }

        [Theory]
        [InlineData("csharp")]
        [InlineData("java")]
        [InlineData("python")]
        [InlineData("typescript")]
        [InlineData("javascript")]
        [InlineData("php")]
        [InlineData("go")]
        [InlineData("rust")]
        [InlineData("ruby")]
        [InlineData("kotlin")]
        [InlineData("swift")]
        [InlineData("cpp")]
        public void LanguagePatterns_For_ReturnsPatternsWithAllRequiredFields(string language)
        {
            var patterns = LanguagePatterns.For(language);

            Assert.NotEmpty(patterns.Namespace);
            Assert.NotEmpty(patterns.Class);
            Assert.NotEmpty(patterns.Method);
            Assert.NotEmpty(patterns.Summary);
        }

        [Fact]
        public void TypeScript_Method_PatternFixed()
        {
            var patterns = LanguagePatterns.For("typescript");

            var methodPattern = patterns.Method;
            Assert.DoesNotContain("A-Za6789_", methodPattern);
            Assert.Contains("A-Za-z0-9_", methodPattern);
        }
    }
}
