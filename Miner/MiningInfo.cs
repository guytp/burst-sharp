using Newtonsoft.Json;
using System;
using Guytp.BurstSharp.BurstLib;
using System.Collections.Generic;
using System.Text;

namespace Guytp.BurstSharp.Miner
{
    public class MiningInfo
    {
        public string PreviousGenerationSignature { get; private set; }

        public byte[] PreviousGenerationSignatureBytes { get; private set; }
        
        public ulong BaseTarget { get; private set; }

        public ulong BlockHeight { get; private set; }

        public byte[] BlockHeightBytes { get; private set; }

        public ulong Deadline { get; private set; }
        public byte[] DeadlineBytes { get; private set; }

        [JsonConstructor]
        public MiningInfo(string generationSignature, string baseTarget, string height, ulong targetDeadline)
        {
            PreviousGenerationSignature = generationSignature;
            PreviousGenerationSignatureBytes = generationSignature.ToByteArray();
            BaseTarget = ulong.Parse(baseTarget);
            BlockHeight = ulong.Parse(height);
            BlockHeightBytes = BitConverter.GetBytes(BlockHeight);
            Array.Reverse(BlockHeightBytes);
            Deadline = targetDeadline;
            DeadlineBytes = BitConverter.GetBytes(Deadline);
            Array.Reverse(DeadlineBytes);
        }
    }
}