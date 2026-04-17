using System.Collections.Generic;

namespace Cave.Logging;

/// <summary>
/// Abstraction for writing log output to a backend sink. Maintains current state for style and color,
/// and is responsible for formatting and emitting provided log components.
/// </summary>
/// <remarks>
/// Used in <see cref="LogReceiver"/> to write formatted <see cref="LogMessage"/> instances
/// after processing them through a <see cref="LogMessageFormatter"/>.
/// </remarks>
public interface ILogWriter
{
    #region Public Properties

    /// <summary>
    /// Gets a value indicating whether this writer is closed.
    /// When <c>true</c>, further writes should not be accepted.
    /// </summary>
    bool IsClosed { get; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Closes the writer and releases associated resources.
    /// Further calls to <see cref="Write"/> may be ignored or throw an exception.
    /// </summary>
    void Close();

    /// <summary>
    /// Waits for pending writes to complete and flushes buffered data to the sink.
    /// </summary>
    void Flush();

    /// <summary>
    /// Writes log message components to the backend sink.
    /// </summary>
    /// <param name="message">The original log message with metadata (level, timestamp, etc.).</param>
    /// <param name="items">The formatted text components with style and color information.</param>
    void Write(LogMessage message, IEnumerable<ILogText> items);

    #endregion Public Methods
}
