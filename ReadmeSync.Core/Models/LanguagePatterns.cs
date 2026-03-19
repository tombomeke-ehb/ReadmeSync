namespace ReadmeSync.Models
{
    /// <summary>
    /// Defines the regex profiles for different languages.
    /// Each profile includes:
    ///  - Namespace/Package pattern
    ///  - Class pattern
    ///  - Public method pattern
    ///  - File extension
    ///  - Summary/documentation pattern
    /// </summary>
    public class LanguagePatterns
    {
        public string Namespace { get; }
        public string Class { get; }
        public string Method { get; }
        public string Extension { get; }
        public string Summary { get; }

        private LanguagePatterns(string ns, string cls, string method, string ext, string summary)
        {
            Namespace = ns;
            Class = cls;
            Method = method;
            Extension = ext;
            Summary = summary;
        }

        public static LanguagePatterns For(string lang)
        {
            return lang switch
            {
                "java" => new LanguagePatterns(
                    @"package\s+([A-Za-z0-9_.]+)",
                    @"(class|interface|enum|record)\s+([A-Za-z0-9_]+)\s*([^{]*)",
                    @"public\s+[A-Za-z0-9_<>,\[\]\s]+\s+([A-Za-z0-9_]+)\s*\(",
                    ".java",
                    @"/\*\*(.*?)\*/"
                ),

                "php" => new LanguagePatterns(
                    @"namespace\s+([A-Za-z0-9_\\]+);",
                    @"(class|interface|trait)\s+([A-Za-z0-9_]+)\s*(?:extends\s+([A-Za-z0-9_]+))?\s*(?:implements\s+([A-Za-z0-9_,\s]+))?",
                    @"public\s+function\s+([A-Za-z0-9_]+)\s*\(",
                    ".php",
                    @"/\*\*(.*?)\*/"
                ),

                "python" => new LanguagePatterns(
                    @"#\s*Package:\s*([A-Za-z0-9_.]+)",
                    @"class\s+([A-Za-z0-9_]+)\s*(?:\(([^)]*)\))?:",
                    @"def\s+([A-Za-z0-9_]+)\s*\(",
                    ".py",
                    "\"\"\"(.*?)\"\"\""
                ),

                "typescript" => new LanguagePatterns(
                    @"(?:export\s+)?(?:namespace|module)\s+([A-Za-z0-9_.]+)|^(?=export\s+(?:class|interface))",
                    @"(?:export\s+)?(?:abstract\s+)?(class|interface|enum|type)\s+([A-Za-z0-9_<>]+)\s*(.*?)(?:\s*\{|$)",
                    @"(?:public\s+|private\s+|protected\s+)?(?:static\s+)?(?:async\s+)?([A-Za6789_]+)\s*[<(]",
                    ".ts",
                    @"/\*\*(.*?)\*/"
                ),

                "javascript" => new LanguagePatterns(
                    @"(?:export\s+)?(?:namespace|module)\s+([A-Za-z0-9_.]+)|^(?=export\s+(?:class|function))",
                    @"(?:export\s+)?(?:abstract\s+)?(class|function)\s+([A-Za-z0-9_$]+)\s*(?:extends\s+)?([^\s{]*)",
                    @"(?:async\s+)?(?:function\s+)?([A-Za-z0-9_$]+)\s*[<(]",
                    ".js",
                    @"/\*\*(.*?)\*/"
                ),

                _ => new LanguagePatterns( // Default: C#
                    @"namespace\s+([A-Za-z0-9_.]+)",
                    @"(class|interface|record|struct|enum)\s+([A-Za-z0-9_]+)\s*([^{]*)",
                    @"public\s+[A-Za-z0-9_<>,\[\]\s]+\s+([A-Za-z0-9_]+)\s*\(",
                    ".cs",
                    @"///\s*<summary>(.*?)</summary>"
                )
            };
        }
    }
}
