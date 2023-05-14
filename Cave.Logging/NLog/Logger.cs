using System;
using System.Runtime.CompilerServices;
using Cave;
using Cave.Logging;

namespace NLog;

/// <summary>NLog replacement implementation.</summary>
[Obsolete("Replace the 'using NLog;' directive with 'using Cave.Logging;'.")]
public class Logger : Cave.Logging.Logger
{
    #region Constructors

    /// <summary>Initializes a new instance of the <see cref="Logger"/> class.</summary>
    /// <param senderName="senderName">Name of the logger.</param>
    public Logger(string senderName) : base(senderName) { }

    #endregion Constructors

    #region Public Methods

    /// <summary>(0) Transmits a <see cref="Cave.Logging.LogLevel.Emergency"/> message.</summary>
    /// <param senderName="msg">Message to write.</param>
    /// <param senderName="args">The message arguments.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    [Obsolete("Use Emergency() instead!")]
    public void Fatal(LogText msg, params object[] args) => Send(Sender, LogLevel.Emergency, null, msg, args);

    /// <summary>(0) Transmits a <see cref="Cave.Logging.LogLevel.Emergency"/> message.</summary>
    /// <param senderName="msg">Message to write.</param>
    /// <param senderName="ex">Exception to write.</param>
    /// <param senderName="args">The message arguments.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    [Obsolete("Use Emergency() instead!")]
    public void Fatal(Exception ex, LogText msg = null, params object[] args) => Send(Sender, LogLevel.Emergency, ex, msg, args);

    /// <summary>(4) Transmits a <see cref="Cave.Logging.LogLevel.Warning"/> message.</summary>
    /// <param senderName="msg">The message to be logged.</param>
    /// <param senderName="args">The message arguments.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    [Obsolete("Use Warning() instead!")]
    public void Warn(LogText msg, params object[] args) => Send(Sender, LogLevel.Warning, null, msg, args);

    /// <summary>(4) Transmits a <see cref="Cave.Logging.LogLevel.Warning"/> message.</summary>
    /// <param senderName="msg">Message to write.</param>
    /// <param senderName="ex">Exception to write.</param>
    /// <param senderName="args">The message arguments.</param>
    [Obsolete("Use Warning() instead!")]
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Warn(Exception ex, LogText msg, params object[] args) => Send(Sender, LogLevel.Warning, ex, msg, args);

    #endregion Public Methods
}