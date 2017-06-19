using System;

namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// Defines a configuration that is used by the miner.
    /// </summary>
    public static class Configuration
    {
        /// <summary>
        /// Gets the directories to read plots from.
        /// </summary>
        public static string[] PlotDirectories
        {
            get
            {
                return new string[] { @"/mnt/Plot" };
                //return new string[] { @"\\10.0.1.32\Plot02" };
            }
        }

#if STUB
        public static string StubJson = "{\"generationSignature\":\"313d2e263791718787dce43e74c08c5973179e3d356e2398da82252ab341e9be\",\"baseTarget\":\"347848\",\"requestProcessingTime\":16,\"height\":\"372517\",\"targetDeadline\":580000}";

        public static ulong StubNonce = 52167538;
#endif

        /// <summary>
        /// Gets the URL for the pool that we are using.
        /// </summary>
        public static string PoolApiUrl {  get { return "http://pool.burstcoin.sk"; } }
        //public static string PoolApiUrl { get { return "http://burst.ninja"; } }

        /// <summary>
        /// Gets the maximum amount of RAM that can be allocated to each reader.
        /// </summary>
        public static uint MemoryLimitPerReader { get { return 500 * 1024 * 1024; } }

        /// <summary>
        /// Gets the maximum amount of RAM that can be allocated in total to plot checking.
        /// </summary>
        public static uint MemoryLimitPlotChecker { get { return 500 * 1024 * 1024; } }

        /// <summary>
        /// Gets how many CPU cores can be assigned to the plot checker.
        /// </summary>
        public static uint ThreadCountPlotChecker { get { return 4; } }
    }
}