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
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of this class.
        /// </summary>
        public Miner()
        {
            Logger.Info("Miner starting up");
            _plotReaderManager = new PlotReaderManager();
            _miningInfoUpdater = new MiningInfoUpdater();
            _miningInfoUpdater.PropertyChanged += OnMiningInfoUpdate;
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
            Logger.Info("Miner disposed");
        }
        #endregion
    }
}