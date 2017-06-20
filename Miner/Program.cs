namespace Guytp.BurstSharp.Miner
{
    class Program
    {
        static void Main(string[] args)
        {
            /* Miner
             *      Submit to network
             *      Colours for error levels
             *      Loadable config file
             *      Report % completion
             *      Improved logger with different log levels/colours for output
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

            using (Miner miner = new Miner())
            {
                miner.Run();
            }
        }
    }
}