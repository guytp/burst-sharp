using System;


namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// This class describes a single deadline.
    /// </summary>
    public class Deadline
    {
        #region Properties
        /// <summary>
        /// Gets the status of the deadline.
        /// </summary>
        public DeadlineStatus Status { get; private set; }

        /// <summary>
        /// Gets the duration of the deadline.
        /// </summary>
        public TimeSpan DeadlineDuration { get; private set; }

        /// <summary>
        /// Gets the scoop the dealdine came from.
        /// </summary>
        public Scoop Scoop { get; private set; }

        /// <summary>
        /// Gets the information about the network that this deadline was found matching.
        /// </summary>
        public MiningInfo MiningInfo { get; private set; }

        /// <summary>
        /// Gets the earliest this deadline should be sent to the network.
        /// </summary>
        public DateTime NextSubmissionDate { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of this class.
        /// </summary>
        /// <param name="deadline">
        /// The duration of the deadline.
        /// </param>
        /// <param name="scoop">
        /// The scoop the dealdine came from.
        /// The</param>
        /// <param name="miningInfo">
        /// The information about the network that this deadline was found matching.
        /// </param>
        public Deadline(TimeSpan deadline, Scoop scoop, MiningInfo miningInfo)
        {
            Status = DeadlineStatus.Found;
            DeadlineDuration = deadline;
            Scoop = scoop;
            MiningInfo = miningInfo;
            NextSubmissionDate = DateTime.UtcNow;
        }
        #endregion

        /// <summary>
        /// Marks this deadline as accepted by the network.
        /// </summary>
        public void Accept()
        {
            if (Status != DeadlineStatus.Submitted)
                throw new Exception("Invalid state transition");
            Status = DeadlineStatus.Accepted;
        }

        /// <summary>
        /// Marks this deadline as rejected by the network.
        /// </summary>
        public void Reject()
        {
            if (Status != DeadlineStatus.Submitted)
                throw new Exception("Invalid state transition");
            Status = DeadlineStatus.Rejected;
        }

        /// <summary>
        /// Marks this deadline as being submitted to the network.
        /// </summary>
        public void Submit()
        {
            if (Status != DeadlineStatus.Found)
                throw new Exception("Invalid state transition");
            Status = DeadlineStatus.Submitted;
        }

        /// <summary>
        /// Mark this deadline as having failed a submission.
        /// </summary>
        public void SubmissionFailed()
        {
            if (Status != DeadlineStatus.Submitted && Status != DeadlineStatus.SubmissionFailure)
                throw new Exception("Invalid state transition");
            NextSubmissionDate = DateTime.UtcNow.AddSeconds(5);
            Status = DeadlineStatus.SubmissionFailure;
        }
    }
}