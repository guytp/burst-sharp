using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using Guytp.BurstSharp.BurstLib;

namespace Guytp.BurstSharp.Plotter
{
    class Program
    {
        static bool[] _threadsRunning;
        static object _fsLocker = new object();

        static void Main(string[] args)
        {
            ulong accountId = 9729149722895447507;
            ulong nonce = 0;
            uint toCreate = 400;

            Log("Starting generation...");
            string fn = accountId + "_" + nonce + "_" + toCreate + "_" + toCreate;
            if (File.Exists(fn))
                File.Delete(fn);
            FileStream fs = File.Create(fn, 65535, FileOptions.RandomAccess);
            fs.SetLength((long)(Plot.PLOT_SIZE * toCreate));

            int threadCount = System.Environment.ProcessorCount;
            _threadsRunning = new bool[threadCount];

            ulong startNonce = nonce;
            uint perThread = (uint)(toCreate / threadCount);
            for (int i = 0; i < threadCount; i++)
            {
                Thread thr = new Thread(ThreadEntry);
                _threadsRunning[i] = true;
                uint amountForThread = perThread + (i != threadCount - 1 ? 0 : (uint)(toCreate % threadCount));
                thr.Start(new object[] { nonce, toCreate, startNonce, amountForThread, accountId, i, fs });
                startNonce += amountForThread;
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (_threadsRunning.Any(tr => tr))
            {
                Thread.Sleep(100);
            }
            sw.Stop();

            fs.Dispose();

            Log("All threads completed in " + sw.Elapsed + " = " + (toCreate / sw.Elapsed.Seconds) + " per second");
        }

        private static void ThreadEntry(object args)
        {
            object[] argArray = (object[])args;
            ulong fileNonce = (ulong)argArray[0];
            uint allThreadGenerationCount = (uint)argArray[1];
            ulong startNonce = (ulong)argArray[2];
            uint numberOfNonces = (uint)argArray[3];
            ulong accountId = (ulong)argArray[4];
            int threadNumber = (int)argArray[5];
            FileStream fs = (FileStream)argArray[6];
            CalculatePlots(fileNonce, allThreadGenerationCount, startNonce, numberOfNonces, accountId, threadNumber, fs);
        }

        private static void CalculatePlots(ulong fileNonce, uint allThreadGenerationCount, ulong startNonce, uint numberOfNonces, ulong accountId, int threadNumber, FileStream fs)
        {
            ulong lastNonce = startNonce + numberOfNonces;
            uint totalScoopSize = (uint)(Plot.SCOOP_SIZE * allThreadGenerationCount);
            for (ulong nonce = startNonce; nonce < lastNonce; nonce++)
            {
                Plot plot = new Plot(accountId, nonce);
                for (int scoopNumber = 0; scoopNumber < Plot.SCOOPS_PER_PLOT; scoopNumber++)
                {
                    byte[] buff = plot.GetScoop(scoopNumber);
                    int plotNumber = (int)(nonce - fileNonce);
                    int offset = (int)(scoopNumber * totalScoopSize) + (Plot.SCOOP_SIZE * plotNumber);
                    lock (_fsLocker)
                    {
                        fs.Seek(offset, SeekOrigin.Begin);
                        fs.Write(buff, 0, buff.Length);
                    }
                }
            }
            _threadsRunning[threadNumber] = false;
        }


        static void Log(string message)
        {
            Console.WriteLine(message);
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

    }
}