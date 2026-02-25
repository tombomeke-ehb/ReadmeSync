using System;
using System.IO;
using System.Linq;

namespace ReadmeSync.Services
{
    /// <summary>
    /// Service responsible for finding the repository root directory.
    /// </summary>
    public class RepoRootFinder
    {
        /// <summary>
        /// Recursively searches upward from the given path to locate the
        /// most likely repository root (preferring .git, solution, or README).
        /// </summary>
        public string? FindRepoRoot(string startPath, out string? reason)
        {
            reason = null;
            var dir = new DirectoryInfo(Path.GetFullPath(startPath));

            while (dir != null)
            {
                // Priority 1: .git directory
                if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                {
                    reason = $".git at {dir.FullName}";
                    return dir.FullName;
                }

                // Priority 2: Solution file
                if (Directory.EnumerateFiles(dir.FullName, "*.sln").Any())
                {
                    reason = $"solution (*.sln) at {dir.FullName}";
                    return dir.FullName;
                }

                // Priority 3: Common repository markers
                if (File.Exists(Path.Combine(dir.FullName, "README.md")) ||
                    File.Exists(Path.Combine(dir.FullName, "LICENSE")) ||
                    Directory.Exists(Path.Combine(dir.FullName, ".github")))
                {
                    reason = $"marker file at {dir.FullName}";
                    return dir.FullName;
                }

                // Priority 4: Python/Node specific markers
                if (File.Exists(Path.Combine(dir.FullName, "setup.py")) ||
                    File.Exists(Path.Combine(dir.FullName, "pyproject.toml")) ||
                    File.Exists(Path.Combine(dir.FullName, "package.json")))
                {
                    reason = $"project file at {dir.FullName}";
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            return null;
        }
    }
}
