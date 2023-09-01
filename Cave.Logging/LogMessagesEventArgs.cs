using System;
using System.Collections.Generic;

namespace Cave.Logging;

/// <summary>Provides event argument for log message handling events</summary>
public class LogMessagesEventArgs : EventArgs
{
    #region Public Constructors

    /// <summary>Creates a new instance of the <see cref="LogMessageEventArgs"/> class.</summary>
    /// <param name="messages">The message to handle.</param>
    public LogMessagesEventArgs(IEnumerable<LogMessage> messages) => Messages = messages;

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets the message.</summary>
    public IEnumerable<LogMessage> Messages { get; }

    #endregion Public Properties
}
