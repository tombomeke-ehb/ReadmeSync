using System.Text;
using ReadmeSync.Cli;
using ReadmeSync.Models;
using ReadmeSync.Services;

// ============================================================================
// ReadmeSync
// ----------------------------------------------------------------------------
// A command-line tool that scans your source code and generates or updates a
// README or ROADMAP file with an overview of its namespaces, types, public
// methods, documentation comments and open TODO notes.
//
// Everything outside the generated section of the output file is left alone,
// so the tool is safe to run as often as you like.
//
// Run "readmesync --help" for the options.
//
// © 2025 Tombomeke Studios — All rights reserved
// ============================================================================

namespace ReadmeSync
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch (IOException)
            {
                // No console attached; the default encoding will do.
            }

            CliOptions options;
            try
            {
                options = CliOptions.Parse(args);
            }
            catch (CliException ex)
            {
                WriteError(ex.Message);
                Console.Error.WriteLine("Run 'readmesync --help' to see the options.");
                return Runner.ExitError;
            }

            if (options.Version)
            {
                Console.WriteLine(TelemetryService.GetToolVersion());
                return Runner.ExitOk;
            }

            if (options.Help)
            {
                WriteBanner(Console.Out);
                Console.WriteLine(CliOptions.Usage());
                return Runner.ExitOk;
            }

            if (options.Init)
                return InitSettingsFile();

            // Without arguments the interactive mode starts, unless there is nobody to interact with.
            bool interactive = options.Tui ||
                (args.Length == 0 && !Console.IsInputRedirected && !Console.IsOutputRedirected);

            if (interactive)
            {
                InteractiveMode.Run();
                return Runner.ExitOk;
            }

            return Execute(options);
        }

        /// <summary>
        /// Resolves the settings and runs one generation. Returns the process exit code.
        /// </summary>
        public static int Execute(CliOptions options, bool showBanner = true)
        {
            try
            {
                var settings = RunSettings.Resolve(options, Directory.GetCurrentDirectory());

                // In dry-run mode standard output carries the document, so messages go to standard error.
                TextWriter log = settings.Quiet ? TextWriter.Null : settings.DryRun ? Console.Error : Console.Out;
                if (showBanner)
                    WriteBanner(log);

                Task? telemetry = null;
                if (!settings.NoTracking && !TelemetryService.IsDisabledByEnvironment())
                    telemetry = new TelemetryService().SendTelemetryAsync(string.Join(",", settings.Languages));

                int exitCode = Runner.Run(settings, Console.Out, log);

                // Give telemetry a moment to finish, but never hold the user up for it.
                telemetry?.Wait(TimeSpan.FromMilliseconds(1500));
                return exitCode;
            }
            catch (Exception ex) when (ex is CliException or ArgumentException or IOException or UnauthorizedAccessException)
            {
                WriteError(ex.Message);
                return Runner.ExitError;
            }
        }

        private static int InitSettingsFile()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), AppSettings.FileName);
            if (File.Exists(path))
            {
                WriteError($"{AppSettings.FileName} already exists in this directory.");
                return Runner.ExitError;
            }

            File.WriteAllText(path, AppSettings.CreateTemplate());
            Console.WriteLine($"Created {path}");
            return Runner.ExitOk;
        }

        private static void WriteBanner(TextWriter writer)
        {
            if (writer == TextWriter.Null)
                return;

            Console.ForegroundColor = ConsoleColor.Cyan;
            writer.WriteLine($"ReadmeSync {TelemetryService.GetToolVersion()} – Automatically update README or ROADMAP with code overview");
            writer.WriteLine("Made by tombomeke Studios");
            Console.ResetColor();
            writer.WriteLine("--------------------------------------------------------------------------");
            writer.WriteLine();
        }

        private static void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Error: {message}");
            Console.ResetColor();
        }
    }
}
