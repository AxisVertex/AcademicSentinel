using System.Text.Json;
using System.IO;

namespace SecureAssessmentClient.Config
{
    public class ServerConfig
    {
        public ServerSettings ServerSettings { get; set; }
        public MonitoringSettings MonitoringSettings { get; set; }
        public LoggingSettings LoggingSettings { get; set; }

        public static ServerConfig Load(string configPath = "Config/AppSettings.json")
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<ServerConfig>(json);
            return config ?? new ServerConfig();
        }
    }

    public class ServerSettings
    {
        public string ApiBaseUrl { get; set; }
        public string SignalRHubUrl { get; set; }
        public string Environment { get; set; }
    }

    public class MonitoringSettings
    {
        public bool EnableEnvironmentCheck { get; set; }
        public bool EnableBehavioralMonitoring { get; set; }
        public int EventTransmissionInterval { get; set; }
        public int ReconnectionRetryCount { get; set; }
        public int ReconnectionRetryDelay { get; set; }
    }

    public class LoggingSettings
    {
        public string LogFilePath { get; set; }
        public string LogLevel { get; set; }
    }
}
