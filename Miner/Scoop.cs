using Guytp.BurstSharp.BurstLib;
using System;

namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// A scoop represents a chunk of data read from a plot file and some associated data during the mining process.
    /// </summary>
    public class Scoop
    {
        #region Properties
        /// <summary>
        /// Gets the height of the block this scoop was found for.
        /// </summary>
        public ulong BlockHeight { get; private set; }

        /// <summary>
        /// Gets the data contained in this scoope.
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Gets the nonce that this scoop came from.
        /// </summary>
        public ulong Nonce { get; private set; }

        /// <summary>
        /// Gets the account ID that generated this scoop.
        /// </summary>
        public ulong AccountId { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of this class.
        /// </summary>
        /// <param name="blockHeight">
        /// The height of the block this scoop was found for.
        /// </param>
        /// <param name="nonce">
        /// The nonce that this scoop came from.
        /// </param>
        /// <param name="accountId">
        /// The account ID that generated this scoop.
        /// </param>
        /// <param name="data">
        /// The data contained in this scoope.
        /// </param>
        /// <param name="bufferOffset">
        /// The offset in data to read the scoop from.
        /// </param>
        public Scoop(ulong blockHeight, ulong nonce, ulong accountId, byte[] data, uint bufferOffset)
        {
            BlockHeight = blockHeight;
            Nonce = nonce;
            AccountId = accountId;
            Data = new byte[Plot.SCOOP_SIZE];
            Array.Copy(data, (int)bufferOffset, Data, 0, Plot.SCOOP_SIZE);
        }
        #endregion
    }
}