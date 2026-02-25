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
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;

        public TelemetryService()
        {
            _supabaseUrl = "https://botvdxbfaffjyaidiulb.supabase.co";
            _supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImJvdHZkeGJmYWZmanlhaWRpdWxiIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzE1OTA5MDAsImV4cCI6MjA4NzE2NjkwMH0.ax0GpGn9SktnJVfDnLmSo2IV2n8AZnIrFb7-3ZCG1jw";
        }

        /// <summary>
        /// Sends telemetry data asynchronously without blocking the application.
        /// Silently fails if network or service is unavailable.
        /// </summary>
        public async Task SendTelemetryAsync(string language)
        {
            try
            {
                // Skip if using placeholder values
                if (_supabaseUrl.Contains("VUL_HIER"))
                    return;

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", _supabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabaseKey}");

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

                await client.PostAsync($"{_supabaseUrl}/rest/v1/telemetry_logs", content);
            }
            catch
            {
                // Silently ignore errors - telemetry should never break the tool
            }
        }
    }
}
