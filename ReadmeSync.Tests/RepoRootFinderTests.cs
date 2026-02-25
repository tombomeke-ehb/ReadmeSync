using System.IO;
using Xunit;
using ReadmeSync.Services;

namespace ReadmeSync.Tests
{
    public class RepoRootFinderTests
    {
        private readonly RepoRootFinder _finder = new();

        [Fact]
        public void FindRepoRoot_WithGitDirectory_ReturnsCorrectPath()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), "test-repo-git");
            string subDir = Path.Combine(tempDir, "src", "app");
            Directory.CreateDirectory(subDir);
            Directory.CreateDirectory(Path.Combine(tempDir, ".git"));

            try
            {
                // Act
                string? result = _finder.FindRepoRoot(subDir, out string? reason);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(Path.GetFullPath(tempDir), result);
                Assert.Contains(".git", reason);
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void FindRepoRoot_WithSolutionFile_ReturnsCorrectPath()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), "test-repo-sln");
            string subDir = Path.Combine(tempDir, "src");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(tempDir, "MySolution.sln"), "");

            try
            {
                // Act
                string? result = _finder.FindRepoRoot(subDir, out string? reason);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(Path.GetFullPath(tempDir), result);
                Assert.Contains("solution", reason);
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void FindRepoRoot_WithReadme_ReturnsCorrectPath()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), "test-repo-readme");
            string subDir = Path.Combine(tempDir, "docs");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(tempDir, "README.md"), "# Test");

            try
            {
                // Act
                string? result = _finder.FindRepoRoot(subDir, out string? reason);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(Path.GetFullPath(tempDir), result);
                Assert.Contains("marker file", reason);
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void FindRepoRoot_WithPackageJson_ReturnsCorrectPath()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), "test-repo-node");
            string subDir = Path.Combine(tempDir, "src");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(tempDir, "package.json"), "{}");

            try
            {
                // Act
                string? result = _finder.FindRepoRoot(subDir, out string? reason);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(Path.GetFullPath(tempDir), result);
                Assert.Contains("project file", reason);
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void FindRepoRoot_NoMarkers_ReturnsNull()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), "test-no-markers");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Act
                string? result = _finder.FindRepoRoot(tempDir, out string? reason);

                // Assert
                Assert.Null(result);
                Assert.Null(reason);
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }
    }
}
