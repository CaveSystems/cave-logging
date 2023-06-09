using System;
using System.Collections.Generic;
using System.Threading;
using Cave.IO;

namespace Cave.Logging;

/// <summary>Provides the abstract log receiver implementation.</summary>
public abstract class LogReceiverBase : ILogReceiver
{
    class MessageQueue : LinkedList<IList<LogMessage>> { }

    #region Private Fields

    readonly IRingBuffer<IList<LogMessage>> ringBuffer = new UncheckedRingBuffer<IList<LogMessage>>();
    Thread? receiverThread;
    volatile int messageQueueCount;
    volatile int currentDelayMsec;
    volatile bool isIdle;
    volatile bool delayWarningSent;

    #endregion Private Fields

    /// <summary>Gets the protected logger instance.</summary>
    protected Logger Log { get; }

    /// <inheritdoc/>
    void ILogReceiver.Start() => StartThread();

    /// <summary>
    /// Starts the async output thread of this receiver.
    /// </summary>
    protected virtual void StartThread()
    {
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

    #region Constructors

    /// <summary>Initializes a new instance of the <see cref="LogReceiver"/> class.</summary>
    protected LogReceiverBase()
    {
        Name = GetType().Name;
        Log = new Logger(GetType());
        Logger.Register(this);
    }

    #endregion Constructors

    #region ILogReceiver Members

    /// <inheritdoc/>
    public TimeSpan CurrentDelay => new TimeSpan(currentDelayMsec * TimeSpan.TicksPerMillisecond);

    /// <inheritdoc/>
    public string Name { get; protected set; }

    #endregion ILogReceiver Members

    #region Overrides

    /// <summary>Finalizes an instance of the <see cref="LogReceiver"/> class.</summary>
    ~LogReceiverBase() => Dispose(false);

    #endregion Overrides

    #region Members

    IList<LogMessage> WaitForMessages(MessageQueue messageQueue)
    {
        while (!Closed)
        {
            //idle mode
            if (messageQueueCount == 0 && ringBuffer.Available == 0)
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

    void MoveMessages(MessageQueue messageQueue)
    {
        if (ringBuffer.Available > 0)
        {
            isIdle = false;
            while (ringBuffer.TryRead(out var list))
            {
                messageQueueCount += list.Count;
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

    bool IsLate(LogMessage message)
    {
        if (LateMessageMilliseconds <= 0) return false;
        currentDelayMsec = (int)(message.Age.Ticks / TimeSpan.TicksPerMillisecond);
        return currentDelayMsec > LateMessageMilliseconds;
    }

    #endregion Members

    #region ILogReceiver Member

    /// <summary>Releases the unmanaged resources used by this instance and optionally releases the managed resources.</summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        Logger.Unregister(this);
        Closed = true;
    }

    /// <inheritdoc />
    public bool Closed { get; set; }

    /// <inheritdoc />
    public bool Idle => isIdle && (ringBuffer.Available == 0);

    /// <inheritdoc />
    public int LateMessageMilliseconds { get; set; } = 10000;

    /// <inheritdoc />
    public int LateMessageThreshold { get; set; } = 1000;

    /// <inheritdoc />
    public LogLevel Level { get; set; } = LogLevel.Information;

    /// <inheritdoc />
    public virtual LogReceiverMode Mode { get; set; } = LogReceiverMode.Continuous;

    /// <inheritdoc />
    public TimeSpan TimeBetweenWarnings { get; set; }

    IRingBuffer<IList<LogMessage>> ILogReceiver.RingBuffer => ringBuffer;

    /// <inheritdoc />
    public virtual void Close() => Dispose();

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion ILogReceiver Member

    /// <inheritdoc />
    public abstract void Write(LogMessage message);
}
