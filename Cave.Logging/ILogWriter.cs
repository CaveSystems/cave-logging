using System.Collections.Generic;

namespace Cave.Logging;

/// <summary>Provides an interface for writing log data. The writer keeps the current state for style and color.</summary>
public interface ILogWriter
{
    #region Public Properties

    /// <summary>Gets a value indicating whether this instance is closed.</summary>
    bool IsClosed { get; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Closes the writer.</summary>
    void Close();

    /// <summary>Writes all buffered data to the sink.</summary>
    void Flush();

    /// <summary>Writes all components of the log message to the backend</summary>
    /// <param name="message">The original message.</param>
    /// <param name="items">The formatted items to write.</param>
    void Write(LogMessage message, IEnumerable<ILogText> items);

    #endregion Public Methods
}
