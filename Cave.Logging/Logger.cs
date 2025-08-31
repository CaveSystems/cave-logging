using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Cave.Collections.Generic;
using Cave.IO;

namespace Cave.Logging;

/// <summary>
/// This is a full featured asynchronous logging facility for general status monitoring and logging for end users in production products. Messages logged are
/// queued and then distributed by a background thread to provide full speed even with slow loggers (file, database, network).
/// </summary>
public class Logger
{
    #region Private Fields

    static readonly Fifo<LogMessage> Fifo = new();
    static readonly AutoResetEvent MessageTrigger = new(false);
    static readonly Set<LogReceiver> ReceiverSet = new();
    static readonly Thread Thread;
    static volatile bool isIdle;

    #endregion Private Fields

    #region Private Methods

    static void MasterWorker()
    {
        while (true)
        {
            isIdle = true;
            while (isIdle && Fifo.Available == 0)
            {
                //todo: check if there are still race conditions with isIdle, fifo.Available and messageTrigger
                if (MessageTrigger.WaitOne(1000)) break;
            }
            isIdle = false;

            Thread.BeginThreadAffinity();
            Thread.BeginCriticalRegion();

            //read from ringbuffer
            var count = Fifo.Available;
            IList<LogMessage> messages;
            {
                List<LogMessage> list = new(count);
                for (var i = 0; i < count; i++)
                {
                    if (Fifo.TryDequeue(out var message))
                    {
                        list.Add(message!);
                    }
                    else break;
                }
                messages = list.AsReadOnly();
            }

            //push to receivers
            lock (ReceiverSet)
            {
                foreach (var receiver in ReceiverSet)
                {
                    if (receiver.Started)
                    {
                        receiver.Fifo.Enqueue(messages);
                    }
                }
            }

            Thread.EndCriticalRegion();
            Thread.EndThreadAffinity();
        }
    }

    static void SetLogToDebug(bool value)
    {
        if (value)
        {
            (DebugReceiver ??= new LogDebugReceiver()).LogToDebug = value;
            if (!DebugReceiver.Started) DebugReceiver.Start();
        }
        else if (DebugReceiver is not null)
        {
            DebugReceiver.LogToDebug = value;
        }
    }

    static void SetLogToTrace(bool value)
    {
        if (value)
        {
            (DebugReceiver ??= new LogDebugReceiver()).LogToTrace = value;
            if (!DebugReceiver.Started) DebugReceiver.Start();
        }
        else if (DebugReceiver is not null)
        {
            DebugReceiver.LogToTrace = value;
        }
    }

    #endregion Private Methods

    #region Public Constructors

    /// <summary>Starts the logging system.</summary>
    static Logger()
    {
        try
        {
            HostName = Environment.MachineName.ToLowerInvariant();
        }
        catch
        {
            System.Diagnostics.Debug.WriteLine("Logger.cctor(): Could not get HostName!");
            HostName = InstallationGuid.SystemGuid.ToString("D");
        }

        try
        {
            Process = Process.GetCurrentProcess();
        }
        catch
        {
            System.Diagnostics.Debug.WriteLine("Logger.cctor(): Could not get Process!");
        }

        Thread = new Thread(MasterWorker)
        {
            IsBackground = true,
            Name = "Logger.MasterWorker",
            Priority = ThreadPriority.Highest,
        };
        Thread.Start();
    }

    /// <summary>
    /// Constructor for backward compatibility - do not use, requires slow stacktrace
    /// </summary>
    /// <param senderType="senderType">Type of the log source.</param>
    /// <param senderName="senderName">(Optional) Name of the log source. Defaults to <paramref name="senderType"/>.Name</param>
    /// <exception cref="ArgumentNullException"></exception>
    [Obsolete("Slow constructor usage: Use one of the newer constructors if possible.")]
    public Logger(Type senderType, string? senderName)
    {
        var frame = new StackFrame(1, true);
        var method = frame.GetMethod();
        SenderSource = $"{frame.GetFileName()}:{frame.GetFileLineNumber()}:{method?.DeclaringType?.Name}";
        SenderType = senderType;
        SenderName = senderName ?? SenderType?.Name ?? "unknown";
    }
    
    /// <summary>Initializes a new instance of the <see cref="Logger"/> class.</summary>
    /// <param senderName="senderName">Name of the log source.</param>
    /// <remarks>
    /// This method is the slowest option when creating a logger. This should not be called thousands of times. Faster variants are: <see
    /// cref="Logger.Create(object)"/> or new Logger(Type)
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public Logger(string? senderName = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
    {
        SenderSource = $"{file}:{line}:{member}";
        var method = new StackFrame(1).GetMethod();
        SenderType = method?.DeclaringType;
        SenderName = senderName ?? SenderType?.Name ?? "unknown";
    }

    /// <summary>Initializes a new instance of the <see cref="Logger"/> class.</summary>
    /// <param senderType="senderType">Type of the log source.</param>
    /// <param senderName="senderName">(Optional) Name of the log source. Defaults to <paramref name="senderType"/>.Name</param>
    /// <remarks>This method is a fast way to create a logger.</remarks>
    public Logger(Type senderType, string? senderName = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
    {
        SenderSource = $"{file}:{line}:{member}";
        SenderName = senderName ?? senderType?.Name ?? throw new ArgumentNullException(nameof(senderType));
        SenderType = senderType;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets the <see cref="LogDebugReceiver"/> instance.</summary>
    public static LogDebugReceiver? DebugReceiver { get; set; }

    /// <summary>Gets or sets the host senderName of the local computer.</summary>
    public static string HostName { get; set; }

    /// <summary>Gets or sets a value indicating whether debug information ({member}:{file}:{line}) shall be part of the sender name or not.</summary>
    public static bool IncludeDebugInformation { get; set; } = Debugger.IsAttached;

    /// <summary>Gets or sets a value indicating whether the logging system logs to <see cref="System.Diagnostics.Debug"/>.</summary>
    public static bool LogToDebug { get => DebugReceiver?.LogToDebug == false; set => SetLogToDebug(value); }

    /// <summary>Gets or sets a value indicating whether the logging system logs to <see cref="Trace"/>. This setting is false by default.</summary>
    public static bool LogToTrace { get => DebugReceiver?.LogToTrace == false; set => SetLogToTrace(value); }

    /// <summary>Gets or sets my process.</summary>
    public static Process? Process { get; set; }

    /// <summary>Gets or sets the number of messages read by receivers.</summary>
    public static long ReadCount => Fifo.ReadCount;

    /// <summary>Gets all registered log receivers</summary>
    public static IEnumerable<LogReceiver> Receivers
    {
        get
        {
            lock (ReceiverSet)
            {
                return ReceiverSet.ToArray();
            }
        }
    }

    /// <summary>Gets or sets the number of messages written to the ring buffer.</summary>
    public static long WriteCount => Fifo.WriteCount;

    /// <summary>Gets or sets the name of the log source.</summary>
    /// <value>The name of the log source.</value>
    public string SenderName { get; set; }

    /// <summary>Gets or sets the sourcecode information of the log source.</summary>
    /// <value>The sorucecode information of the log source.</value>
    public string? SenderSource { get; set; }

    /// <summary>Gets or sets the type of the log source.</summary>
    /// <value>The type of the log source.</value>
    public Type? SenderType { get; set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Closes all receivers, does not flush or wait.</summary>
    public static void Close()
    {
        LogReceiver[] receivers;
        lock (ReceiverSet)
        {
            receivers = [.. ReceiverSet];
            ReceiverSet.Clear();
        }

        foreach (var worker in receivers)
        {
            worker.Close();
        }
    }

    /// <remarks>This method is a fast way to create a logger.</remarks>
    public static Logger Create(object sender) => new(sender.GetType());

    /// <summary>Waits until all notifications are sent.</summary>
    public static void Flush() => Flush(10000, false);

    /// <summary>Waits until all notifications are sent.</summary>
    public static void Flush(int maxWaitMilliseconds = 10000, bool throwTimeoutException = false)
    {
        var deadlockWatch = StopWatch.StartNew();
        while (true)
        {
            Parallel.ForEach(Receivers, receiver => receiver.Flush());

            if (!isIdle)
            {
                while (!isIdle) Thread.Sleep(1);
                deadlockWatch.Reset();
            }
            // any receivers not idle means we need to wait
            if ((Fifo.Available == 0) && ReceiverSet.All(w => w.Idle))
            {
                // all receivers idle
                if (isIdle) return;
            }

            if (maxWaitMilliseconds > 0 && deadlockWatch.ElapsedMilliSeconds > maxWaitMilliseconds)
            {
                Trace.WriteLine($"Waiting for receivers: {ReceiverSet.Where(r => !r.Idle).Join(',')}");
                if (throwTimeoutException) throw new TimeoutException();
                deadlockWatch.Reset();
            }
        }
    }

    /// <summary>Registers and starts an <see cref="LogReceiver"/>.</summary>
    /// <param senderName="logReceiver">The <see cref="LogReceiver"/> to register.</param>
    public static void Register(LogReceiver logReceiver)
    {
        if (logReceiver == null)
        {
            throw new ArgumentNullException(nameof(logReceiver));
        }

        if (logReceiver.Closed)
        {
            throw new ArgumentException($"Receiver {logReceiver} was already closed!");
        }

        lock (ReceiverSet)
        {
            if (ReceiverSet.Contains(logReceiver))
            {
                throw new InvalidOperationException($"LogReceiver {logReceiver} is already registered!");
            }

            ReceiverSet.Add(logReceiver);
        }
    }

    /// <summary>Writes a <see cref="LogMessage"/> instance to the logging system.</summary>
    /// <param name="message">Message to send</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public static void Send(LogMessage message)
    {
        Fifo.Enqueue(message);
        if (isIdle)
        {
            isIdle = false;
            MessageTrigger.Set();
        }
    }

    /// <summary>Unregisters a receiver.</summary>
    public static void Unregister(LogReceiver logReceiver)
    {
        if (logReceiver == null)
        {
            throw new ArgumentNullException(nameof(logReceiver));
        }

        lock (ReceiverSet)
        {
            // remove if present
            ReceiverSet.TryRemove(logReceiver);
        }
    }

    /// <summary>(1) Transmits a <see cref="LogLevel.Alert"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Alert(IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Alert, content, exception, member, file, line));

    /// <summary>(1) Transmits a <see cref="LogLevel.Alert"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Alert(FormattableString content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Alert, content, exception, member, file, line));

    /// <summary>(1) Transmits a <see cref="LogLevel.Alert"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Alert(Exception exception, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Alert, LogString.Empty, exception, member, file, line));

    /// <summary>(2) Transmits a <see cref="LogLevel.Critical"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Critical(IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Critical, content, exception, member, file, line));

    /// <summary>(2) Transmits a <see cref="LogLevel.Critical"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Critical(FormattableString content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Critical, content, exception, member, file, line));

    /// <summary>(2) Transmits a <see cref="LogLevel.Critical"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Critical(Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Critical, LogString.Empty, exception, member, file, line));

    /// <summary>(7) Transmits a <see cref="LogLevel.Debug"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Debug(IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Debug, content, exception, member, file, line));

    /// <summary>(7) Transmits a <see cref="LogLevel.Debug"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Debug(FormattableString content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Debug, content, exception, member, file, line));

    /// <summary>(7) Transmits a <see cref="LogLevel.Debug"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Debug(Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Debug, LogString.Empty, exception, member, file, line));

    /// <summary>(0) Transmits a <see cref="LogLevel.Emergency"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Emergency(IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Emergency, content, exception, member, file, line));

    /// <summary>(0) Transmits a <see cref="LogLevel.Emergency"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Emergency(FormattableString content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Emergency, content, exception, member, file, line));

    /// <summary>(0) Transmits a <see cref="LogLevel.Emergency"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Emergency(Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Emergency, LogString.Empty, exception, member, file, line));

    /// <summary>(3) Transmits a <see cref="LogLevel.Error"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Error(Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Error, LogString.Empty, exception, member, file, line));

    /// <summary>(3) Transmits a <see cref="LogLevel.Error"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Error(IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Error, content, exception, member, file, line));

    /// <summary>(3) Transmits a <see cref="LogLevel.Error"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Error(FormattableString content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Error, content, exception, member, file, line));

    /// <summary>(6) Transmits a <see cref="LogLevel.Information"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Info(IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Information, content, exception, member, file, line));

    /// <summary>(6) Transmits a <see cref="LogLevel.Information"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Info(FormattableString content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Information, content, exception, member, file, line));

    /// <summary>(6) Transmits a <see cref="LogLevel.Information"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Info(Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Information, LogString.Empty, exception, member, file, line));

    /// <summary>(5) Transmits a <see cref="LogLevel.Notice"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Notice(IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Notice, content, exception, member, file, line));

    /// <summary>(5) Transmits a <see cref="LogLevel.Notice"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Notice(FormattableString content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Notice, content, exception, member, file, line));

    /// <summary>(5) Transmits a <see cref="LogLevel.Notice"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Notice(Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Notice, LogString.Empty, exception, member, file, line));

    /// <summary>Transmits a message.</summary>
    /// <param name="level">The level.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Send(LogLevel level, IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, level, content, exception, member, file, line));

    /// <summary>Transmits a message.</summary>
    /// <param name="level">The level.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Send(LogLevel level, Exception exception, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, level, LogString.Empty, exception, member, file, line));

    /// <summary>(8) Transmits a <see cref="LogLevel.Verbose"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Verbose(IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Verbose, content, exception, member, file, line));

    /// <summary>(8) Transmits a <see cref="LogLevel.Verbose"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Verbose(FormattableString content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Verbose, content, exception, member, file, line));

    /// <summary>(8) Transmits a <see cref="LogLevel.Verbose"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Verbose(Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Verbose, LogString.Empty, exception, member, file, line));

    /// <summary>(4) Transmits a <see cref="LogLevel.Warning"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Warning(IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Warning, content, exception, member, file, line));

    /// <summary>(4) Transmits a <see cref="LogLevel.Warning"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Warning(FormattableString content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Warning, content, exception, member, file, line));

    /// <summary>(4) Transmits a <see cref="LogLevel.Warning"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Warning(Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(this, LogLevel.Warning, LogString.Empty, exception, member, file, line));

    #endregion Public Methods

}
