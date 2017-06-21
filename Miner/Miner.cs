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
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of this class.
        /// </summary>
        public Miner()
        {
        }
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
            _plotReaderManager.ReadPlots(_miningInfoUpdater.MiningInfo);
        }
        #endregion

        /// <summary>
        /// Start the miner.
        /// </summary>
        public void Run()
        {
            if (_started)
                throw new Exception("Already started");
            _started = true;
            _consoleUi = new ConsoleUi();
            Logger.Info("Miner starting up");
            _plotReaderManager = new PlotReaderManager();
            _miningInfoUpdater = new MiningInfoUpdater();
            _miningInfoUpdater.PropertyChanged += OnMiningInfoUpdate;
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
                _plotReaderManager.Dispose();
                _plotReaderManager = null;
            }
            if (_consoleUi != null)
                _consoleUi.Dispose();
            Logger.Debug("Miner disposed");
        }
        #endregion
    }
}