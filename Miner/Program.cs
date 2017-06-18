using System;

namespace Guytp.BurstSharp.Miner
{
    class Program
    {
        static void Main(string[] args)
        {
            /* ToDo
             * Cancel on new block not working - check each nonce
             * Not sure deadline is correct truncation - see https://github.com/burst-team/burstcoin/blob/master/src/java/nxt/GeneratorImpl.java#L175
             * Multi-thread the noncing versus reading
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