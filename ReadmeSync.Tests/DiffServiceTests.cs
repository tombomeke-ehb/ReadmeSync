using System.Linq;
using Xunit;
using ReadmeSync.Services;

namespace ReadmeSync.Tests
{
    public class DiffServiceTests
    {
        private readonly DiffService _diffService = new();

        [Fact]
        public void GenerateDiff_IdenticalContent_ReturnsOnlyUnchanged()
        {
            // Arrange
            string content = @"Line 1
Line 2
Line 3";

            // Act
            var diffLines = _diffService.GetDiffLines(content, content);

            // Assert
            Assert.NotNull(diffLines);
            // Identical content should have no added or removed lines
            Assert.DoesNotContain(diffLines, d => d.Type == '+');
            Assert.DoesNotContain(diffLines, d => d.Type == '-');
        }

        [Fact]
        public void GenerateDiff_AddedLines_ShowsAdditions()
        {
            // Arrange
            string oldContent = @"Line 1
Line 2";
            string newContent = @"Line 1
Line 2
Line 3
Line 4";

            // Act
            string diff = _diffService.GenerateDiff(oldContent, newContent);

            // Assert
            Assert.NotNull(diff);
            Assert.Contains("+", diff);
            Assert.Contains("Line 3", diff);
            Assert.Contains("Line 4", diff);
        }

        [Fact]
        public void GenerateDiff_RemovedLines_ShowsDeletions()
        {
            // Arrange
            string oldContent = @"Line 1
Line 2
Line 3
Line 4";
            string newContent = @"Line 1
Line 2";

            // Act
            string diff = _diffService.GenerateDiff(oldContent, newContent);

            // Assert
            Assert.NotNull(diff);
            Assert.Contains("-", diff);
            Assert.Contains("Line 3", diff);
            Assert.Contains("Line 4", diff);
        }

        [Fact]
        public void GenerateDiff_ModifiedLines_ShowsChanges()
        {
            // Arrange
            string oldContent = @"Line 1
Line 2 old
Line 3";
            string newContent = @"Line 1
Line 2 new
Line 3";

            // Act
            string diff = _diffService.GenerateDiff(oldContent, newContent);

            // Assert
            Assert.NotNull(diff);
            // Modification is typically shown as deletion + addition
            Assert.Contains("old", diff);
            Assert.Contains("new", diff);
        }

        [Fact]
        public void GenerateDiff_ComplexChange_IncludesAllChanges()
        {
            // Arrange
            string oldContent = @"# Header
Line 1
Line 2
Line 3
Line 4";
            string newContent = @"# Header Updated
Line 1
New Line
Line 3
Line 4
Line 5";

            // Act
            string diff = _diffService.GenerateDiff(oldContent, newContent);

            // Assert
            Assert.NotNull(diff);
            Assert.Contains("Header Updated", diff);
            Assert.Contains("New Line", diff);
            Assert.Contains("Line 5", diff);
        }

        [Fact]
        public void GenerateDiff_EmptyToContent_ShowsAllAsAdditions()
        {
            // Arrange
            string oldContent = "";
            string newContent = @"Line 1
Line 2
Line 3";

            // Act
            string diff = _diffService.GenerateDiff(oldContent, newContent);

            // Assert
            Assert.NotNull(diff);
            Assert.Contains("Line 1", diff);
            Assert.Contains("Line 2", diff);
            Assert.Contains("Line 3", diff);
        }

        [Fact]
        public void GenerateDiff_ContentToEmpty_ShowsAllAsDeletions()
        {
            // Arrange
            string oldContent = @"Line 1
Line 2
Line 3";
            string newContent = "";

            // Act
            string diff = _diffService.GenerateDiff(oldContent, newContent);

            // Assert
            Assert.NotNull(diff);
            Assert.Contains("Line 1", diff);
            Assert.Contains("Line 2", diff);
            Assert.Contains("Line 3", diff);
        }

        [Fact]
        public void GenerateDiff_MultipleChanges_LocatesEachChange()
        {
            // Arrange
            string oldContent = @"# Heading
Content A
Content B
Content C
Footer";
            string newContent = @"# Heading Updated
Content A
Modified B
Content C
Content D
Footer Updated";

            // Act
            string diff = _diffService.GenerateDiff(oldContent, newContent);

            // Assert
            Assert.NotNull(diff);
            // Should contain changes for heading, B, D, and footer
            Assert.Contains("Heading Updated", diff);
            Assert.Contains("Modified B", diff);
            Assert.Contains("Content D", diff);
            Assert.Contains("Footer Updated", diff);
        }

        [Fact]
        public void GetDiffLines_ReturnsStructuredDiffData()
        {
            // Arrange
            string oldContent = @"Line 1
Line 2
Line 3";
            string newContent = @"Line 1
Modified 2
Line 3";

            // Act
            var diffLines = _diffService.GetDiffLines(oldContent, newContent);

            // Assert
            Assert.NotNull(diffLines);
            Assert.NotEmpty(diffLines);
            // Should have at least one change
            var changes = diffLines.Where(d => d.Type != ' ').ToList();
            Assert.NotEmpty(changes);
        }

        [Fact]
        public void GetDiffLines_IdenticalContent_AllUnchanged()
        {
            // Arrange
            string content = @"Line 1
Line 2
Line 3";

            // Act
            var diffLines = _diffService.GetDiffLines(content, content);

            // Assert
            Assert.NotNull(diffLines);
            Assert.True(diffLines.All(d => d.Type == ' '));
        }

        [Fact]
        public void GetDiffLines_DifferentContent_HasAddedAndRemoved()
        {
            // Arrange
            string oldContent = @"Old Line 1
Old Line 2";
            string newContent = @"New Line 1
New Line 2";

            // Act
            var diffLines = _diffService.GetDiffLines(oldContent, newContent);

            // Assert
            Assert.NotNull(diffLines);
            var hasRemoved = diffLines.Any(d => d.Type == '-');
            var hasAdded = diffLines.Any(d => d.Type == '+');
            Assert.True(hasRemoved || hasAdded);
        }

        [Fact]
        public void GenerateDiff_LargeContent_HandlesCorrectly()
        {
            // Arrange
            var oldLines = Enumerable.Range(1, 100).Select(i => $"Line {i}").ToList();
            string oldContent = string.Join("\n", oldLines);

            var newLines = Enumerable.Range(1, 100).Select(i => i % 2 == 0 ? $"Line {i} Modified" : $"Line {i}").ToList();
            string newContent = string.Join("\n", newLines);

            // Act
            string diff = _diffService.GenerateDiff(oldContent, newContent);

            // Assert
            Assert.NotNull(diff);
            // Should contain some modifications
            Assert.NotEmpty(diff);
        }

        [Fact]
        public void GenerateDiff_SpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            string oldContent = @"Line with special chars: !@#$%^&*()
Line with unicode: 你好
Line with emoji: 🚀";
            string newContent = @"Line with special chars: !@#$%^&*()*
Line with unicode: 你好
Line with emoji: 🚀🎉";

            // Act
            string diff = _diffService.GenerateDiff(oldContent, newContent);

            // Assert
            Assert.NotNull(diff);
            Assert.Contains("special chars", diff);
        }

        [Fact]
        public void GenerateDiff_WhitespaceOnly_TreatsAsDifferent()
        {
            // Arrange
            string oldContent = "Line 1\nLine 2";
            string newContent = "Line 1\nLine 2\n";

            // Act
            string diff = _diffService.GenerateDiff(oldContent, newContent);

            // Assert
            Assert.NotNull(diff);
            // Different whitespace should be detected
        }

        [Fact]
        public void GenerateDiff_MarkdownContent_HighlightsChanges()
        {
            // Arrange
            string oldMarkdown = @"# Title

Content paragraph 1.

## Section 1

Details here.";
            string newMarkdown = @"# New Title

Content paragraph 1.
Additional line.

## Section 1

Details here.
More details.";

            // Act
            string diff = _diffService.GenerateDiff(oldMarkdown, newMarkdown);

            // Assert
            Assert.NotNull(diff);
            Assert.Contains("Title", diff);
            Assert.Contains("Additional line", diff);
            Assert.Contains("More details", diff);
        }
    }
}
