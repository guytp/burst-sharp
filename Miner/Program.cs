using System;

namespace Guytp.BurstSharp.Miner
{
    class Program
    {
        static void Main(string[] args)
        {
            /* ToDo
             * Ability to actually submit to a pool (only best deadline per account)
             * Other stagger sizes
             * Solo mining
             */

            using (Miner miner = new Miner())
            {
                Console.Write("Press any key to exit... ");
                Console.ReadKey();
            }
        }

    }
}