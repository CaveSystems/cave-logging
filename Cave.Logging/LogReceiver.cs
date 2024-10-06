using System;
using System.Collections.Generic;
using System.Threading;
using Cave.IO;

namespace Cave.Logging;

/// <summary>Provides a log receiver implementation with <see cref="ILogMessageFormatter"/> and <see cref="ILogWriter"/>.</summary>
public abstract class LogReceiver : IDisposable
{
    #region Private Classes

    sealed class MessageQueue : LinkedList<IList<LogMessage>> { }

    #endregion Private Classes

    #region Private Fields

    volatile int currentDelayMsec;

    volatile bool delayWarningSent;

    volatile bool isIdle;

    volatile int messageQueueCount;

    Thread? receiverThread;

    #endregion Private Fields

    #region Private Destructors

    /// <summary>Finalizes an instance of the <see cref="LogReceiver"/> class.</summary>
    ~LogReceiver() => Dispose(false);

    #endregion Private Destructors

    #region Private Methods

    bool IsLate(LogMessage message)
    {
        if (LateMessageMilliseconds <= 0) return false;
        currentDelayMsec = (int)(message.Age.Ticks / TimeSpan.TicksPerMillisecond);
        return currentDelayMsec > LateMessageMilliseconds;
    }

    void MoveMessages(MessageQueue messageQueue)
    {
        if (Fifo.Available > 0)
        {
            isIdle = false;
            while (Fifo.TryDequeue(out var list))
            {
                messageQueueCount += list!.Count;
                messageQueue.AddLast(list);
            }
        }
    }

    void ReceiverWorker()
    {
        Log.Verbose($"Thread:{Thread.CurrentThread.Name} started.");
        var nextWarningUtc = DateTime.MinValue;
        var discardedCount = 0;
        var errorCount = 0;
        MessageQueue messageQueue = new();

        while (!Closed)
        {
            try
            {
                var list = WaitForMessages(messageQueue);
                for (var i = 0; i < list.Count; i++)
                {
                    MoveMessages(messageQueue);
                    if (list[i] is not LogMessage message) continue;

                    // is this message late ?
                    if (IsLate(message))
                    {
                        // yes, opportune logging ?
                        if (Mode == LogReceiverMode.Opportune)
                        {
                            // yes -> discard
                            discardedCount++;
                            continue;
                        }

                        // no continuous logging -> shall we warn user?
                        if (LateMessageThreshold <= 0)
                        {
                            //no warning
                        }
                        else if (!delayWarningSent && (messageQueueCount + list.Count > LateMessageThreshold) && (MonotonicTime.UtcNow > nextWarningUtc))
                        {
                            var backlog = messageQueueCount + list.Count;
                            var delay = CurrentDelay.FormatTime();
                            // warn all
                            Log.Warning($"LogReceiver {Name} has a backlog of {backlog} messages (current delay {delay})!");

                            // warn self (direct write)
                            Write(new(Name, GetType(), LogLevel.Warning, $"LogReceiver {Name} has a backlog of {backlog} messages (current delay {delay})!"));

                            // calc next
                            nextWarningUtc = MonotonicTime.UtcNow + TimeBetweenWarnings;
                            delayWarningSent = true;
                        }
                    }

                    if (Closed)
                    {
                        break;
                    }

                    if (message.Level > Level)
                    {
                        continue;
                    }

                    Write(message);
                }

                if (discardedCount > 0)
                {
                    if (MonotonicTime.UtcNow > nextWarningUtc)
                    {
                        Write(new LogMessage(Name, GetType(), LogLevel.Warning, $"LogReceiver {Name} discarded {discardedCount} late messages!"));
                        discardedCount = 0;
                        nextWarningUtc = MonotonicTime.UtcNow + TimeBetweenWarnings;
                    }
                }
                errorCount = 0;
            }
            catch (Exception ex)
            {
                if (errorCount++ > 5)
                {
                    Log.Emergency($"LogReceiver {Name} encountered a fatal exception and is removed!", ex);
                    Close();
                    return;
                }
                Log.Error($"LogReceiver {Name} encountered a exception (retry {errorCount})!", ex);
            }
        }
    }

    IList<LogMessage> WaitForMessages(MessageQueue messageQueue)
    {
        while (!Closed)
        {
            //idle mode
            if (messageQueueCount == 0 && Fifo.Available == 0)
            {
                if (!isIdle)
                {
                    // entering idle mode
                    if (delayWarningSent)
                    {
                        Log.Notice($"LogReceiver {Name} backlog has recovered!");
                        delayWarningSent = false;
                        continue;
                    }
                    isIdle = true;
                }
                Thread.Sleep(1);
                continue;
            }

            //pump
            MoveMessages(messageQueue);

            //handle
            if (messageQueue.Count > 0)
            {
                var list = messageQueue.First!.Value;
                messageQueue.RemoveFirst();
                messageQueueCount -= list.Count;
                return list;
            }
        }
        return new LogMessage[0];
    }

    #endregion Private Methods

    #region Protected Constructors

    /// <summary>Initializes a new instance of the <see cref="LogReceiver"/> class.</summary>
    protected LogReceiver()
    {
        Name = GetType().Name;
        Log = new Logger(GetType());
        Logger.Register(this);
    }

    #endregion Protected Constructors

    #region Protected Properties

    /// <summary>Gets the protected logger instance.</summary>
    protected Logger Log { get; }

    #endregion Protected Properties

    #region Protected Methods

    /// <summary>Starts a <see cref="LogReceiver"/></summary>
    /// <typeparam name="TLogReceiver">The type</typeparam>
    /// <param name="receiver">The receiver instance</param>
    /// <returns>Returns the specified <paramref name="receiver"/>.</returns>
    protected static TLogReceiver Start<TLogReceiver>(TLogReceiver receiver)
        where TLogReceiver : LogReceiver
    {
        receiver.Start();
        return receiver;
    }

    /// <summary>Releases the unmanaged resources used by this instance and optionally releases the managed resources.</summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        Logger.Unregister(this);
        Closed = true;
    }

    #endregion Protected Methods

    #region Internal Properties

    internal Fifo<IList<LogMessage>> Fifo { get; } = new Fifo<IList<LogMessage>>();

    #endregion Internal Properties

    #region Public Properties

    /// <summary>Gets a value indicating whether this instance was already closed or not.</summary>
    public bool Closed { get; set; }

    /// <summary>Gets the current delay.</summary>
    public TimeSpan CurrentDelay => new TimeSpan(currentDelayMsec * TimeSpan.TicksPerMillisecond);

    /// <summary>Gets a value indicating whether the receiver is idle or not.</summary>
    public bool Idle => (isIdle && (Fifo.Available == 0)) || !Started;

    /// <summary>Gets a value indicating whether the receiver is idle or not.</summary>
    public int LateMessageMilliseconds { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the maximum number of messages allowed to be older than <see cref="LateMessageMilliseconds"/> when using <see
    /// cref="LogReceiverMode.Continuous"/>. Default is 1000.
    /// </summary>
    public int LateMessageThreshold { get; set; } = 1000;

    /// <summary>Gets or sets the <see cref="LogLevel"/> currently used. Default is <see cref="LogLevel.Information"/>.</summary>
    public LogLevel Level { get; set; } = LogLevel.Information;

    /// <summary>Provides formatting for log messages.</summary>
    public ILogMessageFormatter MessageFormatter { get; set; } = new LogMessageFormatter();

    /// <summary>Gets or sets the operation mode of the receiver. The default for fast loggers is <see cref="LogReceiverMode.Continuous"/>.</summary>
    /// <remarks>
    /// A system generating messages faster than the receivers can consume them may eat up memory if set to <see cref="LogReceiverMode.Continuous"/>. <see
    /// cref="LogReceiverMode.Opportune"/> allows to the receiver to keep always up with the system by discarding messages older than <see cref="LateMessageMilliseconds"/>.
    /// </remarks>
    public virtual LogReceiverMode Mode { get; set; } = LogReceiverMode.Continuous;

    /// <summary>Gets the name of the log receiver.</summary>
    public string Name { get; protected set; }

    /// <summary>Gets a value indicating whether the receiver was started or not.</summary>
    public bool Started => receiverThread != null;

    /// <summary>Gets or sets the time between two warnings.</summary>
    public TimeSpan TimeBetweenWarnings { get; set; }

    /// <summary>Provides writing to the backend.</summary>
    public ILogWriter Writer { get; set; } = LogWriter.Empty;

    #endregion Public Properties

    #region Public Methods

    /// <summary>Closes this instance.</summary>
    public virtual void Close()
    {
        Writer.Close();
        Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Calls the <see cref="LogWriter.Flush"/> method until <see cref="Idle"/> is true.</summary>
    public virtual void Flush()
    {
        while (!Idle)
        {
            Thread.Sleep(0);
            Writer.Flush();
        }
    }

    /// <summary>Starts the receiver.</summary>
    public virtual void Start()
    {
        if (Started) return;
        if (receiverThread is null)
        {
            receiverThread = new Thread(ReceiverWorker)
            {
                Name = Name,
                IsBackground = true,
                Priority = ThreadPriority.Highest
            };
            receiverThread.Start();
        }
    }

    /// <summary>Writes a message directly to the backend. This is used by the receiver thread and in case of fatal failures by the logger class.</summary>
    /// <remarks>This may be used directly to inject messages without using the logging system.</remarks>
    /// <param name="message">The message to write.</param>
    public virtual void Write(LogMessage message)
    {
        //message filtered ?
        if (message.Level > Level) return;
        var items = MessageFormatter.FormatMessage(message);
        Writer.Write(message, items);
    }

    #endregion Public Methods
}
