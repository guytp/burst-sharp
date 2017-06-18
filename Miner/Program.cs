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
                Console.Write("Press any key to exit... ");
                Console.ReadKey();
            }
        }

    }
}