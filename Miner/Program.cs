using System;


namespace Guytp.BurstSharp.Miner
{
    class Program
    {
        static void Main(string[] args)
        {
            /* Miner
             *      Colours for error levels
             *      Improved logger (different levels, optional to file)
             *      Report % completion (and make all the scrolling per-file optional)
             *      Issues with per-line printing causing UI to warp
             *      Other stagger sizes
             *      Solo mining
             *      More granular CPU/memory configuration
             * Data Integrity
             *      Duplicate nonce detector
             *      Optimization
             *      Where to start nonces from on drives
             *      Fill up free space
             * Plotter
             *      Suggest next nonce based on existing on disk
             *      "Fill Disk" option to chose number of plots
             * GUIs
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