namespace Cave.Logging
{
    /// <summary>
    /// Provides available syslog message versions
    /// </summary>
    public enum SyslogMessageVersion : int
    {
        /// <summary>
        /// Undefined syslog message version (usually RFC3164)
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Standard syslog message RFC 3164
        /// </summary>
        RFC3164 = 1,

        /// <summary>
        /// Extended syslog message RFC 5424
        /// </summary>
        RFC5424 = 2,

        /// <summary>
        /// rsyslog format (needed for extended messages since rsyslog is not capable of handling RFC 5424 correctly)
        /// </summary>
        RSYSLOG = 3,
    }
}
