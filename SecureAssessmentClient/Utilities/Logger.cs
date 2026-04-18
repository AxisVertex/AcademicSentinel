using log4net;
using log4net.Config;
using System.IO;

namespace SecureAssessmentClient.Utilities
{
    public static class Logger
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Logger));

        static Logger()
        {
            XmlConfigurator.Configure(new FileInfo("Config/log4net.config"));
        }

        public static void Info(string message) => log.Info(message);
        public static void Error(string message, Exception ex = null) => log.Error(message, ex);
        public static void Warn(string message) => log.Warn(message);
        public static void Debug(string message) => log.Debug(message);
    }
}
