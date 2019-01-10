using System;
using System.Diagnostics;

namespace Cave.Logging
{
    /// <summary>
    /// Provides a basic log receiver splitting the public callback into two member callback functions
    /// one for text and one for progress notifications.
    /// </summary>
    public abstract class LogReceiverBase : ILogReceiver
    {
        /// <summary>Finalizes an instance of the <see cref="LogReceiverBase"/> class.</summary>
        ~LogReceiverBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// Creates a new instance of a log receiver and registers it
        /// </summary>
        protected LogReceiverBase()
        {
            TimeBetweenWarnings = TimeSpan.FromSeconds(10);
            LateMessageMilliSeconds = 10000;
            LateMessageTreshold = 1000;
            Level = LogLevel.Information;
            ExceptionMode = Debugger.IsAttached ? LogExceptionMode.Full : LogExceptionMode.IncludeChildren;
            Logger.Register(this);
        }

        #region ILogReceiver Member
        /// <summary>
        /// Gets/sets the time between two warnings.
        /// </summary>
        public TimeSpan TimeBetweenWarnings { get; set; }

        /// <summary>
        /// Gets/sets the time in milli seconds for detecting late messages. Messages older than this value will result in a warning message to the log system.
        /// </summary>
        public int LateMessageMilliSeconds { get; set; }

        /// <summary>
        /// Gets / sets the maximum number of messages allowed to be older than <see cref="LateMessageMilliSeconds"/>
        /// </summary>
        public int LateMessageTreshold { get; set; }

        /// <summary>
        /// Gets / sets the operation mode of the receiver.
        /// </summary>
        public virtual LogReceiverMode Mode { get; set; } = LogReceiverMode.Opportune;

        /// <summary>Provides the callback function used to transmit the logging notifications</summary>
        /// <param name="msg">The message</param>
        public abstract void Write(LogMessage msg);

        /// <summary>
        /// The <see cref="LogLevel"/> currently used.
        /// This defaults to <see cref="LogLevel.Information"/>
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>Gets or sets the exception mode.</summary>
        /// <value>The exception mode.</value>
        public LogExceptionMode ExceptionMode { get; set; }

        /// <summary>
        /// Obtains whether the <see cref="LogReceiver"/> was already closed
        /// </summary>
        public bool Closed { get; private set; }

        /// <summary>
        /// Closes the <see cref="LogReceiver"/>
        /// </summary>
        public virtual void Close()
        {
            Dispose();
        }

        /// <summary>Releases the unmanaged resources used by this instance and optionally releases the managed resources.</summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Closed = true;
                Logger.Unregister(this);
            }
        }

        /// <summary>
        /// Releases all resources used by the this instance
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Obtains the name of the log
        /// </summary>
        public abstract string LogSourceName { get; }

        #endregion

        /// <summary>
        /// Obtains a string representing this instance
        /// </summary>
        public override string ToString()
        {
            return LogSourceName;
        }
    }
}