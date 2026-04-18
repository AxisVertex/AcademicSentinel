using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using SecureAssessmentClient.Testing;
using SecureAssessmentClient.Services;
using SecureAssessmentClient.Utilities;

namespace SecureAssessmentClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// Supports dual-mode startup: WPF (default) or Testing Console (--test flag)
    /// </summary>
    public partial class App : Application
    {
        // P/Invoke to allocate console window for WPF app
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Check if --test argument was passed
            if (e.Args.Length > 0 && e.Args[0] == "--test")
            {
                // Allocate a console window for this WPF application
                AllocConsole();

                // Don't show the MainWindow
                this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                // Launch testing console instead
                try
                {
                    var testConsole = new DetectionTestConsole();
                    await testConsole.RunAsync();
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error launching test console: {ex.Message}");
                    System.Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                }

                // Close the app when console exits
                this.Shutdown();
            }
            // Otherwise, show MainWindow as normal (do nothing - default behavior)
        }

        /// <summary>
        /// Authenticates with server and establishes SignalR connection
        /// Call this from MainWindow when exam is about to start
        /// </summary>
        public static async Task<bool> InitializeServerConnectionAsync(SignalRService signalRService, 
            string email, string password, int roomId)
        {
            try
            {
                // Server configuration (adjust for your environment)
                string serverBaseUrl = "https://localhost:7236";
                string hubUrl = "https://localhost:7236/monitoringHub";

                Logger.Info("🔐 Starting server authentication...");

                // Step 1: Authenticate and get JWT token
                string jwtToken = await signalRService.AuthenticateAsync(serverBaseUrl, email, password);
                if (string.IsNullOrEmpty(jwtToken))
                {
                    Logger.Error("❌ Failed to obtain JWT token");
                    return false;
                }

                Logger.Info("✅ JWT token obtained");

                // Step 2: Connect to SignalR hub with token
                Logger.Info("📡 Connecting to SignalR hub...");
                await signalRService.ConnectAsync(jwtToken, email);

                Logger.Info("✅ SignalR hub connected");

                // Step 3: Join the exam room
                Logger.Info($"📋 Joining exam room {roomId}...");
                bool joinedRoom = await signalRService.JoinExamAsync(roomId);

                if (joinedRoom)
                {
                    Logger.Info("✅ Successfully joined exam room");
                    Logger.Info("🎯 Server integration complete! Real-time monitoring active.");
                    return true;
                }
                else
                {
                    Logger.Error("❌ Failed to join exam room");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("❌ Server initialization failed", ex);
                return false;
            }
        }
    }
}
