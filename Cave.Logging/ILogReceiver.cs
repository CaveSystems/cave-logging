using System;

namespace Cave.Logging
{
    /// <summary>
    /// Provides an interface for log receivers.
    /// </summary>
    public interface ILogReceiver : IDisposable
    {
        /// <summary>
        /// Gets or sets the time between two warnings.
        /// </summary>
        TimeSpan TimeBetweenWarnings { get; set; }

        /// <summary>
        /// Gets or sets the time in milli seconds for detecting late messages. Messages older than this value will result in a warning message to the log system.
        /// </summary>
        int LateMessageMilliSeconds { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of messages allowed to be older than <see cref="LateMessageMilliSeconds"/>.
        /// </summary>
        int LateMessageTreshold { get; set; }

        /// <summary>
        /// Gets or sets the operation mode of the receiver.
        /// </summary>
        LogReceiverMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="LogLevel"/> of the receiver (will not receive notifications with loglevel greater then this).
        /// </summary>
        LogLevel Level { get; set; }

        /// <summary>Gets or sets the exception mode.</summary>
        /// <value>The exception mode.</value>
        LogExceptionMode ExceptionMode { get; set; }

        /// <summary>
        /// Gets or sets the callback function used to transmit the logging notifications.
        /// </summary>
        /// <param name="msg">The message.</param>
        void Write(LogMessage msg);

        /// <summary>
        /// Gets a value indicating whether the <see cref="ILogReceiver"/> was already closed or not.
        /// </summary>
        bool Closed { get; }

        /// <summary>
        /// Closes the <see cref="ILogReceiver"/>.
        /// </summary>
        void Close();
    }
}
