namespace Cave.Logging;

/// <summary>
/// Provides the log writer base class.
/// </summary>
public abstract class LogWriterBase : ILogWriter
{
    sealed class LogWriterEmpty : LogWriterBase
    {
        public override void ChangeColor(LogColor color) { }
        public override void ChangeStyle(LogStyle style) { }
        public override void NewLine() { }
        public override void Reset() { }
        public override void Write(string text) { }
    }

    /// <inheritdoc/>
    public bool IsClosed { get; private set; }

    /// <summary>
    /// Gets the empty (no-op) log writer instance.
    /// </summary>
    public static LogWriterBase Empty { get; } = new LogWriterEmpty();

    /// <inheritdoc/>
    public abstract void Reset();

    /// <inheritdoc/>
    public abstract void ChangeColor(LogColor color);

    /// <inheritdoc/>
    public abstract void ChangeStyle(LogStyle style);

    /// <inheritdoc/>
    public abstract void Write(string text);

    /// <inheritdoc/>
    public abstract void NewLine();

    /// <inheritdoc/>
    public virtual void Close() => IsClosed = true;

}
