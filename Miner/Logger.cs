using System;

namespace Guytp.BurstSharp.Miner
{
    public static class Logger
    {
        public static void Error(string message, Exception ex = null)
        {
            Log(message, ex, LogLevel.Error);
        }

        public static void Info(string message, Exception ex = null)
        {
            Log(message, ex, LogLevel.Info);
        }

        public static void Debug(string message, Exception ex = null)
        {
            Log(message, ex, LogLevel.Debug);
        }

        public static void Warn(string message, Exception ex = null)
        {
            Log(message, ex, LogLevel.Warn);
        }

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
            ConsoleUi.WriteLine(message, foreground);
            if (ex != null)
                ConsoleUi.WriteLine(ex.ToString(), foreground);
        }

    }
}