using Cave.Console;
using System;

namespace Cave.Logging
{
    /// <summary>
    /// Provides a <see cref="ILogReceiver"/> implementation for sending notifications to <see cref="System.Diagnostics.Debug"/> and
    /// <see cref="System.Diagnostics.Trace"/>
    /// </summary>
    public sealed class LogDebugReceiver : LogReceiver
    {
        /// <summary>
        /// Log to <see cref="System.Diagnostics.Trace"/>. This setting is false by default.
        /// </summary>
#if DEBUG
        public bool LogToTrace = false;
#else
        public bool LogToTrace = false;
#endif

        /// <summary>
        /// Log to <see cref="System.Diagnostics.Debug"/>. This setting is true on debug compiles by default.
        /// </summary>
#if DEBUG
        public bool LogToDebug = true;
#else
        public bool LogToDebug = false;
#endif

        /// <summary>
        /// Do not use string.Format while initializing this class!
        /// </summary>
        internal LogDebugReceiver()
        {
            Mode = LogReceiverMode.Continuous;
            Level = LogLevel.Debug;
        }

        /// <summary>Writes the specified log message.</summary>
        /// <param name="dateTime">The date time.</param>
        /// <param name="level">The level.</param>
        /// <param name="source">The source.</param>
        /// <param name="content">The content.</param>
        protected override void Write(DateTime dateTime, LogLevel level, string source, XT content)
        {
            string text = dateTime.ToLocalTime().ToString(StringExtensions.DisplayDateTimeFormat) + " " + level + " " + source + ": " + content.Text;
            if (LogToDebug)
            {
                LogHelper.DebugLine(text);
            }

            if (LogToTrace)
            {
                LogHelper.TraceLine(text);
            }
        }

        /// <summary>
        /// Obtains the string "LogDebugReceiver[Debug+Tracewriter,Level]"
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "LogDebugReceiver[Debug+Tracewriter," + Level + "]";
        }

        /// <summary>
        /// Obtains the name of the log
        /// </summary>
        public override string LogSourceName => "LogDebugReceiver[Debug+Tracewriter]";
    }
}
