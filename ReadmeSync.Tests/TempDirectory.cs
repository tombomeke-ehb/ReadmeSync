using System;
using System.IO;

namespace ReadmeSync.Tests
{
    /// <summary>
    /// A uniquely named scratch directory that is deleted when the test is done.
    /// </summary>
    internal sealed class TempDirectory : IDisposable
    {
        public string Path { get; }

        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "readmesync-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        /// <summary>Writes a file below the directory, creating folders as needed, and returns its full path.</summary>
        public string Write(string relativePath, string content)
        {
            string full = System.IO.Path.Combine(Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content);
            return full;
        }

        public string Read(string relativePath) => File.ReadAllText(System.IO.Path.Combine(Path, relativePath));

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, true);
            }
            catch (IOException)
            {
                // Best effort: a leftover temp folder must not fail a test.
            }
        }
    }
}
