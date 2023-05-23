using System.Diagnostics;
using System.Text;

namespace Cave.Logging;

/// <summary>Provides a <see cref="ILogReceiver"/> implementation for sending notifications to <see cref="System.Diagnostics.Debug"/> and <see cref="System.Diagnostics.Trace"/>.</summary>
public sealed class LogDebugReceiver : LogReceiver
{
    class MyWriter : LogWriterBase
    {
        StringBuilder buffer = new();

        public override void ChangeColor(LogColor color) { }
        public override void ChangeStyle(LogStyle style) { }
        public override void NewLine()
        {
            var msg = buffer.ToString();
            LogHelper.DebugLine(msg);
            LogHelper.TraceLine(msg);
            buffer = new();
        }
        public override void Reset() { }
        public override void Write(string text) => buffer.Append(text);
    }

    #region Internal Constructors

    /// <summary>Do not use string.Format while initializing this class!.</summary>
    internal LogDebugReceiver()
    {
        Mode = LogReceiverMode.Continuous;
        Level = LogLevel.Debug;
        Writer = new MyWriter();
    }

    #endregion Internal Constructors

    /// <inheritdoc />
    public override void Write(LogMessage message)
    {
        if (LogToDebug || LogToTrace)
        {
            base.Write(message);
        }
    }

    #region Public Fields

    /// <summary>Log to <see cref="Debug"/>. This setting is false by default.</summary>
    public bool LogToDebug { get => LogHelper.LogToDebug; set => LogHelper.LogToDebug = value; }

    /// <summary>Log to <see cref="Trace"/>. This setting is false by default.</summary>
    public bool LogToTrace { get => LogHelper.LogToTrace; set => LogHelper.LogToTrace = value; }

    #endregion Public Fields
}
