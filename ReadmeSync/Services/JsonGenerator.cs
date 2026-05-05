using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ReadmeSync.Models;

namespace ReadmeSync.Services
{
    /// <summary>
    /// Service responsible for generating JSON output for API usage or external integrations.
    /// </summary>
    public class JsonGenerator
    {
        /// <summary>
        /// Generates a structured JSON file containing all parsed code information.
        /// Useful for the Live Roadmap API.
        /// </summary>
        public void GenerateJson(string outputFile, IEnumerable<IGrouping<string, CodeFileInfo>> fileGroups)
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
                f.Path,
                f.Link
            }).ToList();

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            string json = JsonSerializer.Serialize(flatList, options);
            File.WriteAllText(outputFile, json);
        }

        /// <summary>
        /// Generates JSON content as a string without writing to file (dry-run preview).
        /// </summary>
        public string PreviewJson(IEnumerable<IGrouping<string, CodeFileInfo>> fileGroups)
        {
            var flatList = fileGroups.SelectMany(g => g).Select(f => new
            {
                f.Namespace,
                f.TypeKeyword,
                f.Class,
                f.Inheritance,
                f.Summary,
                f.Methods,
                f.Todos,
                f.Path,
                f.Link
            }).ToList();

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(flatList, options);
        }
    }
}
