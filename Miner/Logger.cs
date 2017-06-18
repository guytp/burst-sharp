using System;

namespace Guytp.BurstSharp.Miner
{
    public static class Logger
    {
        public static void Error(string message, Exception ex = null)
        {
            Console.WriteLine(message);
            if (ex != null)
                Console.WriteLine(ex.ToString());
        }

        public static void Info(string message)
        {
            Console.WriteLine(message);
        }
    }
}