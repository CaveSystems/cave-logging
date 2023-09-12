﻿namespace Cave.Logging;

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

    /// <summary>Writes a text</summary>
    /// <param name="text"></param>
    void Write(ILogText text);

    #endregion Public Methods
}
