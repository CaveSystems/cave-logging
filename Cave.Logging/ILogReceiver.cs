using System;
using System.Collections.Generic;
using Cave.IO;

namespace Cave.Logging;

/// <summary>Provides an interface for log receivers.</summary>
public interface ILogReceiver : IDisposable
{
    #region Properties

    /// <summary>Ringbuffer the loggingsystem will drop messages at.</summary>
    /// <remarks>Implement explicit. Should not be called by user.</remarks>
    IRingBuffer<IList<LogMessage>> RingBuffer { get; }

    /// <summary>Gets a value indicating whether the <see cref="ILogReceiver"/> was already closed or not.</summary>
    bool Closed { get; }

    /// <summary>Gets the current delay.</summary>
    TimeSpan CurrentDelay { get; }

    /// <summary>Gets a value indicating whether the receiver is idle or not.</summary>
    bool Idle { get; }

    /// <summary>
    /// Gets or sets the time in milli seconds for detecting late messages. Messages older than this value will result in a warning message to the log system.
    /// Default is 10s = 10000ms.
    /// </summary>
    int LateMessageMilliseconds { get; set; }

    /// <summary>Gets or sets the maximum number of messages allowed to be older than <see cref="LateMessageMilliseconds"/> when using <see cref="LogReceiverMode.Continuous"/>.
    /// Default is 1000.</summary>
    int LateMessageThreshold { get; set; }

    /// <summary>Gets or sets the <see cref="LogLevel"/> currently used. Default is <see cref="LogLevel.Information"/>.</summary>
    LogLevel Level { get; set; }

    /// <summary>
    /// Gets or sets the operation mode of the receiver.
    /// The default for fast loggers is <see cref="LogReceiverMode.Continuous"/>.
    /// </summary>
    /// <remarks>
    /// A system generating messages faster than the receivers can consume them may eat up memory if set to <see cref="LogReceiverMode.Continuous"/>.
    /// <see cref="LogReceiverMode.Opportune"/> allows to the receiver to keep always up with the system by discarding messages older than 
    /// <see cref="LateMessageMilliseconds"/>.
    /// </remarks>
    LogReceiverMode Mode { get; set; }

    /// <summary>Gets the name of the log receiver.</summary>
    string Name { get; }

    /// <summary>Gets or sets the time between two warnings.</summary>
    TimeSpan TimeBetweenWarnings { get; set; }

    #endregion Properties

    #region Members

    /// <summary>Called by the first <see cref="Logger.Register(ILogReceiver)"/> call.</summary>
    /// <remarks>Implement explicit. Should not be called by user.</remarks>
    void Start();

    /// <summary>Closes the <see cref="ILogReceiver"/>.</summary>
    void Close();

    #endregion Members
}
