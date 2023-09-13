using System.Collections.Generic;

namespace Cave.Logging;

/// <summary>Provides the log writer base class.</summary>
public abstract class LogWriter : ILogWriter
{
    #region Private Classes

    sealed class LogWriterEmpty : LogWriter
    {
        #region Public Methods

        public override void Write(LogMessage message, IEnumerable<ILogText> items) { }

        #endregion Public Methods
    }

    #endregion Private Classes

    #region Public Properties

    /// <summary>Gets the empty (no-op) log writer instance.</summary>
    public static ILogWriter Empty { get; } = new LogWriterEmpty();

    /// <inheritdoc/>
    public bool IsClosed { get; private set; }

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc/>
    public virtual void Close() => IsClosed = true;

    /// <inheritdoc/>
    public abstract void Write(LogMessage message, IEnumerable<ILogText> items);

    #endregion Public Methods
}
