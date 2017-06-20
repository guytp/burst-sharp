using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// This class deals with a queue-based system to submit deadlines that are discovered to the backend.
    /// </summary>
    public class DeadlineSubmitter : IDisposable
    {
        #region Declarations
        /// <summary>
        /// Defines an object used for thread-safe access to our queue.
        /// </summary>
        private readonly object _deadlineQueueLocker = new object();

        /// <summary>
        /// Defines a list of deadlines we need to submit.
        /// </summary>
        private readonly List<Deadline> _deadlineQueue = new List<Deadline>();

        /// <summary>
        /// Defines whether or not the deadline submitter is alive and active.
        /// </summary>
        private bool _isAlive;

        /// <summary>
        /// Defines the thread that actually submits deadlines.
        /// </summary>
        private Thread _thread;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of this class.
        /// </summary>
        public DeadlineSubmitter()
        {
            _isAlive = true;
            _thread = new Thread(ThreadEntry) { IsBackground = true, Name = "Deadline Submitter" };
            _thread.Start();
        }
        #endregion

        /// <summary>
        /// This method provides the main logic that manages state transitions and deals with our queue.
        /// </summary>
        private void ThreadEntry()
        {
            while (_isAlive)
            {
                // First let's get a handle to our deadlines
                Deadline[] deadlines;
                lock (_deadlineQueueLocker)
                {
                    // Remove any old deadlines
                    List<Deadline> toRemove = new List<Deadline>();
                    toRemove.AddRange(_deadlineQueue.Where(dl => dl.Status == DeadlineStatus.Accepted || dl.Status == DeadlineStatus.Rejected));
                    foreach (Deadline dl in toRemove)
                        _deadlineQueue.Remove(dl);

                    // Copy the list locally
                    deadlines = _deadlineQueue.Where(dl => dl.NextSubmissionDate <= DateTime.UtcNow).ToArray();
                }

                // Now for each deadline let's submit it to the network
                // TODO: Submit to network
                // TODO: Update console UI

                // Wait to retry
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Reset the deadline submitter and remvoe any queued deadlines that are for old blocks.
        /// </summary>
        /// <param name="miningInfo">
        /// The current blocks mining information.
        /// </param>
        public void Reset(MiningInfo miningInfo)
        {
            // Reset our deadlines list
            lock (_deadlineQueueLocker)
                _deadlineQueue.Clear();
        }

        /// <summary>
        /// Notify the submitter that a new deadline has been found and add it to the queue.
        /// </summary>
        /// <param name="deadline">
        /// The deadline to add.
        /// </param>
        public void NewDeadline(Deadline deadline)
        {
            // Let's log this and notify the UI
            Logger.Info("New deadline found " + deadline.DeadlineDuration + " for block " + deadline.MiningInfo.BlockHeight);
            ConsoleUi.AddDeadlineDetails(deadline);

            // Add this to our queue
            lock (_deadlineQueueLocker)
                _deadlineQueue.Add(deadline);
        }

        #region IDisposable Implementation
        /// <summary>
        /// Free up our used resources;
        /// </summary>
        public void Dispose()
        {
            _isAlive = false;
            if (_thread != null)
            {
                Logger.Info("Stopping deadline submitter");
                _thread?.Join();
                _thread = null;
            }
        }
        #endregion
    }
}