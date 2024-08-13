using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cave.Logging;

namespace Cave.Logging;

/// <summary>Provides simple ugly event logging for windows.</summary>
public sealed class LogEventLog : LogReceiver, IDisposable
{
    #region Private Classes

    class LogEventLogWriter : ILogWriter
    {
        #region Private Fields

        readonly object flushLock = new();
        StringBuilder currentMessage = new();
        EventLogEntryType currentType = EventLogEntryType.Information;
        volatile bool flushWaiting;
        int lastWrite;
        LogEventLog logEventLog;

        #endregion Private Fields

        #region Private Properties

        int LastWriteMillis => Math.Abs(Environment.TickCount - lastWrite);

        #endregion Private Properties

        #region Private Methods

        void FlushLater()
        {
            while (LastWriteMillis < 1000) Thread.Sleep(100);
            lock (flushLock)
            {
                Flush();
                flushWaiting = false;
            }
        }

        #endregion Private Methods

        #region Public Constructors

        public LogEventLogWriter(LogEventLog logEventLog) => this.logEventLog = logEventLog ?? throw new ArgumentNullException(nameof(logEventLog));

        #endregion Public Constructors

        #region Public Properties

        public bool IsClosed { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Close()
        {
            IsClosed = true;
            Flush();
        }

        public void Flush()
        {
            lock (flushLock)
            {
                if (currentMessage.Length > 0)
                {
                    logEventLog.eventLog?.WriteEntry(currentMessage.ToString(), currentType);
                    currentMessage = new();
                }
                lastWrite = Environment.TickCount;
            };
        }

        public void Write(LogMessage message, IEnumerable<ILogText> items)
        {
            if (IsClosed || logEventLog.eventLog == null || message.Level > logEventLog.logLevel)
            {
                return;
            }

            lock (flushLock)
            {
                var type = EventLogEntryType.Information;
                if (message.Level <= LogLevel.Warning)
                {
                    type = EventLogEntryType.Warning;
                }
                if (message.Level <= LogLevel.Error)
                {
                    type = EventLogEntryType.Error;
                }

                if (type != currentType || currentMessage.Length > 16384)
                {
                    Flush();
                }
                currentType = type;
                var lf = false;
                foreach (var item in items)
                {
                    if (item.Equals(LogText.NewLine))
                    {
                        currentMessage.AppendLine(item.Text);
                        lf = true;
                    }
                    else
                    {
                        currentMessage.Append(item.Text);
                    }
                }
                if (!lf) currentMessage?.AppendLine();

                if (!flushWaiting)
                {
                    flushWaiting = true;
                    Task.Factory.StartNew(FlushLater);
                }
            }
        }

        #endregion Public Methods
    }

    #endregion Private Classes

    #region Private Fields

    EventLog? eventLog = null;
    LogLevel logLevel = LogLevel.Information;

    #endregion Private Fields

    #region Private Methods

    void Init()
    {
        try
        {
            if (!EventLog.SourceExists(ProcessName))
            {
                EventLog.CreateEventSource(ProcessName, LogName);
            }
            if (!EventLog.SourceExists(ProcessName))
            {
                throw new NotSupportedException("Due to a bug in the event log system you need to restart this program once (newly created event source is not reported back to the creating process until process recreation)!");
            }
        }
        catch (SecurityException ex)
        {
            throw new SecurityException($"The event source {ProcessName} does not exist and the current user has no right to create it!", ex);
        }
        eventLog = new EventLog(LogName, ".", ProcessName);
        Writer = new LogEventLogWriter(this);
    }

    #endregion Private Methods

    #region Protected Methods

    /// <summary>Releases the unmanaged resources used by this instance and optionally releases the managed resources.</summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            if (eventLog != null)
            {
                eventLog?.Close();
                eventLog = null;
            }
        }
    }

    #endregion Protected Methods

    #region Public Fields

    /// <summary>Retrieves the target event log name.</summary>
    public readonly string LogName;

    /// <summary>Retrieves the process name of the process generating the messages (defaults to the program name).</summary>
    public readonly string ProcessName;

    #endregion Public Fields

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="LogEventLog"/> class, with the default process name and at the default log: "Application:ProcessName".</summary>
    public LogEventLog()
    {
        if (!Platform.IsMicrosoft)
        {
            throw new InvalidOperationException("Do not use LogEventLog on non Microsoft Platforms!");
        }

        ProcessName = Process.GetCurrentProcess().ProcessName;
        LogName = "Application";
        Init();
        Name = eventLog?.LogDisplayName ?? $"{ProcessName}.LogEventLog";
    }

    /// <summary>Initializes a new instance of the <see cref="LogEventLog"/> class.</summary>
    /// <param name="eventLog">The event log.</param>
    public LogEventLog(EventLog eventLog)
    {
        this.eventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));
        ProcessName = Process.GetCurrentProcess().ProcessName;
        LogName = this.eventLog.Log;
        Init();
        Name = eventLog.LogDisplayName;
    }

    /// <summary>Initializes a new instance of the <see cref="LogEventLog"/> class.</summary>
    /// <param name="eventLog">The event log.</param>
    /// <param name="processName">The process name.</param>
    public LogEventLog(EventLog eventLog, string processName)
    {
        this.eventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));
        LogName = this.eventLog.Log;
        ProcessName = processName;
        Init();
        Name = eventLog.LogDisplayName;
    }

    #endregion Public Constructors

    #region Public Methods

    /// <summary>Closes the <see cref="LogReceiver"/>.</summary>
    public override void Close()
    {
        Writer.Flush();
        base.Close();
    }

    #endregion Public Methods
}
