using System;

namespace Cave.Logging
{
    /// <summary>
    /// Provides a basic log receiver splitting the public callback into two member callback functions
    /// one for text and one for progress notifications.
    /// </summary>
    public abstract class LogReceiver : LogReceiverBase
    {
        /// <summary>Writes the specified log message.</summary>
        /// <param name="dateTime">The date time.</param>
        /// <param name="level">The level.</param>
        /// <param name="source">The source.</param>
        /// <param name="content">The content.</param>
        protected abstract void Write(DateTime dateTime, LogLevel level, string source, XT content);

        /// <summary>Provides the callback function used to transmit the logging notifications.</summary>
        /// <param name="msg">The message.</param>
        public override void Write(LogMessage msg)
        {
            if (msg.Level > Level)
            {
                return;
            }

            if (msg.Exception == null || ExceptionMode == 0)
            {
                Write(msg.DateTime, msg.Level, msg.Source, msg.Content);
                return;
            }

            // log stacktrace
            bool stackTrace = (ExceptionMode & LogExceptionMode.StackTrace) != 0;
            var exceptionMessage = msg.Exception.ToXT(stackTrace);

            // with same level ?
            if ((ExceptionMode & LogExceptionMode.SameLevel) != 0)
            {
                Write(msg.DateTime, msg.Level, msg.Source, msg.Content + new XT("\n") + exceptionMessage);
            }
            else
            {
                // two different messages
                Write(msg.DateTime, msg.Level, msg.Source, msg.Content);
                Write(msg.DateTime, LogLevel.Verbose, msg.Source, exceptionMessage);
            }
        }
    }
}
