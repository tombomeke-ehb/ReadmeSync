using System.Collections.Generic;

namespace ReadmeSync.Models
{
    /// <summary>
    /// Describes one type (class, interface, record, ...) found in a source file.
    /// </summary>
    public class CodeFileInfo
    {
        public string Namespace { get; set; } = "";
        public string TypeKeyword { get; set; } = "";
        public string Class { get; set; } = "";
        public string Inheritance { get; set; } = "";
        public string Summary { get; set; } = "";
        public List<string> Methods { get; set; } = new();

        /// <summary>TODO texts. FIXME and HACK notes are prefixed with their tag.</summary>
        public List<string> Todos { get; set; } = new();

        /// <summary>The same notes as <see cref="Todos"/>, with tag and line number.</summary>
        public List<TodoItem> TodoItems { get; set; } = new();

        public string Path { get; set; } = "";
        public string? Link { get; set; }

        /// <summary>Language key the file was parsed as (for example "csharp").</summary>
        public string Language { get; set; } = "";

        /// <summary>1-based line of the type declaration.</summary>
        public int Line { get; set; }
    }
}
