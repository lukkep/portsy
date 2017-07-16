namespace Portsy.PortQrySupport
{
    /// <summary>
    /// Port status enumeration.
    /// </summary>
    public enum PortStatus
    {
        /// <summary>
        /// Port is in listening state.
        /// </summary>
        LISTENING,

        /// <summary>
        /// Port is not listening.
        /// </summary>
        NOT_LISTENING,

        /// <summary>
        /// Port is filtered.
        /// </summary>
        FILTERED,

        /// <summary>
        /// Error reading data.
        /// </summary>
        ERROR
    }
}
