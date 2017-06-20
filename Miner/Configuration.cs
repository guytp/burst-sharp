using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Reflection;

namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// Defines a configuration that is used by the miner.
    /// </summary>
    public static class Configuration
    {
        #region Properties
        /// <summary>
        /// Gets the directories to read plots from.
        /// </summary>
        public static string[] PlotDirectories { get; private set; }

#if STUB
        public static string StubJson = "{\"generationSignature\":\"313d2e263791718787dce43e74c08c5973179e3d356e2398da82252ab341e9be\",\"baseTarget\":\"347848\",\"requestProcessingTime\":16,\"height\":\"372517\",\"targetDeadline\":580000}";

        public static ulong StubNonce = 52167538;
#endif

        /// <summary>
        /// Gets the URL for the pool that we are using.
        /// </summary>
        public static string PoolApiUrl { get; private set; }

        /// <summary>
        /// Gets the maximum amount of RAM that can be allocated to each reader.
        /// </summary>
        public static uint MemoryLimitPerReader { get; private set; }

        /// <summary>
        /// Gets the maximum amount of RAM that can be allocated in total to plot checking.
        /// </summary>
        public static uint MemoryLimitPlotChecker { get; private set; }

        /// <summary>
        /// Gets how many CPU cores can be assigned to the plot checker.
        /// </summary>
        public static uint ThreadCountPlotChecker { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Handle one-time static construction of the class.
        /// </summary>
        static Configuration()
        {
            try
            {
                string json = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "miner-config.json"));
                JObject obj = JObject.Parse(json);
                PlotDirectories = obj.GetValue("PlotDirectories").ToObject<string[]>();
                PoolApiUrl = obj.GetValue("PoolApiUrl").ToObject<string>();
                MemoryLimitPerReader = obj.GetValue("MemoryLimitPerReader").ToObject<uint>() * 1024 * 1024;
                MemoryLimitPlotChecker = obj.GetValue("MemoryLimitPlotChecker").ToObject<uint>() * 1024 * 1024;
                ThreadCountPlotChecker = obj.GetValue("ThreadCountPlotChecker").ToObject<uint>();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to open config file", ex);
            }
        }
        #endregion
    }
}