namespace Guytp.BurstSharp.Miner
{
    class Program
    {
        static void Main(string[] args)
        {
            /* ToDo
             * Miner
             *      Console UI
             *              Nonce found info to right pane
             *              Colours for error levels
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