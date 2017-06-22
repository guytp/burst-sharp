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

        /// <summary>
        /// Gets whether or not info messages should be logged.
        /// </summary>
        public static bool LogInfo { get; private set; }

        /// <summary>
        /// Gets whether or not error messages should be logged.
        /// </summary>
        public static bool LogError { get; private set; }

        /// <summary>
        /// Gets whether or not debug messages should be logged.
        /// </summary>
        public static bool LogDebug { get; private set; }

        /// <summary>
        /// Gets whether or not warning messages should be logged.
        /// </summary>
        public static bool LogWarn { get; private set; }
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
                LogInfo = obj.GetValue("LogInfo").ToObject<bool>();
                LogError = obj.GetValue("LogError").ToObject<bool>();
                LogDebug = obj.GetValue("LogDebug").ToObject<bool>();
                LogWarn = obj.GetValue("LogWarn").ToObject<bool>();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to open config file", ex);
            }
        }
        #endregion
    }
}