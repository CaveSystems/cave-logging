using System;

namespace Cave.Logging;

/// <summary>Provides event data for handling syslog messages.</summary>
public sealed class SyslogMessageEventArgs : EventArgs
{
    #region Public Fields

    /// <summary>Provides the <see cref="SyslogMessage"/>.</summary>
    public readonly SyslogMessage Message;

    #endregion Public Fields

    #region Constructors

    /// <summary>Creates a new <see cref="SyslogMessageEventArgs"/> instance.</summary>
    /// <param name="msg"></param>
    public SyslogMessageEventArgs(SyslogMessage msg) => Message = msg;

    #endregion Constructors
}