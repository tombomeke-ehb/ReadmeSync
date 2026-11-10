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
        private const string TelemetryApiUrl = "https://tombomekestudio.com/api/telemetry/readmesync";

        /// <summary>
        /// True when the user opted out through the READMESYNC_NO_TRACKING or DO_NOT_TRACK
        /// environment variable.
        /// </summary>
        public static bool IsDisabledByEnvironment()
        {
            return IsSet("READMESYNC_NO_TRACKING") || IsSet("DO_NOT_TRACK");

            static bool IsSet(string name)
            {
                string? value = Environment.GetEnvironmentVariable(name);
                return !string.IsNullOrWhiteSpace(value) && value != "0" &&
                       !value.Equals("false", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Version of the running tool, without build metadata.
        /// </summary>
        public static string GetToolVersion()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            string? informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(informational))
                return informational.Split('+')[0];

            return assembly.GetName().Version?.ToString(3) ?? "unknown";
        }

        /// <summary>
        /// Sends telemetry data asynchronously without blocking the application.
        /// Silently fails if network or service is unavailable.
        /// Only the tool version, the scanned language and the OS description are sent.
        /// </summary>
        public async Task SendTelemetryAsync(string language)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

                var payload = new
                {
                    tool_version = GetToolVersion(),
                    language_scanned = language,
                    os_platform = RuntimeInformation.OSDescription
                };

                using var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                await client.PostAsync(TelemetryApiUrl, content);
            }
            catch
            {
                // Silently ignore errors - telemetry should never break the tool
            }
        }
    }
}
