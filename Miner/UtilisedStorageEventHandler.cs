using System;

namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// This class encapsulates how much storage the system is using for plots in an event.
    /// </summary>
    public class UtilisedStorageEventHandler : EventArgs
    {
        #region Properties
        /// <summary>
        /// Gets how many gigabytes of storage are being used for plots.
        /// </summary>
        public decimal Gigabytes { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of this class.
        /// </summary>
        /// <param name="gigabytes">
        /// How many gigabytes of storage are being used for plots.
        /// </param>
        public UtilisedStorageEventHandler(decimal gigabytes)
        {
            Gigabytes = gigabytes;
        }
        #endregion
    }
}