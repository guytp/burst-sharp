using System;

namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// This class acts as a very simple logger to record information within the application and control how it is shown.
    /// </summary>
    public static class Logger
    {
        #region Public Control
        /// <summary>
        /// Log an error message.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        /// <param name="ex">
        /// An optional exception related to the message.
        /// </param>
        public static void Error(string message, Exception ex = null)
        {
            if (Configuration.LogError)
            Log(message, ex, LogLevel.Error);
        }

        /// <summary>
        /// Log an informational message.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        /// <param name="ex">
        /// An optional exception related to the message.
        /// </param>
        public static void Info(string message, Exception ex = null)
        {
            if (Configuration.LogInfo)
                Log(message, ex, LogLevel.Info);
        }

        /// <summary>
        /// Log a debug message.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        /// <param name="ex">
        /// An optional exception related to the message.
        /// </param>
        public static void Debug(string message, Exception ex = null)
        {
            if (Configuration.LogDebug)
                Log(message, ex, LogLevel.Debug);
        }

        /// <summary>
        /// Log a warning message.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        /// <param name="ex">
        /// An optional exception related to the message.
        /// </param>
        public static void Warn(string message, Exception ex = null)
        {
            if (Configuration.LogWarn)
                Log(message, ex, LogLevel.Warn);
        }
        #endregion

        /// <summary>
        /// Log a message of the specified level..
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        /// <param name="ex">
        /// An optional exception related to the message.
        /// </param>
        /// <param name="logLevel">
        /// The level of the message to log.
        /// </param>
        private static void Log(string message, Exception ex, LogLevel logLevel)
        {
            ConsoleColor foreground;
            switch (logLevel)
            {
                case LogLevel.Warn:
                    foreground = ConsoleColor.Yellow;
                    break;
                case LogLevel.Error:
                    foreground = ConsoleColor.Red;
                    break;
                case LogLevel.Debug:
                    foreground = ConsoleColor.Green;
                    break;
                default:
                    foreground = ConsoleColor.White;
                    break;
            }
            string formattedMessage = string.Format("[{0:HH}:{0:mm}:{0:ss}] {1}", DateTime.Now, message);
            ConsoleUi.WriteLine(formattedMessage, foreground);
            if (ex != null)
                ConsoleUi.WriteLine(ex.ToString(), foreground);
        }

    }
}