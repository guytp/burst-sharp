using Guytp.BurstSharp.BurstLib;
using System;

namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// The miner is responsible for coordinating the actions of all other parts of the program.
    /// </summary>
    public class Miner : IDisposable
    {
        #region Declarations
        /// <summary>
        /// Defines the object that updates our mining status.
        /// </summary>
        private MiningInfoUpdater _miningInfoUpdater;

        /// <summary>
        /// Defines the object that coordinates reading of plots across different disks.
        /// </summary>
        private PlotReaderManager _plotReaderManager;

        /// <summary>
        /// Defines the console UI for this miner.
        /// </summary>
        private ConsoleUi _consoleUi;

        /// <summary>
        /// Defines whether the miner has been started.
        /// </summary>
        private bool _started;

        /// <summary>
        /// Defines the plot checker we use to calculate deadlines.
        /// </summary>
        private DeadlineCalculator _deadlineCalculator;

        /// <summary>
        /// Defines the deadline submitter we use to manage our queue back to the network.
        /// </summary>
        private DeadlineSubmitter _deadlineSubmitter;
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handle new mining info being received.
        /// </summary>
        /// <param name="sender">
        /// The event sender.
        /// </param>
        /// <param name="e">
        /// The event arguments.
        /// </param>
        private void OnMiningInfoUpdate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Update the UI
            if (_miningInfoUpdater.MiningInfo != null)
                Logger.Info("Starting new block: " + _miningInfoUpdater.MiningInfo.BlockHeight);
            else
                Logger.Error("Empty mining info received");
            ConsoleUi.SetTextAreaHeader(_miningInfoUpdater?.MiningInfo != null ? "Block " + _miningInfoUpdater.MiningInfo.BlockHeight : null);

            // Start all objects on the new block
            _plotReaderManager.NotifyNewRound(_miningInfoUpdater.MiningInfo);
            _deadlineSubmitter.NotifyNewRound(_miningInfoUpdater.MiningInfo);
            _deadlineCalculator.NotifyNewRound(_miningInfoUpdater.MiningInfo);
        }

        /// <summary>
        /// Handle new scoops being discovered and pass it over to the plot checker.
        /// </summary>
        /// <param name="sender">
        /// The event sender.
        /// </param>
        /// <param name="e">
        /// The event arguments.
        /// </param>
        private void PlotReaderManagerOnScoopsDiscovered(object sender, ScoopsDiscoveredEventArgs e)
        {
            _deadlineCalculator?.Enqueue(e.Scoops);
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
        private void DeadlineCalculatorOnDeadlineFound(object sender, DeadlineFoundEventArgs e)
        {
            _deadlineSubmitter.NewDeadline(new Deadline(TimeSpan.FromSeconds(e.Deadline), e.Scoop, e.MiningInfo));
        }

        /// <summary>
        /// Handle the known size of plots being updated.
        /// </summary>
        /// <param name="sender">
        /// The event sender.
        /// </param>
        /// <param name="e">
        /// The event arguments.
        /// </param>
        private void PlotReaderManagerOnUtilisedStorageUpdated(object sender, UtilisedStorageEventHandler e)
        {
            _deadlineSubmitter.UpdateUtilisedStorage(e.Gigabytes);
        }
        #endregion

        /// <summary>
        /// Start the miner.
        /// </summary>
        public void Run()
        {
            // Ensure we can only run once
            if (_started)
                throw new Exception("Already started");
            _started = true;

            // Start the UI
            _consoleUi = new ConsoleUi();
            Logger.Info("Miner starting up");

            // Create our plot reader manager to manage reading from disk
            Logger.Debug("Plot reader manager: starting");
            _plotReaderManager = new PlotReaderManager();
            _plotReaderManager.ScoopsDiscovered += PlotReaderManagerOnScoopsDiscovered;
            _plotReaderManager.UtilisedStorageUpdated += PlotReaderManagerOnUtilisedStorageUpdated;
            Logger.Debug("Plot reader manager: started");
            
            // Create our deadline calculator and hook up to its events
            Logger.Debug("Deadline calculator: starting");
            _deadlineCalculator = new DeadlineCalculator(Configuration.MemoryLimitPlotChecker / Plot.SCOOP_SIZE, Configuration.ThreadCountPlotChecker);
            _deadlineCalculator.DeadlineFound += DeadlineCalculatorOnDeadlineFound;
            Logger.Debug("Deadline calculator: started");

            // Setup our deadline submitter
            Logger.Debug("Deadline submitter: starting");
            _deadlineSubmitter = new DeadlineSubmitter();
            _deadlineSubmitter.UpdateUtilisedStorage(_plotReaderManager.UtilisedStorage);
            Logger.Debug("Deadline submitter: started");

            // Create our mining info updater to listen for new rounds
            Logger.Debug("Mining info updater: starting");
            _miningInfoUpdater = new MiningInfoUpdater();
            _miningInfoUpdater.PropertyChanged += OnMiningInfoUpdate;
            Logger.Debug("Mining info updater: started");

            // Finally hand over to the console UI which blocks us
            Logger.Info("Miner has started up successfully");
            _consoleUi.Run();
        }

        #region IDisposable Implementation
        /// <summary>
        /// Free up our resources.
        /// </summary>
        public void Dispose()
        {
            if (_miningInfoUpdater != null)
            {
                _miningInfoUpdater.PropertyChanged -= OnMiningInfoUpdate;
                _miningInfoUpdater.Dispose();
                _miningInfoUpdater = null;
            }
            if (_plotReaderManager != null)
            {
                _plotReaderManager.ScoopsDiscovered -= PlotReaderManagerOnScoopsDiscovered;
                _plotReaderManager.Dispose();
                _plotReaderManager = null;
            }
            if (_deadlineCalculator != null)
            {
                _deadlineCalculator.DeadlineFound -= DeadlineCalculatorOnDeadlineFound;
                _deadlineCalculator?.Dispose();
            }
            if (_consoleUi != null)
                _consoleUi.Dispose();
            Logger.Debug("Miner disposed");
        }
        #endregion
    }
}