using Guytp.BurstSharp.BurstLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// A plot reader is responsible for reading and processing a single plot directory or drive.
    /// </summary>
    public class PlotReader : IDisposable
    {
        #region Declarations
        /// <summary>
        /// Defines the directory we are reading.
        /// </summary>
        private string _directory;

        /// <summary>
        /// Defines whether we are currently alive.
        /// </summary>
        private bool _isAlive;

        /// <summary>
        /// Defines the thread we use to actually mine.
        /// </summary>
        private Thread _miningThread;

        /// <summary>
        /// Defines the current mining info for the active thread.
        /// </summary>
        private MiningInfo _miningInfo;

        /// <summary>
        /// Defines the current scoop for the active thread.
        /// </summary>
        private ulong _scoop;

        /// <summary>
        /// Defines our hashing algorithm.
        /// </summary>
        private Shabal256 _shabal;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="directory">
        /// The directory we are reading.
        /// </param>
        public PlotReader(string directory)
        {
            _directory = directory;
            _shabal = new Shabal256();
        }
        #endregion

        /// <summary>
        /// Commence mining for the current block.
        /// </summary>
        /// <param name="miningInfo">
        /// Information used during the mining session including current network parameters.
        /// </param>
        /// <param name="scoop">
        /// The scoop that this session will use.
        /// </param>
        internal void StartMining(MiningInfo miningInfo, uint scoop)
        {
            if (_miningThread != null || _isAlive)
                throw new Exception("Thread is already mining");
            _miningInfo = miningInfo;
            _scoop = scoop;
            Logger.Info("Mining " + _directory + " for block " + miningInfo.BlockHeight + " with scoop " + scoop);
            _isAlive = true;
            _miningThread = new Thread(ThreadEntry) { Name = "Plot Reader: " + _directory, IsBackground = true };
            _miningThread.Start();
        }

        /// <summary>
        /// Terminate any in-progress reading.
        /// </summary>
        public void Terminate()
        {
            _isAlive = false;
            if (_miningThread != null)
            {
                _miningThread?.Join();
                _miningThread = null;
            }
        }

        /// <summary>
        /// This is the main entry point to the thread.
        /// </summary>
        private void ThreadEntry()
        {
            string[] files = Directory.GetFiles(_directory, "*_*_*_*");
            byte[] hashBuffer = new byte[32 + Plot.SCOOP_SIZE];
            Array.Copy(_miningInfo.PreviousGenerationSignatureBytes, hashBuffer, 32);
            foreach (string file in files)
            {
                // Get handle to file and ensure it is valid
                Stopwatch sw = new Stopwatch();
                sw.Start();
                string[] fileParts = Path.GetFileName(file).Split(new char[] { '_' }, 4);
                ulong accountId;
                ulong startNonce;
                ulong numberOfNonces;
                ulong staggerSize;
                if (!ulong.TryParse(fileParts[0], out accountId))
                {
                    Logger.Error("Unable to parse account ID from " + file);
                    continue;
                }
                if (!ulong.TryParse(fileParts[1], out startNonce))
                {
                    Logger.Error("Unable to parse start nonce from " + file);
                    continue;
                }
                if (!ulong.TryParse(fileParts[2], out numberOfNonces))
                {
                    Logger.Error("Unable to parse number of nonces from " + file);
                    continue;
                }
                if (!ulong.TryParse(fileParts[3], out staggerSize))
                {
                    Logger.Error("Unable to parse stagger size from " + file);
                    continue;
                }
                if (staggerSize != numberOfNonces)
                {
                    Logger.Error("Unoptimised file, will not process " + file);
                    continue;
                }
                long expectedSize = (long)(Plot.PLOT_SIZE * numberOfNonces);
                FileInfo fi = new FileInfo(file);
                if (fi.Length != expectedSize)
                {
                    Logger.Error("Plot file is not expected size, skipping " + file);
                    continue;
                }

                // Now we have a good plot file let's take our scoop and for each nonce process it
                uint totalScoopSize = (uint)(Plot.SCOOP_SIZE * numberOfNonces);
                try
                {
                    using (FileStream stream = File.Open(file, FileMode.Open, FileAccess.Read))
                    {
                        // Move to begining for optimized read
                        long offset = (long)(_scoop * totalScoopSize);
                        stream.Seek(offset, SeekOrigin.Begin);

                        // Keep reading in large chunks up to maximum memory permitted and then handover to have deadline calculated elsewhere
                        //byte[] readBuffer = new byte[Plot.SCOOP_SIZE * numberOfNonces];
                        //stream.Read(readBuffer, 0, readBuffer.Length);

                        for (ulong nonce = 0; nonce < numberOfNonces; nonce++)
                        {
                            // Read this scoop into our buffer
                            stream.Read(hashBuffer, 32, Plot.SCOOP_SIZE);

                            // Determine the target from this scoop
                            byte[] target = _shabal.ComputeBytes(hashBuffer).GetBytes();
                            byte[] targetSwizzled = new byte[8];
                            Array.Copy(target, targetSwizzled, 8);
                            Array.Reverse(targetSwizzled);
                            ulong targetResult = BitConverter.ToUInt64(targetSwizzled, 0);

                            // And with our target compute a deadline
                            ulong deadline = targetResult / _miningInfo.BaseTarget;
                            if (deadline < _miningInfo.Deadline)
                                Logger.Info("Found new deadline " + TimeSpan.FromSeconds(deadline) + " for block height " + _miningInfo.BlockHeight);

                            // Return if we're done
                            if (!_isAlive)
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Error whilst processing " + file, ex);
                }
                sw.Stop();
                double secs = sw.Elapsed.TotalSeconds;
                double bps = totalScoopSize / secs;
                double mbps = bps / 1024 / 1024;
                Logger.Info(String.Format("Processed {0} in {1} secs = {2} MBps", Path.GetFileName(file), secs, mbps));

                // Return if we're done
                if (!_isAlive)
                    break;
            }
            _isAlive = false;
            _miningThread = null;
        }


        #region IDisposable Implementation
        /// <summary>
        /// Free up our resources.
        /// </summary>
        public void Dispose()
        {
            Terminate();
        }
        #endregion
    }
}
 
 