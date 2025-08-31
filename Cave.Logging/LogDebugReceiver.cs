using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Cave.Logging;

/// <summary>Provides a <see cref="LogReceiver"/> implementation for sending notifications to <see cref="Debug"/> and <see cref="Trace"/>.</summary>
public sealed class LogDebugReceiver : LogReceiver
{
    #region Private Classes

    sealed class MyWriter : LogWriter
    {
        #region Public Methods

        public override void Flush() { }

        public override void Write(LogMessage message, IEnumerable<ILogText> items)
        {
            StringBuilder buffer = new();
            if (Logger.IncludeDebugInformation && message.SenderSource is not null)
            {
                buffer.Append($"SenderSource:{message.SenderSource} ");
            }
            void Commit()
            {
                if (buffer.Length > 0)
                {
                    var msg = buffer.ToString();
                    LogHelper.DebugLine(msg);
                    LogHelper.TraceLine(msg);
                    buffer = new();
                }
            }
            foreach (var item in items)
            {
                if (item.Equals(LogText.NewLine))
                {
                    Commit();
                    continue;
                }
                buffer.Append(item.Text);
            }
            Commit();
        }

        #endregion Public Methods
    }

    #endregion Private Classes

    #region Internal Constructors

    /// <summary>Do not use string.Format while initializing this class!.</summary>
    internal LogDebugReceiver()
    {
        Mode = LogReceiverMode.Continuous;
        Level = LogLevel.Debug;
        Writer = new MyWriter();
    }

    #endregion Internal Constructors

    #region Public Properties

    /// <summary>Log to <see cref="Debug"/>. This setting is false by default.</summary>
    public bool LogToDebug { get => LogHelper.LogToDebug; set => LogHelper.LogToDebug = value; }

    /// <summary>Log to <see cref="Trace"/>. This setting is false by default.</summary>
    public bool LogToTrace { get => LogHelper.LogToTrace; set => LogHelper.LogToTrace = value; }

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc/>
    public override void Write(LogMessage message)
    {
        if (LogToDebug || LogToTrace)
        {
            base.Write(message);
        }
    }

    #endregion Public Methods
}
