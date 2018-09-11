using System.Collections.Generic;
using HBS.Logging;

/* Thanks to CptMoore for his Logger */
namespace SpawnVariation.Utils {
    public static class ILogExtensions {
        public static void SetLevel(this ILog @this, LogLevel level) {
            Logger.SetLoggerLevel(@this.Name, level);
        }

        public static LogLevel GetLogLevel(this ILog @this) {
            Logger.GetLoggerLevel(@this.Name, out var logLevel);
            return logLevel;
        }
    }

    public static class LogManager {
        private static readonly Dictionary<string, ILog> Loggers = new Dictionary<string, ILog>();
        private static Dictionary<string, LogLevel> LoggerLevels;
        private static FileLogAppender LogAppender;

        public static ILog GetLogger(string name) {
            if (Loggers.TryGetValue(name, out var logger)) {
                return logger;
            }

            logger = Logger.GetLogger(name);
            SetupLogger(name);
            Loggers[name] = logger;
            return logger;
        }

        private static void SetupLogger(string name) {
            if (LoggerLevels == null || LogAppender == null) {
                return;
            }

            Logger.AddAppender(name, LogAppender);
            if (LoggerLevels.TryGetValue(name, out var level)) {
                Logger.SetLoggerLevel(name, level);
            }
        }

        public static void Setup(string logFilePath, Dictionary<string, LogLevel> loggerLevels) {
            LogAppender = new FileLogAppender(logFilePath, FileLogAppender.WriteMode.INSTANT);
            LoggerLevels = loggerLevels;
            foreach (var logger in Loggers) {
                SetupLogger(logger.Key);
            }
        }
    }
}