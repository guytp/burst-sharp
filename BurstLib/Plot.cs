using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guytp.BurstSharp.BurstLib
{
    public class Plot
    {
        byte[] _data;

        const int HASH_SIZE = 32;
        public const int HASHES_PER_SCOOP = 2;
        public const int SCOOP_SIZE = HASHES_PER_SCOOP * HASH_SIZE;
        public const int SCOOPS_PER_PLOT = 4096; // original 1MB/plot = 16384
        public const int PLOT_SIZE = SCOOPS_PER_PLOT * SCOOP_SIZE;

        public Plot(ulong accountId, ulong nonce)
        {
            byte[] accountIdBytes = BitConverter.GetBytes(accountId);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(accountIdBytes);
            byte[] nonceBytes = BitConverter.GetBytes(nonce);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(nonceBytes);
            byte[] baseBuffer = new byte[16];
            Array.Copy(accountIdBytes, baseBuffer, 8);
            Array.Copy(nonceBytes, 0, baseBuffer, 8, 8);

            int HASH_CAP = 4096;
            _data = new byte[PLOT_SIZE];

            //ShabalGeneric md = new ShabalGeneric(256);
            byte[] gendata = new byte[PLOT_SIZE + baseBuffer.Length];
            Array.Copy(baseBuffer, 0, gendata, PLOT_SIZE, baseBuffer.Length);
            Shabal256 shabel = new Shabal256();
            for (int i = PLOT_SIZE; i > 0; i -= HASH_SIZE)
            {
                //md.reset();
                int len = PLOT_SIZE + baseBuffer.Length - i;
                if (len > HASH_CAP)
                {
                    len = HASH_CAP;
                }
                byte[] buff = new byte[len];
                Array.Copy(gendata, i, buff, 0, len);
                byte[] computed = shabel.ComputeBytes(buff).GetBytes();
                Array.Copy(computed, 0, gendata, i - HASH_SIZE, HASH_SIZE);
                //md.update(gendata, i, len);
                //md.digest(gendata, i - HASH_SIZE, HASH_SIZE);
            }
            byte[] finalhash = shabel.ComputeBytes(gendata).GetBytes();
            //md.reset();
            //md.update(gendata);
            //byte[] finalhash = md.digest();
            for (int i = 0; i < PLOT_SIZE; i++)
            {
                _data[i] = (byte)(gendata[i] ^ finalhash[i % HASH_SIZE]);
            }
        }

        public byte[] GetHash(int number)
        {
            byte[] hash = new byte[32];
            Array.Copy(_data, number * 32, hash, 0, 32);
            return hash;
        }

        public byte[] GetScoop(int number)
        {
            byte[] scoop = new byte[SCOOP_SIZE];
            Array.Copy(_data, number * SCOOP_SIZE, scoop, 0, SCOOP_SIZE);
            return scoop;
        }
    }
}
