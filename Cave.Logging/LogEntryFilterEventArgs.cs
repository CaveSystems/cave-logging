using System;

namespace Cave.Logging
{
    /// <summary>
    /// Provides event arguments for <see cref="LogEntry"/> filtering.
    /// </summary>
    public class LogEntryFilterEventArgs : EventArgs
    {
        /// <summary>
        /// Gets / sets whether the notification should be displayed or not.
        /// </summary>
        public bool Display { get; set; }

        /// <summary>
        /// Provides access to the notification.
        /// </summary>
        public readonly LogEntry Entry;

        /// <summary>
        /// Creates a new <see cref="LogEntryFilterEventArgs"/>.
        /// </summary>
        /// <param name="logEntry"></param>
        public LogEntryFilterEventArgs(LogEntry logEntry)
        {
            Entry = logEntry;
        }
    }
}
