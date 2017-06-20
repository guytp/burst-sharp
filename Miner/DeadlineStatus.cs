namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// Indicates the state of a deadline.
    /// </summary>
    public enum DeadlineStatus
    {
        /// <summary>
        /// Indicates we have found the deadline in our plots.
        /// </summary>
        Found,

        /// <summary>
        /// Indicates we have submitted the deadline to the pool or network.
        /// </summary>
        Submitted,

        /// <summary>
        /// Indicates a submitted deadline has been accepted.
        /// </summary>
        Accepted,

        /// <summary>
        /// Indicates a submitted deadline has been rejected.
        /// </summary>
        Rejected,

        /// <summary>
        /// Indicates a deadline failed to submit and will be retried.
        /// </summary>
        SubmissionFailure
    }
}