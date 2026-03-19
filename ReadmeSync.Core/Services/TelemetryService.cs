using System;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ReadmeSync.Services
{
    /// <summary>
    /// Service responsible for sending anonymous telemetry data.
    /// </summary>
    public class TelemetryService
    {
        private readonly string _telemetryApiUrl;

        public TelemetryService()
        {
            // Pointing to your own .NET backend server
            _telemetryApiUrl = "https://tombomekestudio.com/api/telemetry/readmesync";
        }

        /// <summary>
        /// Sends telemetry data asynchronously without blocking the application.
        /// Silently fails if network or service is unavailable.
        /// </summary>
        public async Task SendTelemetryAsync(string language)
        {
            try
            {
                using var client = new HttpClient();
                
                // Optioneel: voeg een simpele API key toe als header voor beveiliging van je eigen API
                // client.DefaultRequestHeaders.Add("X-API-Key", "JOUW_GEHEIME_API_KEY");

                var payload = new
                {
                    tool_version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
                    language_scanned = language,
                    os_platform = RuntimeInformation.OSDescription
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                await client.PostAsync(_telemetryApiUrl, content);
            }
            catch
            {
                // Silently ignore errors - telemetry should never break the tool
            }
        }
    }
}
