using System.Collections.Generic;

namespace ReadmeSync.Models
{
    public class CodeFileInfo
    {
        public string Namespace { get; set; } = "";
        public string TypeKeyword { get; set; } = "";
        public string Class { get; set; } = "";
        public string Inheritance { get; set; } = "";
        public string Summary { get; set; } = "";
        public List<string> Methods { get; set; } = new();
        public List<string> Todos { get; set; } = new();
        public string Path { get; set; } = "";
        public string? Link { get; set; }
    }
}
