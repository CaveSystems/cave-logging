using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Cave.Collections.Generic;
using Cave.IO;

namespace Cave.Logging;

/// <summary>
/// This is a full featured asynchronous logging facility for general status monitoring and logging for end users in production products. Messages logged
/// are queued and then distributed by a background thread to provide full speed even with slow loggers (file, database, network).
/// </summary>
public class Logger
{
    #region Private Fields

    static volatile bool isIdle;
    static readonly object idleLock = new();
    static readonly Set<ILogReceiver> receiverSet = new();
    static readonly UncheckedRingBuffer<LogMessage> ringBuffer = new();
    static readonly Thread thread;

    #endregion Private Fields

    #region Private Methods

    static void MasterWorker()
    {
        var log = new Logger(typeof(Logger));
        while (true)
        {
            lock (idleLock)
            {
                while (ringBuffer.Available == 0)
                {
                    isIdle = true;
                    Monitor.PulseAll(idleLock);
                    Monitor.Exit(idleLock);
                    Thread.Sleep(1);
                    Monitor.Enter(idleLock);
                    isIdle = false;
                }
            }

            Thread.BeginThreadAffinity();
            Thread.BeginCriticalRegion();

            //read from ringbuffer
            var count = ringBuffer.Available;
            IList<LogMessage> messages;
            {
                List<LogMessage> list = new(count);
                while (ringBuffer.TryRead(out var message) && list.Count < count)
                {
                    list.Add(message);
                }
                messages = list.AsReadOnly();
            }

            //push to receivers
            lock (receiverSet)
            {
                foreach (var receiver in receiverSet)
                {
                    var ticks = Environment.TickCount;
                    receiver.AddMessages(messages);
                    var duration = Environment.TickCount - ticks;
                    if (duration > 1) log.Alert($"LogReceiver {receiver} needed {duration}ms to accept messages!");
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
        }
        else if (DebugReceiver is not null)
        {
            DebugReceiver.LogToTrace = value;
        }
    }

    #endregion Private Methods

    #region Constructors

    /// <summary>Starts the logging system.</summary>
    static Logger()
    {
        try
        {
            HostName = Dns.GetHostName().ToLower();
        }
        catch
        {
            System.Diagnostics.Debug.WriteLine("Logger.cctor(): Could not get HostName!");
            HostName = Environment.MachineName.ToLower();
        }

        try
        {
            Process = Process.GetCurrentProcess();
        }
        catch
        {
            System.Diagnostics.Debug.WriteLine("Logger.cctor(): Could not get Process!");
        }

        thread = new Thread(MasterWorker)
        {
            IsBackground = true,
            Name = "Logger.MasterWorker",
            Priority = ThreadPriority.Highest,
        };
        thread.Start();
    }

    /// <summary>Initializes a new instance of the <see cref="Logger"/> class.</summary>
    /// <param senderName="senderName">Name of the log source.</param>
    /// <remarks>This method is the slowest when creating a logger. This should not be called thousands of times.
    /// Faster variants are: <see cref="Logger.Create(object)"/> or new Logger(Type)</remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public Logger(string? senderName = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
    {
        SenderType = new StackFrame(1).GetMethod()?.DeclaringType;
        SenderName = senderName ?? SenderType?.Name ?? $"Unknown:{member}:{file}:{line}";
    }

    /// <summary>Initializes a new instance of the <see cref="Logger"/> class.</summary>
    /// <param senderType="senderType">Type of the log source.</param>
    /// <param senderName="senderName">(Optional) Name of the log source. Defaults to <paramref name="senderType"/>.Name</param>
    /// <remarks>This method is fast way to create a logger.</remarks>
    public Logger(Type senderType, string? senderName = null)
    {
        SenderName = senderName ?? senderType?.Name ?? throw new ArgumentNullException(nameof(senderType));
        SenderType = senderType;
    }

    /// <remarks>This method is fast way to create a logger.</remarks>
    public static Logger Create(object sender) => new Logger(sender.GetType());

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets the <see cref="LogDebugReceiver"/> instance.</summary>
    public static LogDebugReceiver? DebugReceiver { get; set; }

    /// <summary>Gets or sets the host senderName of the local computer.</summary>
    public static string HostName { get; set; }

    /// <summary>Gets or sets a value indicating whether the logging system logs to <see cref="System.Diagnostics.Debug"/>.</summary>
    public static bool LogToDebug { get => DebugReceiver?.LogToDebug == false; set => SetLogToDebug(value); }

    /// <summary>Gets or sets a value indicating whether the logging system logs to <see cref="Trace"/>. This setting is false by default.</summary>
    public static bool LogToTrace { get => DebugReceiver?.LogToTrace == false; set => SetLogToTrace(value); }

    /// <summary>Gets or sets the number of messages lost due ring buffer overflows.</summary>
    public static long LostCount => ringBuffer.LostCount;

    /// <summary>Gets or sets my process.</summary>
    public static Process? Process { get; set; }

    /// <summary>Gets or sets the number of messages read by receivers.</summary>
    public static long ReadCount => ringBuffer.ReadCount;

    /// <summary>Gets or sets the current read position at the ring buffer.</summary>
    public static int ReadPosition => ringBuffer.ReadPosition;

    /// <summary>Gets or sets the number of rejected messages.</summary>
    public static long RejectedCount => ringBuffer.RejectedCount;

    /// <summary>Gets or sets the number of messages written to the ring buffer.</summary>
    public static long WriteCount => ringBuffer.WriteCount;

    /// <summary>Gets or sets the write position at the ring buffer.</summary>
    public static int WritePosition => ringBuffer.WritePosition;

    /// <summary>Gets or sets the senderName of the log source.</summary>
    /// <value>The senderName of the log source.</value>
    public string SenderName { get; set; }

    /// <summary>Gets or sets the senderType of the log source.</summary>
    /// <value>The senderType of the log source.</value>
    public Type? SenderType { get; set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Closes all receivers, does not flush or wait.</summary>
    public static void Close()
    {
        ILogReceiver[] receivers;
        lock (receiverSet)
        {
            receivers = receiverSet.ToArray();
            receiverSet.Clear();
        }

        foreach (var worker in receivers)
        {
            worker.Close();
        }
    }

    /// <summary>Waits until all notifications are sent.</summary>
    public static void Flush()
    {
        lock (idleLock)
        {
            while (true)
            {
                if (!Monitor.Wait(idleLock) || !isIdle)
                {
                    continue;
                }
                // any receivers not idle means we need to wait
                if (ringBuffer.Available == 0 && receiverSet.All(w => w.Idle))
                {

                    // all receivers idle
                    return;
                }
            }
        }
    }

    /// <summary>Registers and starts an <see cref="ILogReceiver"/>.</summary>
    /// <param senderName="logReceiver">The <see cref="ILogReceiver"/> to register.</param>
    public static void Register(ILogReceiver logReceiver)
    {
        if (logReceiver == null)
        {
            throw new ArgumentNullException(nameof(logReceiver));
        }

        if (logReceiver.Closed)
        {
            throw new ArgumentException($"Receiver {logReceiver} was already closed!");
        }

        lock (receiverSet)
        {
            if (receiverSet.Contains(logReceiver))
            {
                throw new InvalidOperationException($"LogReceiver {logReceiver} is already registered!");
            }

            receiverSet.Add(logReceiver);
        }

        logReceiver.Start();
    }

    /// <summary>Writes a <see cref="LogMessage"/> instance to the logging system.</summary>
    /// <param name="message">Message to send</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public static void Send(LogMessage message) => ringBuffer.Write(message);

    /// <summary>Unregisters a receiver.</summary>
    public static void Unregister(ILogReceiver logReceiver)
    {
        if (logReceiver == null)
        {
            throw new ArgumentNullException(nameof(logReceiver));
        }

        lock (receiverSet)
        {
            // remove if present
            receiverSet.TryRemove(logReceiver);
        }
    }

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
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Send(LogLevel level, string? content, Exception exception, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, level, $"{content}", exception, member, file, line));

    /// <summary>Transmits a message.</summary>
    /// <param name="level">The level.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Send(LogLevel level, Exception exception, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, level, $"", exception, member, file, line));

    /// <summary>(1) Transmits a <see cref="LogLevel.Alert"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Alert(IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Alert, content, exception, member, file, line));

    /// <summary>(1) Transmits a <see cref="LogLevel.Alert"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Alert(string? content, Exception exception, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Alert, $"{content}", exception, member, file, line));

    /// <summary>(1) Transmits a <see cref="LogLevel.Alert"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Alert(Exception exception, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Alert, $"", exception, member, file, line));

    /// <summary>(2) Transmits a <see cref="LogLevel.Critical"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Critical(IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Critical, content, exception, member, file, line));

    /// <summary>(2) Transmits a <see cref="LogLevel.Critical"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Critical(string content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Critical, $"{content}", exception, member, file, line));

    /// <summary>(2) Transmits a <see cref="LogLevel.Critical"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Critical(Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Critical, $"", exception, member, file, line));

    /// <summary>(7) Transmits a <see cref="LogLevel.Debug"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Debug(IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Debug, content, exception, member, file, line));

    /// <summary>(7) Transmits a <see cref="LogLevel.Debug"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Debug(string content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Debug, $"{content}", exception, member, file, line));

    /// <summary>(7) Transmits a <see cref="LogLevel.Debug"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Debug(Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Debug, $"", exception, member, file, line));

    /// <summary>(0) Transmits a <see cref="LogLevel.Emergency"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Emergency(IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Emergency, content, exception, member, file, line));

    /// <summary>(0) Transmits a <see cref="LogLevel.Emergency"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Emergency(string content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Emergency, $"{content}", exception, member, file, line));

    /// <summary>(0) Transmits a <see cref="LogLevel.Emergency"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Emergency(Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Emergency, $"", exception, member, file, line));

    /// <summary>(3) Transmits a <see cref="LogLevel.Error"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Error(Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Error, $"", exception, member, file, line));

    /// <summary>(3) Transmits a <see cref="LogLevel.Error"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Error(IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Error, content, exception, member, file, line));

    /// <summary>(3) Transmits a <see cref="LogLevel.Error"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Error(string content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Error, $"{content}", exception, member, file, line));

    /// <summary>(6) Transmits a <see cref="LogLevel.Information"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Info(IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Information, content, exception, member, file, line));

    /// <summary>(6) Transmits a <see cref="LogLevel.Information"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Info(string content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Information, $"{content}", exception, member, file, line));

    /// <summary>(6) Transmits a <see cref="LogLevel.Information"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Info(Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Information, $"", exception, member, file, line));


    /// <summary>(5) Transmits a <see cref="LogLevel.Notice"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Notice(IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Notice, content, exception, member, file, line));

    /// <summary>(5) Transmits a <see cref="LogLevel.Notice"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Notice(string content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Notice, $"{content}", exception, member, file, line));

    /// <summary>(5) Transmits a <see cref="LogLevel.Notice"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Notice(Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Notice, $"", exception, member, file, line));

    /// <summary>(8) Transmits a <see cref="LogLevel.Verbose"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Verbose(IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Verbose, content, exception, member, file, line));

    /// <summary>(8) Transmits a <see cref="LogLevel.Verbose"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Verbose(string content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Verbose, $"{content}", exception, member, file, line));

    /// <summary>(8) Transmits a <see cref="LogLevel.Verbose"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Verbose(Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Verbose, $"", exception, member, file, line));

    /// <summary>(4) Transmits a <see cref="LogLevel.Warning"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Warning(IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Warning, content, exception, member, file, line));

    /// <summary>(4) Transmits a <see cref="LogLevel.Warning"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Warning(string content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Warning, $"{content}", exception, member, file, line));

    /// <summary>(4) Transmits a <see cref="LogLevel.Warning"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Warning(Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(SenderName, SenderType, LogLevel.Warning, $"", exception, member, file, line));

    #endregion Public Methods
}
