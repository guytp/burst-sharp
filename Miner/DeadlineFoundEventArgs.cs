using System;

namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// This class handles encapsulating the details of a new deadline being found.
    /// </summary>
    public class DeadlineFoundEventArgs : EventArgs
    {
        #region Properties
        /// <summary>
        /// Gets the deadline that has been found.
        /// </summary>
        public ulong Deadline { get; private set; }

        /// <summary>
        /// Gets the scoop that this deadline was generated from.
        /// </summary>
        public Scoop Scoop { get; private set; }

        /// <summary>
        /// Gets the mining information that was used to generate this deadline.
        /// </summary>
        public MiningInfo MiningInfo { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of this class.
        /// </summary>
        /// <param name="deadline">
        /// The deadline that has been found.
        /// </param>
        /// <param name="scoop">
        /// The scoop that this deadline was generated from.
        /// </param>
        /// <param name="miningInfo">
        /// The mining information that was used to generate this deadline.
        /// </param>
        public DeadlineFoundEventArgs(ulong deadline, Scoop scoop, MiningInfo miningInfo)
        {
            Scoop = scoop;
            MiningInfo = miningInfo;
            Deadline = deadline;
        }
        #endregion
    }
}