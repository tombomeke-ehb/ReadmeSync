using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ReadmeSync.Models;

namespace ReadmeSync.Services
{
    /// <summary>
    /// Service responsible for generating JSON output for API usage or external integrations.
    /// </summary>
    public class JsonGenerator
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Generates a structured JSON file containing all parsed code information.
        /// Useful for the Live Roadmap API.
        /// </summary>
        public void GenerateJson(string outputFile, IEnumerable<IGrouping<string, CodeFileInfo>> fileGroups)
        {
            File.WriteAllText(outputFile, BuildJson(fileGroups), new UTF8Encoding(false));
        }

        /// <summary>
        /// Returns the JSON document: a flat array with one entry per type.
        /// </summary>
        public string BuildJson(IEnumerable<IGrouping<string, CodeFileInfo>> fileGroups)
        {
            // Flatten the grouped data for an easier-to-consume JSON structure
            var flatList = fileGroups.SelectMany(g => g).Select(f => new
            {
                f.Namespace,
                f.TypeKeyword,
                f.Class,
                f.Inheritance,
                f.Summary,
                f.Methods,
                f.Todos,
                f.TodoItems,
                f.Path,
                f.Link,
                f.Language,
                f.Line
            }).ToList();

            return JsonSerializer.Serialize(flatList, Options);
        }
    }
}
