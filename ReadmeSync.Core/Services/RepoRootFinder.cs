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
        /// Searches upward from the given path to locate the most likely repository root.
        /// A git repository always wins; without one, the nearest directory holding a
        /// solution, README, LICENSE or project file is used.
        /// </summary>
        public string? FindRepoRoot(string startPath, out string? reason)
        {
            reason = null;
            var start = new DirectoryInfo(Path.GetFullPath(startPath));

            // Priority 1: git repository (a directory, or a file in worktrees and submodules)
            for (var dir = start; dir != null; dir = dir.Parent)
            {
                string git = Path.Combine(dir.FullName, ".git");
                if (Directory.Exists(git) || File.Exists(git))
                {
                    reason = $".git at {dir.FullName}";
                    return dir.FullName;
                }
            }

            for (var dir = start; dir != null; dir = dir.Parent)
            {
                // Priority 2: Solution file
                if (HasFile(dir, "*.sln") || HasFile(dir, "*.slnx"))
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

                // Priority 4: Language specific project files
                string[] projectFiles = { "setup.py", "pyproject.toml", "package.json", "composer.json", "go.mod", "pom.xml", "build.gradle", "build.gradle.kts" };
                if (projectFiles.Any(f => File.Exists(Path.Combine(dir.FullName, f))))
                {
                    reason = $"project file at {dir.FullName}";
                    return dir.FullName;
                }
            }

            return null;
        }

        private static bool HasFile(DirectoryInfo dir, string pattern)
        {
            try
            {
                return Directory.EnumerateFiles(dir.FullName, pattern).Any();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
