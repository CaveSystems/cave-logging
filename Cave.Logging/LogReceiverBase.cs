using System;
using System.Collections.Generic;
using System.Threading;

namespace Cave.Logging;

/// <summary>Provides the abstract log receiver implementation.</summary>
public abstract class LogReceiverBase : ILogReceiver
{
    #region Private Fields

    Thread? outputThread;
    bool isIdle;
    LinkedList<IList<LogMessage>> messageQueue = new();
    volatile int messageQueueCount;
    volatile int currentDelayMsec;

    #endregion Private Fields

    protected Logger Log { get; }

    /// <inheritdoc/>
    void ILogReceiver.Start() => StartThread();

    /// <summary>
    /// Starts the async output thread of this receiver.
    /// </summary>
    protected virtual void StartThread()
    {
        if (outputThread is null)
        {
            outputThread = new Thread(OutputWorker)
            {
                Name = Name,
                IsBackground = true,
                Priority = ThreadPriority.Highest
            };
            outputThread.Start();
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

    /// <inheritdoc/>
    void ILogReceiver.AddMessages(IList<LogMessage> messages)
    {
        if (messages == null) throw new ArgumentNullException(nameof(messages));
        if (!Monitor.TryEnter(SyncRoot, 1000))
        {
            var logger = new Logger(GetType());
            logger.Emergency($"Deadlock of logger worker queue {Name} detected. Disabling receiver!");
            Close();
            return;
        }

        messageQueue.AddLast(messages);
        messageQueueCount += messageQueue.Count;
        Monitor.Pulse(SyncRoot);
        Monitor.Exit(SyncRoot);
    }

    #endregion ILogReceiver Members

    #region Overrides

    /// <summary>Finalizes an instance of the <see cref="LogReceiver"/> class.</summary>
    ~LogReceiverBase() => Dispose(false);

    #endregion Overrides

    #region Members

    void OutputWorker()
    {
        Log.Verbose($"Thread:{Thread.CurrentThread.Name} started.");
        var delayWarningSent = false;
        var nextWarningUtc = DateTime.MinValue;
        var discardedCount = 0;
        var errorCount = 0;
        while (!Closed)
        {
            IList<LogMessage>? list = null;

            try
            {
                // wait for messages
                lock (SyncRoot)
                {
                    while (true)
                    {
                        if (messageQueue.Count > 0)
                        {
                            list = messageQueue.First!.Value;
                            messageQueue.RemoveFirst();
                            messageQueueCount -= list.Count;
                            break;
                        }

                        // entering idle mode
                        if (delayWarningSent)
                        {
                            Log.Notice($"LogReceiver {Name} backlog has recovered!");
                            delayWarningSent = false;
                            continue;
                        }

                        isIdle = true;

                        // wait for pulse
                        while (true)
                        {
                            Monitor.Wait(SyncRoot, 1000);
                            if (Closed)
                            {
                                return;
                            }

                            break;
                        }

                        isIdle = false;
                    }
                }

                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i] is not LogMessage message) continue;
                    currentDelayMsec = (int)(message.Age.Ticks / TimeSpan.TicksPerMillisecond);

                    // is this message late ?
                    if (currentDelayMsec > LateMessageMilliseconds)
                    {
                        // yes, opportune logging ?
                        if (Mode == LogReceiverMode.Opportune)
                        {
                            // yes -> discard
                            discardedCount++;
                            continue;
                        }

                        // no continuous logging -> warn user
                        if (!delayWarningSent && (messageQueueCount + list.Count > LateMessageThreshold) && (MonotonicTime.UtcNow > nextWarningUtc))
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
    public bool Idle { get { lock (SyncRoot) { return isIdle; } } }

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
    public object SyncRoot { get; } = new();

    /// <inheritdoc />
    public abstract void Write(LogMessage message);
}
