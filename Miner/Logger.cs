using System;

namespace Guytp.BurstSharp.Miner
{
    public static class Logger
    {
        public static void Error(string message, Exception ex = null)
        {
            ConsoleUi.WriteLine(message);
            if (ex != null)
                ConsoleUi.WriteLine(ex.ToString());
        }

        public static void Info(string message)
        {
            ConsoleUi.WriteLine(message);
        }
    }
}