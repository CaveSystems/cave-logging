using System;

namespace Cave.Logging;

/// <summary>Provides event argument for log message handling events</summary>
public class LogMessageEventArgs : EventArgs
{
    #region Public Constructors

    /// <summary>Creates a new instance of the <see cref="LogMessageEventArgs"/> class.</summary>
    /// <param name="message">The message to handle.</param>
    public LogMessageEventArgs(LogMessage message) => Message = message;

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets or sets a value indicating whether the message has been handles by the event or not. Set this to true to surpress the default handling by the
    /// class invoking the event.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>Gets the message.</summary>
    public LogMessage Message { get; }

    #endregion Public Properties
}