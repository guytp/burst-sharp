﻿using System;

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
                return new string[] { @"\\gtp-npt-san-01\plot", @"\\tsclient\root\mnt\Plot03", @"\\tsclient\root\mnt\SAN-02\Plot" };
            }
        }

        /// <summary>
        /// Gets the URL for the pool that we are using.
        /// </summary>
        //public static string PoolApiUrl {  get { return "http://pool.burstcoin.sk"; } }
        public static string PoolApiUrl { get { return "http://burst.ninja"; } }

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