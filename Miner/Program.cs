using System;

namespace Guytp.BurstSharp.Miner
{
    class Program
    {
        static void Main(string[] args)
        {
            /* ToDo
             * Miner
             *      Ability to actually submit to a pool (only best deadline per account)
             *      Console UI binds Exit F-Key and makes nonce-side of window active and max-widthed to 80px with header
             *      Loadable config file
             *      Report % completion
             *      Improved logger with different log levels/colours for output
             *      Other stagger sizes
             *      Solo mining
             *      More granular CPU/memory configuration
             * Duplicate nonce detector
             * Plotter
             *      Suggest next nonce based on existing on disk
             *      "Fill Disk" option to chose number of plots
             * GUIs
             */

            using (Miner miner = new Miner())
            {
                miner.Run();
            }
        }
    }
}