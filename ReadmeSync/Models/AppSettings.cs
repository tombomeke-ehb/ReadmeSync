using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReadmeSync.Models
{
    /// <summary>
    /// Project settings read from a .readmesync.json file. Every value is optional;
    /// command-line options take precedence over the file.
    /// </summary>
    public class AppSettings
    {
        public const string FileName = ".readmesync.json";

        private static readonly JsonSerializerOptions ReadOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>One language, a comma-separated list, or "auto".</summary>
        [JsonPropertyName("language")]
        public string? Language { get; set; }

        /// <summary>Directory to scan, relative to the settings file.</summary>
        [JsonPropertyName("scanDirectory")]
        public string? ScanDirectory { get; set; }

        /// <summary>Output file, relative to the repository root.</summary>
        [JsonPropertyName("outputFile")]
        public string? OutputFile { get; set; }

        [JsonPropertyName("repositoryUrl")]
        public string? RepositoryUrl { get; set; }

        [JsonPropertyName("branch")]
        public string? Branch { get; set; }

        /// <summary>Extra folders or files to skip, on top of the default excludes.</summary>
        [JsonPropertyName("exclude")]
        public string[]? Exclude { get; set; }

        [JsonPropertyName("useEmojis")]
        public bool? UseEmojis { get; set; }

        [JsonPropertyName("toc")]
        public bool? Toc { get; set; }

        [JsonPropertyName("timestamp")]
        public bool? Timestamp { get; set; }

        [JsonPropertyName("outputJson")]
        public bool? OutputJson { get; set; }

        [JsonPropertyName("noTracking")]
        public bool? NoTracking { get; set; }

        /// <summary>Reads a settings file. Comments and trailing commas are allowed.</summary>
        public static AppSettings Load(string path)
        {
            try
            {
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), ReadOptions) ?? new AppSettings();
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"Invalid settings file {path}: {ex.Message}", ex);
            }
        }

        /// <summary>Returns a settings file with every option spelled out at its default.</summary>
        public static string CreateTemplate()
        {
            var template = new AppSettings
            {
                Language = "auto",
                ScanDirectory = ".",
                OutputFile = "README.md",
                RepositoryUrl = "",
                Branch = "HEAD",
                Exclude = Array.Empty<string>(),
                UseEmojis = false,
                Toc = false,
                Timestamp = true,
                OutputJson = false,
                NoTracking = false
            };
            return JsonSerializer.Serialize(template, WriteOptions) + Environment.NewLine;
        }
    }
}
