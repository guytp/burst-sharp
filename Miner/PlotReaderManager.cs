using System;
using Guytp.BurstSharp.BurstLib;
using System.Threading;
using System.Threading.Tasks;

namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// The plot reader manager is responsible for performing common mining functionality (i.e. scoop determination) before passing it over to individual plot readers to actually mine the data on disk,
    /// once those readers discover a plot it is then responsible for handing this over to a plot checker to perform deadline checking.
    /// </summary>
    public class PlotReaderManager : IDisposable
    {
        #region Declarations
        /// <summary>
        /// Defines the readers that this manager is coordinating.
        /// </summary>
        private PlotReader[] _plotReaders;

        /// <summary>
        /// Defines the hashing algorithm.
        /// </summary>
        private Shabal256 _shabel;

        /// <summary>
        /// Defines how many GB of total storage is used across all plot readers.
        /// </summary>
        private decimal _utilisedStorage;

        /// <summary>
        /// Defines a thread that monitors the progress of a round and updates the UI accordingly.
        /// </summary>
        private Thread _progressMonitoringThread;

        /// <summary>
        /// Defines the mining information about the current block.
        /// </summary>
        private MiningInfo _miningInfo;

        /// <summary>
        /// Defines when the last round started.
        /// </summary>
        private DateTime _lastRoundStart;

        /// <summary>
        /// Defines whether or not this reader manager is alive.
        /// </summary>
        private bool _isAlive;
        #endregion

        #region Events
        /// <summary>
        /// Fired whenever new scoops are discovered.
        /// </summary>
        public event EventHandler<ScoopsDiscoveredEventArgs> ScoopsDiscovered;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of this class.
        /// </summary>
        public PlotReaderManager()
        {
            // Create our shabel for local hashing when determining the scoop number for each block
            _shabel = new Shabal256();
            
            // Now create and hook up to these events
            Logger.Info("Initialising plot readers");
            if (Configuration.PlotDirectories != null && Configuration.PlotDirectories.Length > 0)
            {
                _plotReaders = new PlotReader[Configuration.PlotDirectories.Length];
                Parallel.For(0, Configuration.PlotDirectories.Length, (int i) =>
                {
                    _plotReaders[i] = new PlotReader(Configuration.PlotDirectories[i]);
                    _plotReaders[i].ScoopsDiscovered += PlotReaderOnScoopsDiscovered;
                    _plotReaders[i].UpdateUtilisedStorage();
                    _utilisedStorage += _plotReaders[i].UtilisedStorage;
                });
            }
            Logger.Debug("Finished initialising plot readers");

            // Start the progress monitor
            _isAlive = true;
            _progressMonitoringThread = new Thread(ProgressMonitoringThread) { IsBackground = true, Name = "Plot Reader Progress Monitor" };
            _progressMonitoringThread.Start();
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handle new scoops being discovered and pass it over to the plot checker.
        /// </summary>
        /// <param name="sender">
        /// The event sender.
        /// </param>
        /// <param name="e">
        /// The event arguments.
        /// </param>
        private void PlotReaderOnScoopsDiscovered(object sender, ScoopsDiscoveredEventArgs e)
        {
            ScoopsDiscovered?.Invoke(this, e);
        }
        #endregion

        /// <summary>
        /// This method is the main progress loop which is used to check how we're currently doing and update the UI accordingly.
        /// </summary>
        private void ProgressMonitoringThread()
        {
            bool isFirstLoop = true;
            byte lastAnimationIndex = 0;
            MiningInfo miningInfo = null;
            bool lastVisible = false;

            while (_isAlive)
            {
                // Determine what state we should display - first if block has changed we need to reset everything back to begining
                if (miningInfo != _miningInfo)
                {
                    lastAnimationIndex = 0;
                    miningInfo = _miningInfo;
                }
                bool visible = miningInfo != null;
                if (!visible)
                {
                    // Update UI if we've got a change
                    if (isFirstLoop || lastVisible != visible)
                    {
                        lastVisible = visible;
                        isFirstLoop = false;
                        ConsoleUi.ProgressBarHide();
                    }

                    // Wait to try again
                    Thread.Sleep(50);
                    continue;
                }

                // Determine how far through our readers are of this block (if we're in a block)
                decimal value = 0m;
                string text = null;
                ulong totalScoops = (ulong)(_utilisedStorage * 1000000000 / Plot.SCOOPS_PER_PLOT / Plot.SCOOP_SIZE);
                ulong readScoops = 0;
                ulong totalBytesRead = 0;
                foreach (PlotReader reader in _plotReaders)
                {
                    readScoops += reader.ScoopsRead;
                    totalBytesRead += reader.BytesRead;
                }
                if (totalScoops > 0)
                    value = (decimal)readScoops / (decimal)totalScoops;
                double seconds = DateTime.UtcNow.Subtract(_lastRoundStart).TotalSeconds;
                double bps = totalBytesRead / seconds;
                double mbps = bps / 1000 / 1000;
                text = Math.Round(mbps, 1) + " MBps";

                // Display this on first loop or a change in visibility
                ConsoleUi.ProgressBarSetup(lastAnimationIndex++, value, text);
                if (lastAnimationIndex > 7)
                    lastAnimationIndex = 0;
                lastVisible = true;
                isFirstLoop = false;

                // Wait to redraw
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Starts reading plots for the specifed information and terminates and current plot mining.
        /// </summary>
        /// <param name="miningInfo">
        /// The information to use to commence mining.
        /// </param>
        public void NotifyNewRound(MiningInfo miningInfo)
        {
            // Store this value
            _lastRoundStart = DateTime.UtcNow;
            _miningInfo = miningInfo;

            // First let's kill any existing plot reading
            foreach (PlotReader reader in _plotReaders)
                reader.Terminate();
            
            // If no mining information stop now
            if (miningInfo == null)
                return;

            // Throw error if we have no readers
            if (_plotReaders == null)
            {
                Logger.Warn("Unable to process new block " + miningInfo.BlockHeight + ", no plot readers available");
                return;
            }

            // Now we perform our basics to determine scoop number
            byte[] gensigHashable = new byte[40];
            Array.Copy(miningInfo.PreviousGenerationSignatureBytes, gensigHashable, 32);
            Array.Copy(miningInfo.BlockHeightBytes, 0, gensigHashable, 32, 8);
            byte[] gensig = _shabel.ComputeBytes(gensigHashable).GetBytes();
            uint scoop = (uint)((gensig[gensig.Length - 2] & 0x0F) << 8) | (gensig[gensig.Length - 1]);
            Logger.Debug("Calculated scoop for block as " + scoop);

            // With the config that we have we can now execute all of our readers
            foreach (PlotReader reader in _plotReaders)
                reader.StartMining(miningInfo, scoop);
        }

        #region IDisposable Implementation
        /// <summary>
        /// Free up our resources.
        /// </summary>
        public void Dispose()
        {
            _isAlive = false;
            if (_progressMonitoringThread != null)
            {
                _progressMonitoringThread?.Join();
                _progressMonitoringThread = null;
            }
            if (_plotReaders != null)
            {
                foreach (PlotReader reader in _plotReaders)
                {
                    reader.ScoopsDiscovered -= PlotReaderOnScoopsDiscovered;
                    reader.Dispose();
                }
                _plotReaders = null;
            }
        }
        #endregion
    }
}