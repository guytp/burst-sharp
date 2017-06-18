using System;
using Guytp.BurstSharp.Plotter;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// The plot reader manager is responsible for performing common mining functionality (i.e. scoop determination) before passing it over to individual plot readers to actually mine the data on disk.
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
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of this class.
        /// </summary>
        public PlotReaderManager()
        {
            if (Configuration.PlotDirectories != null && Configuration.PlotDirectories.Length > 0)
            {
                _plotReaders = new PlotReader[Configuration.PlotDirectories.Length];
                for (int i = 0; i < Configuration.PlotDirectories.Length; i++)
                    _plotReaders[i] = new PlotReader(Configuration.PlotDirectories[i]);
            }
            _shabel = new Shabal256();
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
                    reader.Dispose();
                _plotReaders = null;
            }
        }
        #endregion
    }
}