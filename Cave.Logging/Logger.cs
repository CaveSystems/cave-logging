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

namespace Cave.Logging
{
    /// <summary>
    /// This is a full featured asynchronous logging facility for general status monitoring and logging for end users in production products. Messages logged
    /// are queued and then distributed by a background thread to provide full speed even with slow loggers (file, database, network).
    /// </summary>
    [DebuggerDisplay("{" + nameof(SourceName) + "}")]
    public class Logger
    {
        #region Private Fields

        static readonly Set<ILogReceiver> receiverSet = new();
        static readonly UncheckedRingBuffer<LogMessage> ringBuffer = new();
        static readonly Thread thread;

        #endregion Private Fields

        #region Private Methods

        static void MasterWorker()
        {
            while (true)
            {
                LinkedList<LogMessage> items = new();
                while (ringBuffer.TryRead(out var message))
                {
                    items.AddLast(message);
                }

                Thread.Sleep(1);
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
        /// <param name="name">Name of the log source.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Logger(string name = null) => SourceName = name ?? new StackFrame(1).GetMethod()?.DeclaringType?.Name ?? throw new ArgumentException("Could not determine calling class name and no logger name given!");

        /// <summary>Initializes a new instance of the <see cref="Logger"/> class.</summary>
        /// <param name="type">Name of the log source.</param>
        public Logger(Type type) => SourceName = type?.Name ?? throw new ArgumentNullException(nameof(type));

        #endregion Public Constructors

        #region Public Properties

        /// <summary>Gets the <see cref="LogDebugReceiver"/> instance.</summary>
        public static LogDebugReceiver DebugReceiver { get; set; }

        /// <summary>Gets or sets the host name of the local computer.</summary>
        public static string HostName { get; set; }

        /// <summary>Gets or sets a value indicating whether the logging system logs to <see cref="System.Diagnostics.Debug"/>.</summary>
        public static bool LogToDebug { get => DebugReceiver?.LogToDebug == false; set => SetLogToDebug(value); }

        /// <summary>Gets or sets a value indicating whether the logging system logs to <see cref="Trace"/>. This setting is false by default.</summary>
        public static bool LogToTrace { get => DebugReceiver?.LogToTrace == false; set => SetLogToTrace(value); }

        /// <inheritdoc/>
        public static long LostCount => ringBuffer.LostCount;

        /// <summary>Gets or sets the name of the process.</summary>
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

        /// <summary>Gets or sets the name of the log source.</summary>
        /// <value>The name of the log source.</value>
        public string SourceName { get; set; }

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
            while (true)
            {
                Thread.Sleep(1);
                lock (receiverSet)
                {
                    if (receiverSet.Count == 0)
                    {
                        return;
                    }

                    if (receiverSet.Any(w => !w.Idle))
                    {
                        continue;
                    }

                    if (ringBuffer.Available == 0)
                    {
                        continue;
                    }
                }

                // all idle
                break;
            }
        }

        /// <summary>(1) Transmits a <see cref="LogLevel.Alert"/> message.</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogAlert(string source, XT msg, params object[] args) => Send(source, LogLevel.Alert, null, msg, args);

        /// <summary>(1) Transmits a <see cref="LogLevel.Alert"/> message.</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogAlert(string source, Exception ex, XT msg, params object[] args) => Send(source, LogLevel.Alert, ex, msg, args);

        /// <summary>(2) Transmits a <see cref="LogLevel.Critical"/> message.</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogCritical(string source, XT msg, params object[] args) => Send(source, LogLevel.Critical, null, msg, args);

        /// <summary>(2) Transmits a <see cref="LogLevel.Critical"/> message.</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogCritical(string source, Exception ex, XT msg, params object[] args) => Send(source, LogLevel.Critical, ex, msg, args);

        /// <summary>(7) Transmits a <see cref="LogLevel.Debug"/> message.</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogDebug(string source, XT msg, params object[] args) => Send(source, LogLevel.Debug, null, msg, args);

        /// <summary>(7) Transmits a <see cref="LogLevel.Debug"/> message.</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogDebug(string source, Exception ex, XT msg, params object[] args) => Send(source, LogLevel.Debug, ex, msg, args);

        /// <summary>(0) Transmits a <see cref="LogLevel.Emergency"/> message.</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogEmergency(string source, XT msg, params object[] args) => Send(source, LogLevel.Emergency, null, msg, args);

        /// <summary>(0) Transmits a <see cref="LogLevel.Emergency"/> message.</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogEmergency(string source, Exception ex, XT msg, params object[] args) => Send(source, LogLevel.Emergency, ex, msg, args);

        /// <summary>(3) Transmits a <see cref="LogLevel.Error"/> message.</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogError(string source, XT msg, params object[] args) => Send(source, LogLevel.Error, null, msg, args);

        /// <summary>(3) Transmits a <see cref="LogLevel.Error"/> message.</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogError(string source, Exception ex, XT msg, params object[] args) => Send(source, LogLevel.Error, ex, msg, args);

        /// <summary>(6) Transmits a <see cref="LogLevel.Information"/> message.</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogInfo(string source, XT msg, params object[] args) => Send(source, LogLevel.Information, null, msg, args);

        /// <summary>(6) Transmits a <see cref="LogLevel.Information"/> message.</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogInfo(string source, Exception ex, XT msg, params object[] args) => Send(source, LogLevel.Information, ex, msg, args);

        /// <summary>(5) Transmits a <see cref="LogLevel.Notice"/> message.</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogNotice(string source, XT msg, params object[] args) => Send(source, LogLevel.Notice, null, msg, args);

        /// <summary>(5) Transmits a <see cref="LogLevel.Notice"/> message.</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogNotice(string source, Exception ex, XT msg, params object[] args) => Send(source, LogLevel.Notice, ex, msg, args);

        /// <summary>(8) Transmits a <see cref="LogLevel.Verbose"/> message.</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogVerbose(string source, XT msg, params object[] args) => Send(source, LogLevel.Verbose, null, msg, args);

        /// <summary>(8) Transmits a <see cref="LogLevel.Verbose"/> message.</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogVerbose(string source, Exception ex, XT msg, params object[] args) => Send(source, LogLevel.Verbose, ex, msg, args);

        /// <summary>(4) Transmits a <see cref="LogLevel.Warning"/> message.</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogWarning(string source, XT msg, params object[] args) => Send(source, LogLevel.Warning, null, msg, args);

        /// <summary>(4) Transmits a <see cref="LogLevel.Warning"/> message.</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogWarning(string source, Exception ex, XT msg, params object[] args) => Send(source, LogLevel.Warning, ex, msg, args);

        /// <summary>Registers an <see cref="ILogReceiver"/>.</summary>
        /// <param name="logReceiver">The <see cref="ILogReceiver"/> to register.</param>
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
        /// <param name="msg">Message to send.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public static void Send(LogMessage msg) => ringBuffer.Write(msg);

        /// <summary>Creates and writes a new <see cref="LogMessage"/> instance to the logging system.</summary>
        /// <param name="source">The source.</param>
        /// <param name="level">The level.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="content">The content.</param>
        /// <param name="args">The arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public static void Send(string source, LogLevel level, Exception ex, XT content, params object[] args) => Send(new LogMessage(source, DateTime.Now, level, ex, content, args));

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
                receiverSet.Remove(logReceiver);
            }
        }

        /// <summary>(1) Transmits a <see cref="LogLevel.Alert"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Alert(XT msg, params object[] args) => Send(SourceName, LogLevel.Alert, null, msg, args);

        /// <summary>(1) Transmits a <see cref="LogLevel.Alert"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Alert(Exception ex, XT msg = null, params object[] args) => Send(SourceName, LogLevel.Alert, ex, msg, args);

        /// <summary>(2) Transmits a <see cref="LogLevel.Critical"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Critical(XT msg, params object[] args) => Send(SourceName, LogLevel.Critical, null, msg, args);

        /// <summary>(2) Transmits a <see cref="LogLevel.Critical"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Critical(Exception ex, XT msg = null, params object[] args) => Send(SourceName, LogLevel.Critical, ex, msg, args);

        /// <summary>(7) Transmits a <see cref="LogLevel.Debug"/> message.</summary>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Debug(XT msg, params object[] args) => Send(SourceName, LogLevel.Debug, null, msg, args);

        /// <summary>(7) Transmits a <see cref="LogLevel.Debug"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Debug(Exception ex, XT msg = null, params object[] args) => Send(SourceName, LogLevel.Debug, ex, msg, args);

        /// <summary>(0) Transmits a <see cref="LogLevel.Emergency"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Emergency(XT msg, params object[] args) => Send(SourceName, LogLevel.Emergency, null, msg, args);

        /// <summary>(0) Transmits a <see cref="LogLevel.Emergency"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Emergency(Exception ex, XT msg = null, params object[] args) => Send(SourceName, LogLevel.Emergency, ex, msg, args);

        /// <summary>(3) Transmits a <see cref="LogLevel.Error"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Error(XT msg, params object[] args) => Send(SourceName, LogLevel.Error, null, msg, args);

        /// <summary>(3) Transmits a <see cref="LogLevel.Error"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Error(Exception ex, XT msg = null, params object[] args) => Send(SourceName, LogLevel.Error, ex, msg, args);

        /// <summary>(6) Transmits a <see cref="LogLevel.Information"/> message.</summary>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Info(XT msg, params object[] args) => Send(SourceName, LogLevel.Information, null, msg, args);

        /// <summary>(6) Transmits a <see cref="LogLevel.Information"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Info(Exception ex, XT msg = null, params object[] args) => Send(SourceName, LogLevel.Information, ex, msg, args);

        /// <summary>(1) Transmits a <see cref="LogLevel.Alert"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void LogAlert(XT msg, params object[] args) => Send(SourceName, LogLevel.Alert, null, msg, args);

        /// <summary>(1) Transmits a <see cref="LogLevel.Alert"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void LogAlert(Exception ex, XT msg = null, params object[] args) => Send(SourceName, LogLevel.Alert, ex, msg, args);

        /// <summary>(2) Transmits a <see cref="LogLevel.Critical"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void LogCritical(XT msg, params object[] args) => Send(SourceName, LogLevel.Critical, null, msg, args);

        /// <summary>(2) Transmits a <see cref="LogLevel.Critical"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void LogCritical(Exception ex, XT msg = null, params object[] args) => Send(SourceName, LogLevel.Critical, ex, msg, args);

        /// <summary>(7) Transmits a <see cref="LogLevel.Debug"/> message.</summary>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void LogDebug(XT msg, params object[] args) => Send(SourceName, LogLevel.Debug, null, msg, args);

        /// <summary>(7) Transmits a <see cref="LogLevel.Debug"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void LogDebug(Exception ex, XT msg = null, params object[] args) => Send(SourceName, LogLevel.Debug, ex, msg, args);

        /// <summary>(0) Transmits a <see cref="LogLevel.Emergency"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void LogEmergency(XT msg, params object[] args) => Send(SourceName, LogLevel.Emergency, null, msg, args);

        /// <summary>(0) Transmits a <see cref="LogLevel.Emergency"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void LogEmergency(Exception ex, XT msg = null, params object[] args) => Send(SourceName, LogLevel.Emergency, ex, msg, args);

        /// <summary>(3) Transmits a <see cref="LogLevel.Error"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void LogError(XT msg, params object[] args) => Send(SourceName, LogLevel.Error, null, msg, args);

        /// <summary>(3) Transmits a <see cref="LogLevel.Error"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void LogError(Exception ex, XT msg = null, params object[] args) => Send(SourceName, LogLevel.Error, ex, msg, args);

        /// <summary>(6) Transmits a <see cref="LogLevel.Information"/> message.</summary>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void LogInfo(XT msg, params object[] args) => Send(SourceName, LogLevel.Information, null, msg, args);

        /// <summary>(6) Transmits a <see cref="LogLevel.Information"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void LogInfo(Exception ex, XT msg = null, params object[] args) => Send(SourceName, LogLevel.Information, ex, msg, args);

        /// <summary>(5) Transmits a <see cref="LogLevel.Notice"/> message.</summary>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void LogNotice(XT msg, params object[] args) => Send(SourceName, LogLevel.Notice, null, msg, args);

        /// <summary>(5) Transmits a <see cref="LogLevel.Notice"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void LogNotice(Exception ex, XT msg = null, params object[] args) => Send(SourceName, LogLevel.Notice, ex, msg, args);

        /// <summary>(8) Transmits a <see cref="LogLevel.Verbose"/> message.</summary>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void LogVerbose(XT msg, params object[] args) => Send(SourceName, LogLevel.Verbose, null, msg, args);

        /// <summary>(8) Transmits a <see cref="LogLevel.Verbose"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void LogVerbose(Exception ex, XT msg = null, params object[] args) => Send(SourceName, LogLevel.Verbose, ex, msg, args);

        /// <summary>(4) Transmits a <see cref="LogLevel.Warning"/> message.</summary>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void LogWarning(XT msg, params object[] args) => Send(SourceName, LogLevel.Warning, null, msg, args);

        /// <summary>(4) Transmits a <see cref="LogLevel.Warning"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void LogWarning(Exception ex, XT msg = null, params object[] args) => Send(SourceName, LogLevel.Warning, ex, msg, args);

        /// <summary>(5) Transmits a <see cref="LogLevel.Notice"/> message.</summary>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Notice(XT msg, params object[] args) => Send(SourceName, LogLevel.Notice, null, msg, args);

        /// <summary>(5) Transmits a <see cref="LogLevel.Notice"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Notice(Exception ex, XT msg = null, params object[] args) => Send(SourceName, LogLevel.Notice, ex, msg, args);

        /// <summary>(8) Transmits a <see cref="LogLevel.Verbose"/> message.</summary>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Verbose(XT msg, params object[] args) => Send(SourceName, LogLevel.Verbose, null, msg, args);

        /// <summary>(8) Transmits a <see cref="LogLevel.Verbose"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Verbose(Exception ex, XT msg = null, params object[] args) => Send(SourceName, LogLevel.Verbose, ex, msg, args);

        /// <summary>(4) Transmits a <see cref="LogLevel.Warning"/> message.</summary>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Warning(XT msg, params object[] args) => Send(SourceName, LogLevel.Warning, null, msg, args);

        /// <summary>(4) Transmits a <see cref="LogLevel.Warning"/> message.</summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Warning(Exception ex, XT msg = null, params object[] args) => Send(SourceName, LogLevel.Warning, ex, msg, args);

        /// <summary>Writes a message with the specified level.</summary>
        /// <param name="level">The level.</param>
        /// <param name="msg">The message.</param>
        /// <param name="args">The arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Write(LogLevel level, XT msg = null, params object[] args) => Send(SourceName, level, null, msg, args);

        /// <summary>Writes a message with the specified level.</summary>
        /// <param name="level">The level.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="msg">The message.</param>
        /// <param name="args">The arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public void Write(LogLevel level, Exception ex, XT msg = null, params object[] args) => Send(SourceName, level, ex, msg, args);

        #endregion Public Methods
    }
}
