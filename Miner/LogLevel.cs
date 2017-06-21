namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// Indicates the different levels of logging.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Indicates generally useful day-to-day information for the user.
        /// </summary>
        Info,

        /// <summary>
        /// Indicates a message that could be bad depending on the context or that you may care about.
        /// </summary>
        Warn,

        /// <summary>
        /// Indicates a message detailing an error.
        /// </summary>
        Error,

        /// <summary>
        /// Indicates a message detailing diagnostic data.
        /// </summary>
        Debug
    }
}