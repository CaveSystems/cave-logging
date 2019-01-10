using Cave.Console;
using System;

namespace Cave.Logging
{
    /// <summary>
    /// Provides log extensions
    /// </summary>
    public static class LogExtension
    {
        static string GetLogSourceName(object source)
        {
            if (source == null)
            {
                return "null";
            }

            if (source is ILogSource logSource)
            {
                string name = logSource.LogSourceName;
                if (name != null)
                {
                    return name;
                }
            }
            return "type(" + source.GetType().Name + ")";
        }

        #region ILogSource extension methods

        /// <summary>
        /// Obtains the color of a specified loglevel
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static XTColor GetLogLevelColor(this LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Emergency:
                case LogLevel.Alert:
                case LogLevel.Critical:
                case LogLevel.Error:
                    return XTColor.Red;
                case LogLevel.Warning:
                    return XTColor.Yellow;
                case LogLevel.Notice:
                    return XTColor.Green;
                case LogLevel.Information:
                    return XTColor.Cyan;
                case LogLevel.Debug:
                    return XTColor.Default;
                default:
                    return XTColor.Blue;
            }
        }

        /// <summary>Transmits a log message</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="level">Loglevel of the message</param>
        /// <param name="msg">The message to be logged</param>
        /// <param name="args">The message arguments.</param>
        public static void Log(this object source, LogLevel level, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), level, null, msg);
        }

        /// <summary>Transmits a log message</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="level">Loglevel of the message</param>
        /// <param name="msg">The message to be logged</param>
        /// <param name="ex">Exception to write</param>
        /// <param name="args">The message arguments.</param>
        public static void Log(this object source, LogLevel level, Exception ex, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), level, ex, msg);
        }

        #region log message
        /// <summary>(8) Transmits a <see cref="LogLevel.Verbose" /> message</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">The message to be logged</param>
        /// <param name="args">The message arguments.</param>
        public static void LogVerbose(this object source, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), LogLevel.Verbose, null, msg, args);
        }

        /// <summary>
        /// (7) Transmits a <see cref="LogLevel.Debug"/> message
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">The message to be logged</param>
        /// <param name="args">The message arguments.</param>
        public static void LogDebug(this object source, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), LogLevel.Debug, null, msg, args);
        }

        /// <summary>
        /// (6) Transmits a <see cref="LogLevel.Information"/> message
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">The message to be logged</param>
        /// <param name="args">The message arguments.</param>
        public static void LogInfo(this object source, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), LogLevel.Information, null, msg, args);
        }

        /// <summary>
        /// (5) Transmits a <see cref="LogLevel.Notice"/> message
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">The message to be logged</param>
        /// <param name="args">The message arguments.</param>
        public static void LogNotice(this object source, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), LogLevel.Notice, null, msg, args);
        }

        /// <summary>
        /// (4) Transmits a <see cref="LogLevel.Warning"/> message
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">The message to be logged</param>
        /// <param name="args">The message arguments.</param>
        public static void LogWarning(this object source, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), LogLevel.Warning, null, msg, args);
        }

        /// <summary>
        /// (3) Transmits a <see cref="LogLevel.Error"/> message
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write</param>
        /// <param name="args">The message arguments.</param>
        public static void LogError(this object source, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), LogLevel.Error, null, msg, args);
        }

        /// <summary>
        /// (2) Transmits a <see cref="LogLevel.Critical"/> message
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write</param>
        /// <param name="args">The message arguments.</param>
        public static void LogCritical(this object source, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), LogLevel.Critical, null, msg, args);
        }

        /// <summary>
        /// (1) Transmits a <see cref="LogLevel.Alert"/> message
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write</param>
        /// <param name="args">The message arguments.</param>
        public static void LogAlert(this object source, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), LogLevel.Alert, null, msg, args);
        }

        /// <summary>
        /// (0) Transmits a <see cref="LogLevel.Emergency"/> message
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write</param>
        /// <param name="args">The message arguments.</param>
        public static void LogEmergency(this object source, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), LogLevel.Emergency, null, msg, args);
        }
        #endregion

        #region log exception and message
        /// <summary>
        /// (8) Transmits a <see cref="LogLevel.Verbose"/> message
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="ex">Exception that caused the error. This max be logged with a different loglevel based on the receiver settings.</param>
        /// <param name="msg">Error message describing what was tried and went wrong when the exception occured</param>
        /// <param name="args">The message arguments.</param>
        public static void LogVerbose(this object source, Exception ex, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), LogLevel.Verbose, ex, msg, args);
        }

        /// <summary>
        /// (7) Transmits a <see cref="LogLevel.Debug"/> message
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="ex">Exception that caused the error. This max be logged with a different loglevel based on the receiver settings.</param>
        /// <param name="msg">Error message describing what was tried and went wrong when the exception occured</param>
        /// <param name="args">The message arguments.</param>
        public static void LogDebug(this object source, Exception ex, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), LogLevel.Debug, ex, msg, args);
        }

        /// <summary>
        /// (6) Transmits a <see cref="LogLevel.Information"/> message
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="ex">Exception that caused the error. This max be logged with a different loglevel based on the receiver settings.</param>
        /// <param name="msg">Error message describing what was tried and went wrong when the exception occured</param>
        /// <param name="args">The message arguments.</param>
        public static void LogInfo(this object source, Exception ex, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), LogLevel.Information, ex, msg, args);
        }

        /// <summary>
        /// (5) Transmits a <see cref="LogLevel.Notice"/> message
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="ex">Exception that caused the error. This max be logged with a different loglevel based on the receiver settings.</param>
        /// <param name="msg">Error message describing what was tried and went wrong when the exception occured</param>
        /// <param name="args">The message arguments.</param>
        public static void LogNotice(this object source, Exception ex, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), LogLevel.Notice, ex, msg, args);
        }

        /// <summary>
        /// (4) Transmits a <see cref="LogLevel.Warning"/> message
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="ex">Exception that caused the error. This max be logged with a different loglevel based on the receiver settings.</param>
        /// <param name="msg">Error message describing what was tried and went wrong when the exception occured</param>
        /// <param name="args">The message arguments.</param>
        public static void LogWarning(this object source, Exception ex, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), LogLevel.Warning, ex, msg, args);
        }

        /// <summary>
        /// (3) Transmits a <see cref="LogLevel.Error"/> message
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="ex">Exception that caused the error. This max be logged with a different loglevel based on the receiver settings.</param>
        /// <param name="msg">Error message describing what was tried and went wrong when the exception occured</param>
        /// <param name="args">The message arguments.</param>
        public static void LogError(this object source, Exception ex, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), LogLevel.Error, ex, msg, args);
        }

        /// <summary>
        /// (2) Transmits a <see cref="LogLevel.Critical"/> message
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="ex">Exception that caused the error. This max be logged with a different loglevel based on the receiver settings.</param>
        /// <param name="msg">Error message describing what was tried and went wrong when the exception occured</param>
        /// <param name="args">The message arguments.</param>
        public static void LogCritical(this object source, Exception ex, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), LogLevel.Critical, ex, msg, args);
        }

        /// <summary>
        /// (1) Transmits a <see cref="LogLevel.Alert"/> message
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="ex">Exception that caused the error. This max be logged with a different loglevel based on the receiver settings.</param>
        /// <param name="msg">Error message describing what was tried and went wrong when the exception occured</param>
        /// <param name="args">The message arguments.</param>
        public static void LogAlert(this object source, Exception ex, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), LogLevel.Alert, ex, msg, args);
        }

        /// <summary>
        /// (0) Transmits a <see cref="LogLevel.Emergency"/> message
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="ex">Exception that caused the error. This max be logged with a different loglevel based on the receiver settings.</param>
        /// <param name="msg">Error message describing what was tried and went wrong when the exception occured</param>
        /// <param name="args">The message arguments.</param>
        public static void LogEmergency(this object source, Exception ex, XT msg, params object[] args)
        {
            Logger.Send(GetLogSourceName(source), LogLevel.Emergency, ex, msg, args);
        }
        #endregion
        #endregion
    }
}