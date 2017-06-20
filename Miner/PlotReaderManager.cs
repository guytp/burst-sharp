using System;
using Guytp.BurstSharp.BurstLib;
using System.Collections.Generic;
using System.Text;
using System.Threading;

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
        /// Defines the plot checker we use to calculate deadlines.
        /// </summary>
        private PlotChecker _plotChecker;

        /// <summary>
        /// Defines the deadline submitter we use to manage our queue back to the network.
        /// </summary>
        private DeadlineSubmitter _deadlineSubmitter;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of this class.
        /// </summary>
        public PlotReaderManager()
        {
            // Create our shabel for local hashing when determining the scoop number for each block
            _shabel = new Shabal256();

            // Create our plot checker and hook up to its events
            _plotChecker = new PlotChecker(Configuration.MemoryLimitPlotChecker / Plot.SCOOP_SIZE, Configuration.ThreadCountPlotChecker);
            _plotChecker.DeadlineFound += PlotCheckerOnDeadlineFound;

            // Setup our deadline submitter
            _deadlineSubmitter = new DeadlineSubmitter();

            // Now create and hook up to these events
            if (Configuration.PlotDirectories != null && Configuration.PlotDirectories.Length > 0)
            {
                _plotReaders = new PlotReader[Configuration.PlotDirectories.Length];
                for (int i = 0; i < Configuration.PlotDirectories.Length; i++)
                {
                    _plotReaders[i] = new PlotReader(Configuration.PlotDirectories[i]);
                    _plotReaders[i].ScoopsDiscovered += PlotReaderOnScoopsDiscovered;
                }
            }
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
            _plotChecker?.Enqueue(e.Scoops);
        }

        /// <summary>
        /// Handle a deadline being discovered by the plot checker.
        /// </summary>
        /// <param name="sender">
        /// The event sender.
        /// </param>
        /// <param name="e">
        /// The event arguments.
        /// </param>
        private void PlotCheckerOnDeadlineFound(object sender, DeadlineFoundEventArgs e)
        {
            _deadlineSubmitter.NewDeadline(new Deadline(TimeSpan.FromSeconds(e.Deadline), e.Scoop, e.MiningInfo));
        }
        #endregion

        /// <summary>
        /// Starts reading plots for the specifed information and terminates and current plot mining.
        /// </summary>
        /// <param name="miningInfo">
        /// The information to use to commence mining.
        /// </param>
        public void ReadPlots(MiningInfo miningInfo)
        {
            // First let's kill any existing plot reading
            foreach (PlotReader reader in _plotReaders)
                reader.Terminate();

            // Inform deadline submission to scrap old deadlines
            _deadlineSubmitter.Reset(miningInfo);

            // Inform the plot checker of new block
            _plotChecker?.Reset(miningInfo);

            // If no mining information stop now
            if (miningInfo == null)
            {
                Logger.Error("No mining information received, unable to mine");
                return;
            }

            // Log that we're about to read the plots
            Logger.Info("Starting new block: " + miningInfo.BlockHeight);

            // Throw error if we have no readers
            if (_plotReaders == null)
            {
                Logger.Error("Unable to process new block " + miningInfo.BlockHeight + ", no plot readers available");
                return;
            }

            // Now we perform our basics to determine scoop number
            byte[] gensigHashable = new byte[40];
            Array.Copy(miningInfo.PreviousGenerationSignatureBytes, gensigHashable, 32);
            Array.Copy(miningInfo.BlockHeightBytes, 0, gensigHashable, 32, 8);
            byte[] gensig = _shabel.ComputeBytes(gensigHashable).GetBytes();
            uint scoop = (uint)((gensig[gensig.Length - 2] & 0x0F) << 8) | (gensig[gensig.Length - 1]);
            Logger.Info("Calculated scoop for block as " + scoop);

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
            if (_plotReaders != null)
            {
                foreach (PlotReader reader in _plotReaders)
                {
                    reader.ScoopsDiscovered -= PlotReaderOnScoopsDiscovered;
                    reader.Dispose();
                }
                _plotReaders = null;
            }
            if (_plotChecker != null)
            {
                _plotChecker.DeadlineFound -= PlotCheckerOnDeadlineFound;
                _plotChecker?.Dispose();

            }
        }
        #endregion
    }
}