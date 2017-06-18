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
    }
}