using Newtonsoft.Json;
using System;
using Guytp.BurstSharp.BurstLib;

namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// The mining info class contains the information required to mine a particular round and is the data received from the network or pool.
    /// </summary>
    public class MiningInfo
    {
        #region Properties
        /// <summary>
        /// Gets the generation signature for the previous block as a hex string.
        /// </summary>
        public string PreviousGenerationSignature { get; private set; }

        /// <summary>
        /// Gets the generation signature for the previous block.
        /// </summary>
        public byte[] PreviousGenerationSignatureBytes { get; private set; }
        
        /// <summary>
        /// Gets the base target to use when calculating deadlines for round.
        /// </summary>
        public ulong BaseTarget { get; private set; }

        /// <summary>
        /// Gets the block height for this round.
        /// </summary>
        public ulong BlockHeight { get; private set; }

        /// <summary>
        /// Gets the block height for this round expressed as a byte array.
        /// </summary>
        public byte[] BlockHeightBytes { get; private set; }

        /// <summary>
        /// Gets the maximum deadline in seconds that will be permitted in this round.
        /// </summary>
        public ulong Deadline { get; private set; }

        /// <summary>
        /// Gets the maximum deadline in seconds that will be permitted in this round as a byte array.
        /// </summary>
        public byte[] DeadlineBytes { get; private set; }
        #endregion

        #region Properties
        /// <summary>
        /// Create a new instance of this class.
        /// </summary>
        /// <param name="generationSignature">
        /// The generation signature for the previous block as a hex string.
        /// </param>
        /// <param name="baseTarget">
        /// The base target to use when calculating deadlines for round as a JSON string.
        /// </param>
        /// <param name="height">
        /// The block height for this round as a JSON string.
        /// </param>
        /// <param name="targetDeadline">
        /// The maximum deadline in seconds that will be permitted in this round as a JSON string.
        /// </param>
        [JsonConstructor]
        public MiningInfo(string generationSignature, string baseTarget, string height, ulong targetDeadline)
        {
            PreviousGenerationSignature = generationSignature;
            PreviousGenerationSignatureBytes = generationSignature.ToByteArray();
            BaseTarget = ulong.Parse(baseTarget);
            BlockHeight = ulong.Parse(height);
            BlockHeightBytes = BitConverter.GetBytes(BlockHeight);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(BlockHeightBytes);
            Deadline = targetDeadline;
            DeadlineBytes = BitConverter.GetBytes(Deadline);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(DeadlineBytes);
        }
        #endregion
    }
}