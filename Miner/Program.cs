using System;


namespace Guytp.BurstSharp.Miner
{
    class Program
    {
        static void Main(string[] args)
        {
            /* ToDo
             *      Report % completion (and make all the scrolling per-file optional)
             *      Issues with per-line printing causing UI to warp
             *      Other stagger sizes
             *      Solo mining
             *      Package as pre-built binary for each platform
             */
            try
            {
                using (Miner miner = new Miner())
                {
                    miner.Run();
                }
            }
            catch (Exception ex)
            {
                ConsoleUi.WriteLine(ex.ToString());
            }
        }
    }
}