using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
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

    static readonly object idleLock = new();
    static readonly Set<ILogReceiver> receiverSet = new();
    static readonly UncheckedRingBuffer<LogMessage> ringBuffer = new();
    static readonly Thread thread;

    #endregion Private Fields

    #region Private Methods

    static void MasterWorker()
    {
        while (true)
        {
            while (ringBuffer.Available == 0)
            {
                lock (idleLock)
                {
                    Thread.Sleep(1);
                    Monitor.PulseAll(idleLock);
                }
            }

            LinkedList<LogMessage> items = new();
            while (ringBuffer.TryRead(out var message))
            {
                items.AddLast(message);
            }

            lock (receiverSet)
            {
                foreach (var receiver in receiverSet)
                {
                    receiver.AddMessages(items);
                }
            }
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

    #region Public Constructors

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
            HostName = Environment.MachineName;
        }

        try
        {
            ProcessName = Process.GetCurrentProcess().ProcessName;
        }
        catch
        {
            System.Diagnostics.Debug.WriteLine("Logger.cctor(): Could not get ProcessName!");
            ProcessName =
                (Assembly.GetEntryAssembly() ??
                    Assembly.GetExecutingAssembly())?.GetName()?.Name ?? "Unknown process";
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
    [MethodImpl(MethodImplOptions.NoInlining)]
    public Logger(string? senderName = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Sender = senderName ?? new StackFrame(1).GetMethod()?.DeclaringType?.Name ?? $"Unknown:{member}:{file}:{line}";

    /// <summary>Initializes a new instance of the <see cref="Logger"/> class.</summary>
    /// <param senderName="sourceType">Name of the log source.</param>
    public Logger(Type sourceType) => Sender = sourceType?.Name ?? throw new ArgumentNullException(nameof(sourceType));

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets the <see cref="LogDebugReceiver"/> instance.</summary>
    public static LogDebugReceiver DebugReceiver { get; set; }

    /// <summary>Gets or sets the host senderName of the local computer.</summary>
    public static string HostName { get; set; }

    /// <summary>Gets or sets a value indicating whether the logging system logs to <see cref="System.Diagnostics.Debug"/>.</summary>
    public static bool LogToDebug { get => DebugReceiver?.LogToDebug == false; set => SetLogToDebug(value); }

    /// <summary>Gets or sets a value indicating whether the logging system logs to <see cref="Trace"/>. This setting is false by default.</summary>
    public static bool LogToTrace { get => DebugReceiver?.LogToTrace == false; set => SetLogToTrace(value); }

    /// <inheritdoc/>
    public static long LostCount => ringBuffer.LostCount;

    /// <summary>Gets or sets the senderName of the process.</summary>
    public static string ProcessName { get; set; }

    /// <inheritdoc/>
    public static long ReadCount => ringBuffer.ReadCount;

    /// <inheritdoc/>
    public static int ReadPosition => ringBuffer.ReadPosition;

    /// <inheritdoc/>
    public static long RejectedCount => ringBuffer.RejectedCount;

    /// <inheritdoc/>
    public static long WriteCount => ringBuffer.WriteCount;

    /// <inheritdoc/>
    public static int WritePosition => ringBuffer.WritePosition;

    /// <summary>Gets or sets the senderName of the log source.</summary>
    /// <value>The senderName of the log source.</value>
    public string Sender { get; set; }

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

    /// <summary>Closes all receivers, does not flush or wait.</summary>
    [Obsolete("Use Close()")]
    public static void CloseAll() => Close();

    /// <summary>Waits until all notifications are sent.</summary>
    public static void Flush()
    {
        lock (idleLock)
        {
            while (true)
            {
                if (!Monitor.Wait(idleLock))
                {
                    continue;
                }
                // any receivers not idle means we need to wait
                if (receiverSet.All(w => w.Idle))
                {
                    // all receivers idle
                    return;
                }
            }
        }
    }

    /// <summary>Registers an <see cref="ILogReceiver"/>.</summary>
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
    }

    /// <summary>Writes a <see cref="LogMessage"/> instance to the logging system.</summary>
    /// <param name="message">Message to send</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public static void Send(LogMessage message) => ringBuffer.Write(message);

    /// <summary>Creates and writes a new <see cref="LogMessage"/> instance to the logging system.</summary>
    /// <param name="sender">Sender of the message.</param>
    /// <param name="level">The level.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public static void Send(string sender, LogLevel level, Exception? exception, LogText? message, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(new(sender, level, exception, message, member, file, line));

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

    /// <summary>(1) Transmits a <see cref="LogLevel.Alert"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Alert(LogText message, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Alert, exception, message, member, file, line);

    /// <summary>(1) Transmits a <see cref="LogLevel.Alert"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Alert(IFormattable message, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Alert, exception, new(message), member, file, line);

    /// <summary>(1) Transmits a <see cref="LogLevel.Alert"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Alert(Exception exception, LogText? message = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Alert, exception, message, member, file, line);

    /// <summary>(2) Transmits a <see cref="LogLevel.Critical"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Critical(LogText message, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Critical, exception, message, member, file, line);

    /// <summary>(2) Transmits a <see cref="LogLevel.Critical"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Critical(IFormattable message, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Critical, exception, new(message), member, file, line);

    /// <summary>(2) Transmits a <see cref="LogLevel.Critical"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Critical(Exception exception, LogText? message = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Critical, exception, message, member, file, line);

    /// <summary>(7) Transmits a <see cref="LogLevel.Debug"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Debug(IFormattable message, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Debug, exception, new(message), member, file, line);

    /// <summary>(7) Transmits a <see cref="LogLevel.Debug"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Debug(LogText message, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Debug, exception, message, member, file, line);

    /// <summary>(7) Transmits a <see cref="LogLevel.Debug"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Debug(Exception exception, LogText? message = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Debug, exception, message, member, file, line);

    /// <summary>(0) Transmits a <see cref="LogLevel.Emergency"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Emergency(IFormattable message, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Emergency, exception, new(message), member, file, line);

    /// <summary>(0) Transmits a <see cref="LogLevel.Emergency"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Emergency(LogText message, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Emergency, exception, message, member, file, line);

    /// <summary>(0) Transmits a <see cref="LogLevel.Emergency"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Emergency(Exception exception, LogText? message = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Emergency, exception, message, member, file, line);

    /// <summary>(3) Transmits a <see cref="LogLevel.Error"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Error(IFormattable message, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Error, exception, new(message), member, file, line);

    /// <summary>(3) Transmits a <see cref="LogLevel.Error"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Error(LogText message, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Error, exception, message, member, file, line);

    /// <summary>(3) Transmits a <see cref="LogLevel.Error"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Error(Exception exception, LogText? message = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Error, exception, message, member, file, line);

    /// <summary>(6) Transmits a <see cref="LogLevel.Information"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Info(IFormattable message, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Information, exception, new(message), member, file, line);

    /// <summary>(6) Transmits a <see cref="LogLevel.Information"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Info(LogText message, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Information, exception, message, member, file, line);

    /// <summary>(6) Transmits a <see cref="LogLevel.Information"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Info(Exception exception, LogText? message = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Information, exception, message, member, file, line);

    /// <summary>(5) Transmits a <see cref="LogLevel.Notice"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Notice(IFormattable message, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Notice, exception, new(message), member, file, line);

    /// <summary>(5) Transmits a <see cref="LogLevel.Notice"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Notice(LogText message, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Notice, exception, message, member, file, line);

    /// <summary>(5) Transmits a <see cref="LogLevel.Notice"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Notice(Exception exception, LogText? message = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Notice, exception, message, member, file, line);

    /// <summary>(8) Transmits a <see cref="LogLevel.Verbose"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Verbose(IFormattable message, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Verbose, exception, new(message), member, file, line);

    /// <summary>(8) Transmits a <see cref="LogLevel.Verbose"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Verbose(LogText message, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Verbose, exception, message, member, file, line);

    /// <summary>(8) Transmits a <see cref="LogLevel.Verbose"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Verbose(Exception exception, LogText? message = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Verbose, exception, message, member, file, line);

    /// <summary>(4) Transmits a <see cref="LogLevel.Warning"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Warning(IFormattable message, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Warning, exception, new(message), member, file, line);

    /// <summary>(4) Transmits a <see cref="LogLevel.Warning"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Warning(LogText message, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Warning, exception, message, member, file, line);

    /// <summary>(4) Transmits a <see cref="LogLevel.Warning"/> message.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void Warning(Exception exception, LogText? message = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Send(Sender, LogLevel.Warning, exception, message, member, file, line);

    #endregion Public Methods
}
