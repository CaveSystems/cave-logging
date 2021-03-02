using System;
using System.Runtime.CompilerServices;
using Cave;
using Cave.Logging;

namespace NLog
{
    [Obsolete("Replace the 'using NLog;' directive with 'using Cave.Logging;'.")]
    public class Logger : Cave.Logging.Logger
    {
        #region Constructors

        public Logger(string name) : base(name) { }

        #endregion

        #region Public Methods

        /// <summary>(0) Transmits a <see cref="LogLevel.Emergency" /> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        [Obsolete("Use Emergency() instead!")]
        public void Fatal(XT msg, params object[] args) => Send(SourceName, LogLevel.Emergency, null, msg, args);

        /// <summary>(0) Transmits a <see cref="LogLevel.Emergency" /> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        [Obsolete("Use Emergency() instead!")]
        public void Fatal(Exception ex, XT msg = null, params object[] args) => Send(SourceName, LogLevel.Emergency, ex, msg, args);

        /// <summary>(4) Transmits a <see cref="LogLevel.Warning" /> message.</summary>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        [Obsolete("Use Warning() instead!")]
        public void Warn(XT msg, params object[] args) => Send(SourceName, LogLevel.Warning, null, msg, args);

        /// <summary>(4) Transmits a <see cref="LogLevel.Warning" /> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [Obsolete("Use Warning() instead!")]
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Warn(Exception ex, XT msg, params object[] args) => Send(SourceName, LogLevel.Warning, ex, msg, args);

        #endregion Public Methods
    }
}
