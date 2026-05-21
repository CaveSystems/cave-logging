using System;

namespace Cave.Logging;

/// <summary>Interface for log receivers.</summary>
public interface ILogReceiver : IDisposable
{
    /// <summary>Gets or sets a value indicating whether this log receiver is closed.</summary>
    bool Closed { get; set; }

    /// <summary>Gets the current delay.</summary>
    TimeSpan CurrentDelay { get; }

    /// <summary>Gets the number of discarded messages.</summary>
    int DiscardedMessages { get; }

    /// <summary>Gets a value indicating whether this log receiver is idle.</summary>
    bool Idle { get; }

    /// <summary>Gets or sets the milliseconds after which a message is considered late.</summary>
    int LateMessageMilliseconds { get; set; }

    /// <summary>Gets or sets the threshold for late messages.</summary>
    int LateMessageThreshold { get; set; }

    /// <summary>Gets or sets the log level.</summary>
    LogLevel Level { get; set; }

    /// <summary>Gets or sets the log message formatter.</summary>
    ILogMessageFormatter MessageFormatter { get; set; }

    /// <summary>Gets or sets the log receiver mode.</summary>
    LogReceiverMode Mode { get; set; }

    /// <summary>Gets the name of this log receiver.</summary>
    string Name { get; }

    /// <summary>Gets a value indicating whether this log receiver has started.</summary>
    bool Started { get; }
    
    /// <summary>Gets or sets the time span between warnings.</summary>
    TimeSpan TimeBetweenWarnings { get; set; }

    /// <summary>Gets or sets the log writer.</summary>
    ILogWriter Writer { get; set; }

    /// <summary>Closes this log receiver.</summary>
    void Close();

    /// <summary>Flushes this log receiver.</summary>
    void Flush();

    /// <summary>Starts this log receiver.</summary>
    void Start();

    /// <summary>Writes the specified log message.</summary>
    /// <param name="message">The log message to write.</param>
    void Write(LogMessage message);
}
