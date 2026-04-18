using System;
using System.Threading.Tasks;
using SecureAssessmentClient.Testing;

namespace SecureAssessmentClient
{
    /// <summary>
    /// Program entry point for testing harness
    /// Runs detection pipeline validation console
    /// </summary>
    class TestProgram
    {
        static async Task Main(string[] args)
        {
            try
            {
                var testConsole = new DetectionTestConsole();
                await testConsole.RunAsync();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Unhandled exception: {ex}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        }
    }
}
