using System;
using System.Diagnostics;

namespace Cave.Logging
{
    /// <summary>Provides a <see cref="ILogReceiver"/> implementation for sending notifications to <see cref="System.Diagnostics.Debug"/> and <see cref="System.Diagnostics.Trace"/>.</summary>
    public sealed class LogDebugReceiver : LogReceiver
    {
        #region Protected Methods

        /// <summary>Writes the specified log message.</summary>
        /// <param name="dateTime">The date time.</param>
        /// <param name="level">The level.</param>
        /// <param name="source">The source.</param>
        /// <param name="content">The content.</param>
        protected override void Write(DateTime dateTime, LogLevel level, string source, XT content)
        {
            var text = dateTime.ToLocalTime().ToString(StringExtensions.DisplayDateTimeFormat) + " " + level + " " + source + ": " + content.Text;
            if (LogToDebug)
            {
                LogHelper.DebugLine(text);
            }

            if (LogToTrace)
            {
                LogHelper.TraceLine(text);
            }
        }

        #endregion Protected Methods

        #region Internal Constructors

        /// <summary>Do not use string.Format while initializing this class!.</summary>
        internal LogDebugReceiver()
        {
            Mode = LogReceiverMode.Continuous;
            Level = LogLevel.Debug;
        }

        #endregion Internal Constructors

        #region Public Fields

        /// <summary>Log to <see cref="Debug"/>. This setting is false by default.</summary>
        public bool LogToDebug;

        /// <summary>Log to <see cref="Trace"/>. This setting is false by default.</summary>
        public bool LogToTrace;

        #endregion Public Fields
    }
}
