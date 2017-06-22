using Guytp.BurstSharp.BurstLib;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// The deadline calculator is responsible for spawning numerous threads that create shabel hashes of plots as soon as it can.  It acts as a blocking queue up to a predefined size with the
    /// idea that the disk throughput attempts to read as much as it can and then shabel load is then spread across all available cores.
    /// </summary>
    public class DeadlineCalculator : IDisposable
    {
        #region Declarations
        /// <summary>
        /// Defines the queue containing scoops that we wish to process.
        /// </summary>
        private List<Scoop> _scoopQueue;

        /// <summary>
        /// Defines a locker object for thread safe access to the queue.
        /// </summary>
        private readonly object _scoopQueueLocker;

        /// <summary>
        /// Defines the maximum number of scoops that we wish to store at any point - beyond this point we block.
        /// </summary>
        private uint _maximumScoops;

        /// <summary>
        /// Defines a list of all threads used to validate plots.
        /// </summary>
        private Thread[] _threads;

        /// <summary>
        /// Determines whether the class and all threads are currently alive.
        /// </summary>
        private bool _isAlive;

        /// <summary>
        /// Defines the information regards to the current block that will be accepted.
        /// </summary>
        private MiningInfo _miningInfo;
        #endregion

        #region Events
        /// <summary>
        /// Fired when a new deadline is found.
        /// </summary>
        public event EventHandler<DeadlineFoundEventArgs> DeadlineFound;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of this class.
        /// </summary>
        /// <param name="maximumScoops">
        /// The maximum number of scoops we can hold in the queue at any point in time.
        /// </param>
        public DeadlineCalculator(uint maximumScoops, uint threads)
        {
            // Store values
            _scoopQueue = new List<Scoop>();
            _scoopQueueLocker = new object();
            _maximumScoops = maximumScoops;

            // Create our threads
            _threads = new Thread[threads];
            _isAlive = true;
            for (int threadNumber = 0; threadNumber < threads; threadNumber++)
            {
                _threads[threadNumber] = new Thread(ThreadEntry) { Name = "Plot Checker " + (threadNumber + 1), IsBackground = true };
                _threads[threadNumber].Start();
            }
        }
        #endregion

        /// <summary>
        /// The main entry point for the threads that individually check for deadlines.
        /// </summary>
        private void ThreadEntry()
        {
            Shabal256 shabal = new Shabal256();
            byte[] hashBuffer = new byte[32 + Plot.SCOOP_SIZE];
            ulong lastBlockHeightPrevGenCopied = 0;
            while (_isAlive)
            {
                // Get a few items from the queue to process
                Scoop[] scoops;
                lock (_scoopQueueLocker)
                {
                    scoops = new Scoop[_scoopQueue.Count > 100 ? 100 : _scoopQueue.Count];
                    if (scoops.Length > 0)
                    {
                        for (int i = 0; i < scoops.Length; i++)
                            scoops[i] = _scoopQueue[_scoopQueue.Count - i - 1];
                        _scoopQueue.RemoveRange(_scoopQueue.Count - scoops.Length, scoops.Length);
                    }
                }

                // If we didn't get an item or if for some reason the mining info has been wiped wait and try again
                if (scoops == null || scoops.Length < 1)
                {
                    Thread.Sleep(200);
                    continue;
                }

                foreach (Scoop scoop in scoops)
                {
                    // Breakout if we're no longer running
                    MiningInfo miningInfo = _miningInfo;
                    if (!_isAlive || miningInfo == null)
                        break;

                    // Ensure we're on right block
                    if (scoop.BlockHeight != miningInfo.BlockHeight)
                        continue;

                    // Calculate the deadline for this scoop
                    if (lastBlockHeightPrevGenCopied != miningInfo.BlockHeight)
                    {
                        Array.Copy(_miningInfo.PreviousGenerationSignatureBytes, hashBuffer, 32);
                        lastBlockHeightPrevGenCopied = miningInfo.BlockHeight;
                    }
                    Array.Copy(scoop.Data, 0, hashBuffer, 32, Plot.SCOOP_SIZE);
                    byte[] target = shabal.ComputeBytes(hashBuffer).GetBytes();
                    ulong targetResult = BitConverter.ToUInt64(target, 0);

                    // And with our target compute a deadline
                    ulong deadline = targetResult / miningInfo.BaseTarget;
                    if (deadline < miningInfo.Deadline)
                        DeadlineFound?.Invoke(this, new DeadlineFoundEventArgs(deadline, scoop, miningInfo));
                }
            }
        }

        /// <summary>
        /// Resets the queue and specifies the current block height.  Any scoops received for different block heights will be ignored.
        /// </summary>
        /// <param name="miningInfo">
        /// The details of which block is currently being processed.
        /// </param>
        public void NotifyNewRound(MiningInfo miningInfo)
        {
            // Store the information and then clear the queue
            lock (_scoopQueueLocker)
            {
                _miningInfo = miningInfo;
                _scoopQueue.Clear();
            }
        }

        /// <summary>
        /// Add a new scoop to the queue if it matches the current block.  If the queue is full block pending the queue emptying.
        /// </summary>
        /// <param name="scoops">
        /// The scoops to add to the queue.
        /// </param>
        public void Enqueue(IEnumerable<Scoop> scoops)
        {
            List<Scoop> toQueue = new List<Scoop>(scoops);
            while (toQueue.Count > 0)
            {
                lock (_scoopQueueLocker)
                {
                    // If no mining info or doesn't match this scoop then return now
                    if (_miningInfo == null)
                        return;

                    // Add any scoops we have room for
                    uint freeCapacity = _maximumScoops - (uint)_scoopQueue.Count;
                    if (freeCapacity > 0)
                    {
                        uint numberToAdd = freeCapacity < toQueue.Count ? freeCapacity : (uint)toQueue.Count;
                        for (int i = 0; i < numberToAdd; i++)
                            _scoopQueue.Add(toQueue[i]);
                        toQueue.RemoveRange(0, (int)numberToAdd);
                    }
                }

                // If we have some left, wait a bit
                if (toQueue.Count > 0)
                    Thread.Sleep(10);
            }
        }

        #region IDisposable Implementation
        /// <summary>
        /// Free up our resources.
        /// </summary>
        public void Dispose()
        {
            // Log the dispose
            Logger.Debug("Stopping PlotChecker");

            // Instruct threads to termintae and wait for them to all die
            _isAlive = false;
            if (_threads != null)
            {
                foreach (Thread thread in _threads)
                    thread.Join();
                _threads = null;
            }
            Logger.Debug("PlotChecker threads all terminated");

            // Set values that should free up anything wanting to queue
            _maximumScoops = uint.MaxValue;
            _miningInfo = null;

        }
        #endregion
    }
}