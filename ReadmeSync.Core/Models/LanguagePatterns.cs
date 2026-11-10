using System.Text.RegularExpressions;

namespace ReadmeSync.Models
{
    /// <summary>
    /// Defines the regex profile of a language: how to find namespaces, types,
    /// public methods and documentation comments.
    /// Type patterns use the named groups kind, name and inherit (or extends/implements);
    /// method patterns use name (and recv for Go receivers).
    /// </summary>
    public class LanguagePatterns
    {
        private const RegexOptions Opts = RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant;

        private static readonly string[] Keys =
        {
            "csharp", "java", "kotlin", "python", "typescript", "javascript", "php", "go"
        };

        private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["cs"] = "csharp", ["c#"] = "csharp", ["csharp"] = "csharp",
            ["java"] = "java",
            ["kt"] = "kotlin", ["kotlin"] = "kotlin",
            ["py"] = "python", ["python"] = "python",
            ["ts"] = "typescript", ["typescript"] = "typescript",
            ["js"] = "javascript", ["javascript"] = "javascript",
            ["php"] = "php",
            ["go"] = "go", ["golang"] = "go",
        };

        private static readonly Dictionary<string, LanguagePatterns> Cache = new();
        private static readonly object CacheLock = new();

        private Regex? _namespaceRegex, _typeRegex, _methodRegex, _interfaceMethodRegex, _summaryRegex;

        /// <summary>Language key, for example "csharp".</summary>
        public string Name { get; }

        /// <summary>Human readable name, for example "C#".</summary>
        public string DisplayName { get; }

        public string Namespace { get; }
        public string Class { get; }
        public string Method { get; }

        /// <summary>Pattern for members of interfaces, which carry no access modifier. May be empty.</summary>
        public string InterfaceMethod { get; }

        /// <summary>Primary file extension, including the dot.</summary>
        public string Extension { get; }

        /// <summary>All file extensions that belong to the language.</summary>
        public IReadOnlyList<string> Extensions { get; }

        public string Summary { get; }

        /// <summary>Namespace used when a file declares none; empty when a namespace is required.</summary>
        public string FallbackNamespace { get; }

        public Regex NamespaceRegex => _namespaceRegex ??= new Regex(Namespace, Opts);
        public Regex TypeRegex => _typeRegex ??= new Regex(Class, Opts);
        public Regex MethodRegex => _methodRegex ??= new Regex(Method, Opts);
        public Regex? InterfaceMethodRegex => InterfaceMethod.Length == 0
            ? null
            : _interfaceMethodRegex ??= new Regex(InterfaceMethod, Opts);
        public Regex SummaryRegex => _summaryRegex ??= new Regex(Summary, Opts | RegexOptions.Singleline);

        private LanguagePatterns(
            string name, string displayName, string ns, string cls, string method, string interfaceMethod,
            string[] extensions, string summary, string fallbackNamespace)
        {
            Name = name;
            DisplayName = displayName;
            Namespace = ns;
            Class = cls;
            Method = method;
            InterfaceMethod = interfaceMethod;
            Extensions = extensions;
            Extension = extensions[0];
            Summary = summary;
            FallbackNamespace = fallbackNamespace;
        }

        /// <summary>Keys of all supported languages.</summary>
        public static IReadOnlyList<string> SupportedLanguages => Keys;

        /// <summary>Maps a name or alias ("cs", "ts", "golang", ...) to a language key, or null when unknown.</summary>
        public static string? Normalize(string? lang)
        {
            if (string.IsNullOrWhiteSpace(lang)) return null;
            return Aliases.TryGetValue(lang.Trim(), out var key) ? key : null;
        }

        public static bool IsSupported(string? lang) => Normalize(lang) != null;

        /// <summary>Finds the language a file belongs to by its name, or null when it is not a supported source file.</summary>
        public static string? FromFileName(string fileName)
        {
            string lower = fileName.ToLowerInvariant();
            if (lower.EndsWith(".d.ts") || lower.EndsWith(".min.js")) return null;

            foreach (var key in Keys)
            {
                if (For(key).Extensions.Any(ext => lower.EndsWith(ext)))
                    return key;
            }
            return null;
        }

        /// <summary>Returns the profile of a language. Unknown names fall back to C#.</summary>
        public static LanguagePatterns For(string lang)
        {
            string key = Normalize(lang) ?? "csharp";
            lock (CacheLock)
            {
                if (!Cache.TryGetValue(key, out var patterns))
                {
                    patterns = Create(key);
                    Cache[key] = patterns;
                }
                return patterns;
            }
        }

        private const string JavaDoc = @"/\*\*(.*?)\*/";

        private static LanguagePatterns Create(string key)
        {
            return key switch
            {
                "java" => new LanguagePatterns(
                    "java", "Java",
                    @"^\s*package\s+([A-Za-z0-9_.]+)",
                    @"(?<![.\w])(?<kind>class|interface|enum|record|@interface)\s+(?<name>[A-Za-z_]\w*)\s*(?:<[^>{]*>)?\s*(?<inherit>[^{;]*)",
                    @"\bpublic\s+(?:(?:static|final|abstract|synchronized|native|default|strictfp)\s+)*(?:<[^>()]*>\s*)?[A-Za-z_][\w<>,\[\]?.\s]*?\s+(?<name>[A-Za-z_]\w*)\s*\(",
                    @"^[ \t]*(?:(?:public|static|abstract|default)\s+)*(?:<[^>()]*>\s*)?(?<ret>[A-Za-z_][\w<>,\[\]?.]*)\s+(?<name>[A-Za-z_]\w*)\s*\(",
                    new[] { ".java" },
                    JavaDoc,
                    ""
                ),

                "kotlin" => new LanguagePatterns(
                    "kotlin", "Kotlin",
                    @"^\s*package\s+([A-Za-z0-9_.]+)",
                    @"(?<![.\w])(?<kind>(?:(?:data|enum|sealed|annotation|value)\s+)?class|interface|object)\s+(?<name>[A-Za-z_]\w*)\s*(?:<[^>{]*>)?\s*(?:\([^)]*\))?\s*(?::\s*(?<inherit>[^{\n]*))?",
                    @"^[ \t]*(?:(?:public|override|open|abstract|suspend|inline|operator|infix|final)\s+)*fun\s+(?:<[^>()]*>\s*)?(?:[\w.<>?]+\.)?(?<name>[A-Za-z_]\w*)\s*\(",
                    "",
                    new[] { ".kt", ".kts" },
                    JavaDoc,
                    ""
                ),

                "php" => new LanguagePatterns(
                    "php", "PHP",
                    @"\bnamespace\s+([A-Za-z0-9_\\]+)\s*[;{]",
                    @"(?<![:\w$>])(?<kind>class|interface|trait|enum)\s+(?<name>[A-Za-z_]\w*)(?:\s*:\s*\w+)?(?:\s+extends\s+(?<extends>[\w\\]+(?:\s*,\s*[\w\\]+)*))?(?:\s+implements\s+(?<implements>[\w\\]+(?:\s*,\s*[\w\\]+)*))?",
                    @"\b(?:(?:static|final|abstract)\s+)*public\s+(?:(?:static|final|abstract)\s+)*function\s+&?(?<name>[A-Za-z_]\w*)\s*\(",
                    "",
                    new[] { ".php" },
                    JavaDoc,
                    ""
                ),

                "python" => new LanguagePatterns(
                    "python", "Python",
                    @"#\s*Package:\s*([A-Za-z0-9_.]+)",
                    @"^[ \t]*class\s+(?<name>[A-Za-z_]\w*)\s*(?:\((?<inherit>[^)]*)\))?\s*:",
                    @"^[ \t]*(?:async\s+)?def\s+(?<name>[A-Za-z_]\w*)\s*\(",
                    "",
                    new[] { ".py" },
                    "(?:\"\"\"(.*?)\"\"\"|'''(.*?)''')",
                    "default"
                ),

                "typescript" => new LanguagePatterns(
                    "typescript", "TypeScript",
                    @"\b(?:namespace|module)\s+([A-Za-z0-9_.]+)\s*\{",
                    @"(?<![.\w$])(?:(?<kind>class|interface|enum)\s+(?<name>[A-Za-z_$][\w$]*)\s*(?:<[^>{]*>)?\s*(?<inherit>[^{;=]*)|(?<kind>type)\s+(?<name>[A-Za-z_$][\w$]*)\s*(?:<[^>=]*>)?\s*=)",
                    @"^[ \t]*(?:public\s+)?(?:static\s+)?(?:override\s+)?(?:async\s+)?(?:(?:get|set)\s+)?(?<name>[A-Za-z_$][\w$]*)\s*(?:<[^>()]*>)?\s*\([^()]*(?:\([^()]*\)[^()]*)*\)\s*(?::\s*[^;{=]+?)?\s*\{",
                    @"^[ \t]*(?<name>[A-Za-z_$][\w$]*)\??\s*(?:<[^>()]*>)?\s*\([^()]*(?:\([^()]*\)[^()]*)*\)\s*:",
                    new[] { ".ts", ".tsx" },
                    JavaDoc,
                    "global"
                ),

                "javascript" => new LanguagePatterns(
                    "javascript", "JavaScript",
                    @"\b(?:namespace|module)\s+([A-Za-z0-9_.]+)\s*\{",
                    @"(?<![.\w$])(?<kind>class)\s+(?<name>[A-Za-z_$][\w$]*)(?:\s+extends\s+(?<inherit>[\w$.]+))?|^(?:export\s+(?:default\s+)?)?(?:async\s+)?(?<kind>function)\s*\*?\s*(?<name>[A-Za-z_$][\w$]*)\s*\(",
                    @"^[ \t]*(?:static\s+)?(?:async\s+)?\*?[ \t]*(?:(?:get|set)\s+)?(?<name>[A-Za-z_$][\w$]*)\s*\([^()]*(?:\([^()]*\)[^()]*)*\)\s*\{",
                    "",
                    new[] { ".js", ".jsx", ".mjs", ".cjs" },
                    JavaDoc,
                    "global"
                ),

                "go" => new LanguagePatterns(
                    "go", "Go",
                    @"^package\s+([A-Za-z0-9_]+)",
                    @"^type\s+(?<name>[A-Za-z_]\w*)(?:\[[^\]]*\])?\s+(?<kind>struct|interface)\b",
                    @"^func\s+(?:\(\s*\w*\s*\*?(?<recv>[A-Za-z_]\w*)(?:\[[^\]]*\])?\s*\)\s*)?(?<name>[A-Z]\w*)\s*(?:\[[^\]]*\])?\s*\(",
                    @"^[ \t]+(?<name>[A-Z]\w*)\s*\(",
                    new[] { ".go" },
                    @"((?:^[ \t]*//[^\n]*\n)+)",
                    ""
                ),

                _ => new LanguagePatterns( // Default: C#
                    "csharp", "C#",
                    @"\bnamespace\s+([A-Za-z0-9_.]+)",
                    @"(?<![.\w@])(?<kind>class|interface|record(?:\s+(?:struct|class))?|struct|enum)\s+(?<name>[A-Za-z_]\w*)\s*(?:<[^>{]*>)?\s*(?<inherit>[^{;]*)",
                    @"\bpublic\s+(?:(?:static|virtual|override|abstract|async|sealed|new|extern|unsafe|partial|readonly)\s+)*[A-Za-z_][\w<>,\[\]?.\s]*?\s+(?<name>[A-Za-z_]\w*)\s*(?:<[^>()]*>)?\s*\(",
                    @"^[ \t]*(?:(?:public|static|abstract|new|async)\s+)*(?<ret>[A-Za-z_][\w<>,\[\]?.]*)\s+(?<name>[A-Za-z_]\w*)\s*(?:<[^>()]*>)?\s*\(",
                    new[] { ".cs" },
                    @"///\s*<summary>(.*?)</summary>",
                    ""
                )
            };
        }
    }
}
