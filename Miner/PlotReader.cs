using Guytp.BurstSharp.BurstLib;
using System;
using System.Collections.Generic;
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
        #endregion

        #region Properties
        /// <summary>
        /// Gets how much storage space this plot folder takes up in total as of the last scan.  A scan must have taken place first.
        /// </summary>
        public decimal UtilisedStorage { get; private set; }

        /// <summary>
        /// Gets how many scoops have been read this round.
        /// </summary>
        public ulong ScoopsRead { get; private set; }

        /// <summary>
        /// Gets how many bytes have been read this round.
        /// </summary>
        public ulong BytesRead { get; private set; }
        #endregion

        #region Events
        /// <summary>
        /// Fired whenever new scoops are discovered.
        /// </summary>
        public event EventHandler<ScoopsDiscoveredEventArgs> ScoopsDiscovered;
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
        }
        #endregion

        /// <summary>
        /// Recalculates how much space all the plots take on disk.
        /// </summary>
        public void UpdateUtilisedStorage()
        {
            Logger.Debug("Scanning disk utilisation for " + _directory);
            decimal value = 0;
            try
            {
                string[] files = Directory.GetFiles(_directory, "*_*_*_*");
                foreach (string file in files)
                {
                    try
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
                            continue;
                        if (!ulong.TryParse(fileParts[1], out startNonce))
                            continue;
                        if (!ulong.TryParse(fileParts[2], out numberOfNonces))
                            continue;
                        if (!ulong.TryParse(fileParts[3], out staggerSize))
                            continue;
                        if (staggerSize != numberOfNonces)
                            continue;
                        long expectedSize = (long)(Plot.PLOT_SIZE * numberOfNonces);
                        FileInfo fi = new FileInfo(file);
                        if (fi.Length != expectedSize)
                            continue;
                        value += expectedSize / 1000000000m;
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch
            {
            }
            UtilisedStorage = value;
        }

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
                Logger.Info("Waiting for miner of \"" + _directory + "\" to finish");
                _miningThread.Join();
                _miningThread = null;
            }
        }

        /// <summary>
        /// This is the main entry point to the thread.
        /// </summary>
        private void ThreadEntry()
        {
            try
            {
                // Reset some values
                BytesRead = 0;
                ScoopsRead = 0;
                ulong utilisedStorage = 0;

                // Get which files to mine
                string[] files = Directory.GetFiles(_directory, "*_*_*_*");
                byte[] readBuffer = null;
                List<Scoop> scoops = new List<Scoop>(500);
                Stopwatch swTotal = new Stopwatch();
                swTotal.Start();

                // Process each file
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
                    utilisedStorage += (ulong)expectedSize;

                    // Now we have a good plot file let's take our scoop and for each nonce process it
                    uint desiredBufferSize = (uint)(Plot.SCOOP_SIZE * numberOfNonces);
                    try
                    {
                        using (FileStream stream = File.Open(file, FileMode.Open, FileAccess.Read))
                        {
                            // Create our reading buffers
                            uint remainingToRead = desiredBufferSize;
                            uint bufferSize = desiredBufferSize > Configuration.MemoryLimitPerReader ? Configuration.MemoryLimitPerReader : desiredBufferSize;
                            if (readBuffer == null || readBuffer.Length < bufferSize)
                                readBuffer = new byte[bufferSize];

                            // Move to begining for optimized read
                            long offset = (long)(_scoop * desiredBufferSize);
                            stream.Seek(offset, SeekOrigin.Begin);

                            // Keep reading in large chunks up to maximum memory permitted and then handover to have deadline calculated elsewhere
                            ulong currentNonce = startNonce;
                            while (remainingToRead > 0)
                            {
                                uint read = (uint)stream.Read(readBuffer, 0, (int)(remainingToRead > bufferSize ? bufferSize : remainingToRead));
                                BytesRead += read;
                                remainingToRead -= read;
                                scoops.Clear();
                                for (uint bufferOffset = 0; bufferOffset < read; bufferOffset += Plot.SCOOP_SIZE, currentNonce++)
                                {
                                    scoops.Add(new Scoop(_miningInfo.BlockHeight, currentNonce, accountId, readBuffer, bufferOffset));
                                    ScoopsRead++;
                                }

                                // Notify our callers
                                ScoopsDiscovered?.Invoke(this, new ScoopsDiscoveredEventArgs(scoops));

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
                    double bps = desiredBufferSize / secs;
                    double mbps = bps / 1000 / 1000;
                    Logger.Debug(String.Format("Processed {0} in {1} secs = {2} MBps", file, secs, mbps));

                    // Return if we're done
                    if (!_isAlive)
                        break;
                }
                Logger.Info(String.Format("Read {0} {3}GB in {1} secs = {2} MBps", _directory, swTotal.Elapsed.TotalSeconds, (BytesRead / swTotal.Elapsed.TotalSeconds) / 1000 / 1000, BytesRead / 1000 / 1000 / 1000));
                UtilisedStorage = utilisedStorage;
            }
            catch (Exception ex)
            {
                Logger.Error("Failure enumerating \"" + _directory + "\": " + ex.Message);
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

