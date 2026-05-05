using System;
using System.Threading.Tasks;
using ReadmeSync.Cli;
using ReadmeSync.Tui;

#nullable enable

namespace ReadmeSync
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                if (args.Length == 0 || (args.Length == 1 && args[0] == "--tui"))
                {
                    var tui = new TuiApp();
                    await tui.RunAsync();
                }
                else
                {
                    await CliRunner.RunAsync(args);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ An unexpected error occurred:");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }
        }
    }
}
