using System;
using System.Collections.Generic;

namespace Cave.Logging
{
    /// <summary>Provides an interface for log receivers.</summary>
    public interface ILogReceiver : IDisposable
    {
        #region Properties

        /// <summary>Gets a value indicating whether the <see cref="ILogReceiver"/> was already closed or not.</summary>
        bool Closed { get; }

        /// <summary>Gets the current delay.</summary>
        TimeSpan CurrentDelay { get; }

        /// <summary>Gets or sets the exception mode.</summary>
        /// <value>The exception mode.</value>
        LogExceptionMode ExceptionMode { get; set; }

        /// <summary>Gets a value indicating whether the receiver is idle or not.</summary>
        bool Idle { get; }

        /// <summary>
        /// Gets or sets the time in milliseconds for detecting late messages. Messages older than this value will result in a warning message to the log system.
        /// </summary>
        int LateMessageMilliseconds { get; set; }

        /// <summary>Gets or sets the maximum number of messages allowed to be older than <see cref="LateMessageMilliseconds"/>.</summary>
        int LateMessageThreshold { get; set; }

        /// <summary>Gets or sets the <see cref="LogLevel"/> of the receiver (will not receive notifications with log level greater then this).</summary>
        LogLevel Level { get; set; }

        /// <summary>Gets or sets the operation mode of the receiver.</summary>
        LogReceiverMode Mode { get; set; }

        /// <summary>Gets the name of the log receiver.</summary>
        string Name { get; }

        /// <summary>Gets or sets the time between two warnings.</summary>
        TimeSpan TimeBetweenWarnings { get; set; }

        #endregion Properties

        #region Members

        /// <summary>Adds messages to the receiver.</summary>
        /// <param name="messages">The messages to add.</param>
        void AddMessages(IEnumerable<LogMessage> messages);

        /// <summary>Closes the <see cref="ILogReceiver"/>.</summary>
        void Close();

        #endregion Members
    }
}
