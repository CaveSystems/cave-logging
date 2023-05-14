﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Cave.Logging;

/// <summary>Provides the abstract log receiver implementation.</summary>
public abstract class LogReceiver : ILogReceiver
{
    #region Private Fields

    readonly Logger log;
    readonly Thread outputThread;
    int currentDelayMsec;
    bool isIdle;
    LinkedList<LogMessage> messageQueue = new();

    #endregion Private Fields

    #region Constructors

    /// <summary>Initializes a new instance of the <see cref="LogReceiver"/> class.</summary>
    protected LogReceiver()
    {
        Name = GetType().Name;
        TimeBetweenWarnings = TimeSpan.FromSeconds(10);
        LateMessageMilliseconds = 10000;
        LateMessageThreshold = 1000;
        Level = LogLevel.Information;
        ExceptionMode = Debugger.IsAttached ? LogExceptionMode.Full : LogExceptionMode.IncludeChildren;
        log = new Logger($"LogReceiverThread: {Name}");
        outputThread = new Thread(OutputWorker)
        {
            Name = Name,
            IsBackground = true,
            Priority = ThreadPriority.Highest
        };
        outputThread.Start();
        Logger.Register(this);
    }

    #endregion Constructors

    #region ILogReceiver Members

    /// <inheritdoc/>
    public TimeSpan CurrentDelay => new(currentDelayMsec * TimeSpan.TicksPerMillisecond);

    /// <inheritdoc/>
    public string Name { get; protected set; }

    /// <inheritdoc/>
    public void AddMessages(IEnumerable<LogMessage> messages)
    {
        if (messages == null) throw new ArgumentNullException(nameof(messages));
        if (!Monitor.TryEnter(this, 1000))
        {
            var logger = new Logger(GetType());
            logger.Emergency("Deadlock of logger worker queue {0} detected. Disabling receiver!", Name);
            Close();
            return;
        }

        foreach (var msg in messages)
        {
            if (msg != null)
            {
                messageQueue.AddLast(msg);
            }
        }

        Monitor.Pulse(this);
        Monitor.Exit(this);
    }

    #endregion ILogReceiver Members

    #region Overrides

    /// <summary>Finalizes an instance of the <see cref="LogReceiver"/> class.</summary>
    ~LogReceiver() => Dispose(false);

    #endregion Overrides

    #region Members

    void OutputWorker()
    {
        var delayWarningSent = false;
        var nextWarningUtc = DateTime.MinValue;
        var discardedCount = 0;
        var errorCount = 0;
        while (!Closed)
        {
            LinkedList<LogMessage> queue = null;

            try
            {
                // wait for messages
                lock (this)
                {
                    while (true)
                    {
                        if (messageQueue.Count > 0)
                        {
                            queue = messageQueue;
                            messageQueue = new();
                            break;
                        }

                        // entering idle mode
                        if (delayWarningSent)
                        {
                            log.Notice("LogReceiver {0} backlog has recovered!", Name);
                            delayWarningSent = false;
                            continue;
                        }

                        isIdle = true;

                        // wait for pulse
                        while (true)
                        {
                            Monitor.Wait(this, 1000);
                            if (Closed)
                            {
                                return;
                            }

                            break;
                        }

                        isIdle = false;
                    }
                }

                while (queue.Count > 0)
                {
                    var msg = queue.First.Value;
                    queue.RemoveFirst();
                    var delayTicks = (DateTime.UtcNow - msg.DateTime.ToUniversalTime()).Ticks;
                    currentDelayMsec = (int)(delayTicks / TimeSpan.TicksPerMillisecond);

                    // do we have late messages ?
                    if (currentDelayMsec > LateMessageMilliseconds)
                    {
                        // yes, opportune logging ?
                        if (Mode == LogReceiverMode.Opportune)
                        {
                            // discard old notifications
                            if ((delayTicks / TimeSpan.TicksPerMillisecond) > LateMessageMilliseconds)
                            {
                                discardedCount++;
                                continue;
                            }
                        }
                        else
                        {
                            // no continuous logging -> warn user
                            if ((queue.Count > LateMessageThreshold) && (DateTime.UtcNow > nextWarningUtc))
                            {
                                var warning = $"LogReceiver {Name} has a backlog of {queue.Count} messages (current delay {TimeSpan.FromMilliseconds(currentDelayMsec).FormatTime()})!";

                                // warn all
                                log.Warning(warning);

                                // warn self (direct write)
                                Write(new(Name, DateTime.Now, LogLevel.Warning, null, warning, null));

                                // calc next
                                nextWarningUtc = DateTime.UtcNow + TimeBetweenWarnings;
                                delayWarningSent = true;
                            }
                        }
                    }

                    if (Closed)
                    {
                        break;
                    }

                    if (msg.Level > Level)
                    {
                        continue;
                    }

                    Write(msg);
                }

                if (discardedCount > 0)
                {
                    if (DateTime.UtcNow > nextWarningUtc)
                    {
                        var warning = $"LogReceiver {Name} discarded {discardedCount} late messages!";
                        Write(new LogMessage(Name, DateTime.Now, LogLevel.Warning, null, warning, null));
                        discardedCount = 0;
                        nextWarningUtc = DateTime.UtcNow + TimeBetweenWarnings;
                    }
                }
                errorCount = 0;
            }
            catch (Exception ex)
            {
                discardedCount += queue.Count + 1;
                log.Emergency(ex, "LogReceiver {0} encountered a fatal exception!", Name);
                if (errorCount++ > 5)
                {
                    Close();
                    return;
                }
            }
        }
    }

    #endregion Members

    #region ILogReceiver Member

    /// <summary>Releases the unmanaged resources used by this instance and optionally releases the managed resources.</summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Closed = true;
            Logger.Unregister(this);
        }
    }

    /// <summary>Gets a value indicating whether the <see cref="LogReceiver"/> was already closed.</summary>
    public bool Closed { get; set; }

    /// <summary>Gets or sets the exception mode.</summary>
    /// <value>The exception mode.</value>
    public LogExceptionMode ExceptionMode { get; set; }

    /// <summary>Gets a value indicating whether the receiver is idle or not.</summary>
    public bool Idle
    {
        get
        {
            lock (this) { return isIdle; }
        }
    }

    /// <summary>
    /// Gets or sets the time in milli seconds for detecting late messages. Messages older than this value will result in a warning message to the log system.
    /// </summary>
    public int LateMessageMilliseconds { get; set; }

    /// <summary>Gets or sets the maximum number of messages allowed to be older than <see cref="LateMessageMilliseconds"/>.</summary>
    public int LateMessageThreshold { get; set; }

    /// <summary>Gets or sets the <see cref="LogLevel"/> currently used. This defaults to <see cref="LogLevel.Information"/>.</summary>
    public LogLevel Level { get; set; }

    /// <summary>Gets or sets the operation mode of the receiver.</summary>
    public virtual LogReceiverMode Mode { get; set; } = LogReceiverMode.Opportune;

    /// <summary>Gets or sets the time between two warnings.</summary>
    public TimeSpan TimeBetweenWarnings { get; set; }

    /// <summary>Closes the <see cref="LogReceiver"/>.</summary>
    public virtual void Close() => Dispose();

    /// <summary>Releases all resources used by the this instance.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Provides the callback function used to transmit the logging notifications.</summary>
    /// <param name="msg">The message.</param>
    public virtual void Write(LogMessage msg)
    {
        if (msg.Level > Level)
        {
            return;
        }

        if ((msg.Exception == null) || (ExceptionMode == 0))
        {
            Write(msg.DateTime, msg.Level, msg.Source, msg.Content);
            return;
        }

        // log stacktrace
        var stackTrace = (ExceptionMode & LogExceptionMode.StackTrace) != 0;
        var exceptionMessage = msg.Exception.ToLogText(stackTrace);

        // with same level ?
        if ((ExceptionMode & LogExceptionMode.SameLevel) != 0)
        {
            Write(msg.DateTime, msg.Level, msg.Source, msg.Content + new LogText("\n") + exceptionMessage);
        }
        else
        {
            // two different messages
            Write(msg.DateTime, msg.Level, msg.Source, msg.Content);
            Write(msg.DateTime, LogLevel.Verbose, msg.Source, exceptionMessage);
        }
    }

    #endregion ILogReceiver Member
}
