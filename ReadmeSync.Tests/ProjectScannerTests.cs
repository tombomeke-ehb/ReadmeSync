using System;
using System.IO;
using System.Linq;
using Xunit;
using ReadmeSync.Services;

namespace ReadmeSync.Tests
{
    public class ProjectScannerTests
    {
        private const string CSharpClass = "namespace App\n{\n    public class {0}\n    {\n        public void Run() { }\n    }\n}\n";

        private static string Class(string name) => CSharpClass.Replace("{0}", name);

        [Fact]
        public void Scan_SkipsDefaultAndCustomExcludes()
        {
            using var dir = new TempDirectory();
            dir.Write("src/Keep.cs", Class("Keep"));
            dir.Write("obj/Generated.cs", Class("Generated"));
            dir.Write("src/bin/Debug/Built.cs", Class("Built"));
            dir.Write("legacy/Old.cs", Class("Old"));
            dir.Write("src/Thing.g.cs", Class("Thing"));
            dir.Write("src/deep/skip/Deep.cs", Class("Deep"));

            var excludes = ProjectScanner.DefaultExcludes.Concat(new[] { "legacy", "*.g.cs", "src/deep/skip" }).ToList();
            var result = new ProjectScanner().Scan(new ScanOptions { ScanRoot = dir.Path, Excludes = excludes });

            Assert.Equal(new[] { "Keep" }, result.Types.Select(t => t.Class));
            Assert.Equal(1, result.FilesScanned);
        }

        [Fact]
        public void Scan_Auto_DetectsEveryLanguagePresent()
        {
            using var dir = new TempDirectory();
            dir.Write("Api.cs", Class("Api"));
            dir.Write("web/client.ts", "export class Client {\n    load(): void { }\n}\n");
            dir.Write("tools/job.py", "class Job:\n    def run(self):\n        pass\n");
            dir.Write("notes.txt", "class NotCode { }");

            var result = new ProjectScanner().Scan(new ScanOptions { ScanRoot = dir.Path, Languages = new[] { "auto" } });

            Assert.Equal(new[] { "csharp", "python", "typescript" }, result.Languages);
            Assert.Equal(new[] { "Api", "Job", "Client" }, result.Types.Select(t => t.Class));
            // Languages without namespaces are grouped by folder.
            Assert.Equal(new[] { "App", "tools", "web" }, result.GroupByNamespace().Select(g => g.Key));
        }

        [Fact]
        public void Scan_OnlyRequestedLanguages()
        {
            using var dir = new TempDirectory();
            dir.Write("Api.cs", Class("Api"));
            dir.Write("job.py", "class Job:\n    pass\n");

            var result = new ProjectScanner().Scan(new ScanOptions { ScanRoot = dir.Path, Languages = new[] { "py" } });

            var type = Assert.Single(result.Types);
            Assert.Equal("Job", type.Class);
            Assert.Equal("default", type.Namespace);
            Assert.Equal("python", type.Language);
        }

        [Fact]
        public void Scan_SetsRelativePathsAndLinks()
        {
            using var dir = new TempDirectory();
            dir.Write("src/My App/Api.cs", Class("Api"));

            var result = new ProjectScanner().Scan(new ScanOptions
            {
                ScanRoot = Path.Combine(dir.Path, "src"),
                RepoRoot = dir.Path,
                RepoUrl = "https://github.com/user/repo"
            });

            var type = Assert.Single(result.Types);
            Assert.Equal("src/My App/Api.cs", type.Path);
            Assert.Equal("https://github.com/user/repo/blob/HEAD/src/My%20App/Api.cs", type.Link);
        }

        [Fact]
        public void Scan_ReportsFilesWithoutTypes()
        {
            using var dir = new TempDirectory();
            dir.Write("Api.cs", Class("Api"));
            dir.Write("AssemblyInfo.cs", "using System;\n[assembly: CLSCompliant(true)]\n");

            var result = new ProjectScanner().Scan(new ScanOptions { ScanRoot = dir.Path });

            Assert.Equal(2, result.FilesScanned);
            Assert.Equal(new[] { "AssemblyInfo.cs" }, result.FilesWithoutTypes);
        }

        [Fact]
        public void Scan_UnknownLanguage_Throws()
        {
            using var dir = new TempDirectory();

            var ex = Assert.Throws<ArgumentException>(() =>
                new ProjectScanner().Scan(new ScanOptions { ScanRoot = dir.Path, Languages = new[] { "cobol" } }));
            Assert.Contains("cobol", ex.Message);
        }

        [Fact]
        public void Scan_MissingDirectory_Throws()
        {
            Assert.Throws<DirectoryNotFoundException>(() =>
                new ProjectScanner().Scan(new ScanOptions { ScanRoot = Path.Combine(Path.GetTempPath(), "readmesync-does-not-exist") }));
        }

        [Theory]
        [InlineData("https://github.com/user/repo", null, "https://github.com/user/repo/blob/HEAD/src/A.cs")]
        [InlineData("https://github.com/user/repo.git", "develop", "https://github.com/user/repo/blob/develop/src/A.cs")]
        [InlineData("https://gitlab.com/group/repo/", null, "https://gitlab.com/group/repo/-/blob/HEAD/src/A.cs")]
        [InlineData("https://bitbucket.org/team/repo", "main", "https://bitbucket.org/team/repo/src/main/src/A.cs")]
        [InlineData("https://github.com/user/repo/blob/main", null, "https://github.com/user/repo/blob/main/src/A.cs")]
        [InlineData("https://example.com/code", null, "https://example.com/code/src/A.cs")]
        [InlineData("not a url", null, null)]
        [InlineData("", null, null)]
        public void BuildFileUrl_KnowsTheCommonHosts(string repoUrl, string? branch, string? expected)
        {
            Assert.Equal(expected, RepoLinkBuilder.BuildFileUrl(repoUrl, "src\\A.cs", branch));
        }
    }
}
