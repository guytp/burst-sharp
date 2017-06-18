using System;
using System.Collections.Generic;

namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// This class defines the arguments to an event that describes new scoops having been discovered by a plot reader.
    /// </summary>
    public class ScoopsDiscoveredEventArgs : EventArgs
    {
        #region Properties
        /// <summary>
        /// Gets the scoops encapsulated by this event.
        /// </summary>
        public IEnumerable<Scoop> Scoops { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="scoop">
        /// The scoops encapsulated by this event.
        /// </param>
        public ScoopsDiscoveredEventArgs(IEnumerable<Scoop> scoops)
        {
            Scoops = scoops;
        }
        #endregion
    }
}