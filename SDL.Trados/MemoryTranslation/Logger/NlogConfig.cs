using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;

namespace SDL.Trados.MTUOC.Log
{
    /// <summary>
    /// Nlog configuration
    /// </summary>
    /// <remarks>
    /// Must be called at the start of any application
    /// </remarks>
    public static class NlogConfig
    {
        private const string FILE_NAME = "SDL\\LOGS\\{date}_log.txt";
        private const string TEST_FILE_NAME = "SDL\\LOGS_TEST\\{date}_log.txt";

        /// <summary>
        /// Set configuration
        /// </summary>
        public static void Init(bool test)
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var fileName = Path.Combine(appDataFolder, test ? TEST_FILE_NAME : FILE_NAME);
            var config = new LoggingConfiguration();
            // Targets where to log to: File and Console
            var logfile = new FileTarget("logfile")
            { 
                FileName = fileName.Replace("{date}", DateTime.Now.Date.ToString("yyyyMMdd")),
                Layout = "${date:format=yyyy-MM-dd.HH.mm.ss.ffffff} ${level}: ${message}"
            };
            var logconsole = new ConsoleTarget("logconsole")
            {
                Layout = "${date:format=yyyy-MM-dd.HH.mm.ss.ffffff} ${processid}/${threadid} ${level}: ${message}"
            };

            // Rules for mapping loggers to targets            
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

            // Apply config           
            LogManager.Configuration = config;
        }
    }
}
