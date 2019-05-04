using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using Cave.Console;
using res = Cave.Logging.Properties.Resources;

namespace Cave.Logging
{
    /// <summary>
    /// This is a full featured asynchronous logging facility for general statusmonitoring and logging for end users
    /// in production products. Messages logged are queued and then distributed by a background thread to provide full
    /// speed even with slow loggers (file, database, network).
    /// </summary>
    public class Logger : ILogSource
    {
        static string hostName;
        static string processName;

        /// <summary>
        /// Gets the host name of the local computer.
        /// </summary>
        public static string HostName
        {
            get
            {
                if (hostName == null)
                {
                    hostName = Dns.GetHostName().ToLower().ToString();
                }
                return hostName;
            }
        }

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        public static string ProcessName
        {
            get
            {
                if (processName == null)
                {
                    try { processName = AssemblyVersionInfo.Program.Product; }
                    catch (Exception ex) { LogError("Logger", "Error loading process name into logger instance: " + ex.Message); }
                }
                if (processName == null)
                {
                    try { processName = Process.GetCurrentProcess().ProcessName; }
                    catch (Exception ex) { LogError("Logger", "Error loading process name into logger instance: " + ex.Message); }
                }
                return processName;
            }
        }

        #region DistributionWorkerClass
        [DebuggerDisplay("{m_Receiver}")]
        sealed class DistributeWorker : ILogSource
        {
            ILogReceiver receiver;
            LinkedList<LogMessage> messageQueue = new LinkedList<LogMessage>();

            /// <summary>The current position in the static queue, this is relative to queue and is decreased/reseted during cleanup.</summary>
            int currentDelayMilliSeconds;
            bool isIdle;

            void Worker()
            {
                Thread.CurrentThread.Name = "Logger " + receiver;
                Thread.CurrentThread.IsBackground = true;
                bool delayWarningSent = false;
                try
                {
                    DateTime nextWarningUtc = DateTime.MinValue;
                    int discardedCount = 0;

                    while (!receiver.Closed)
                    {
                        LinkedList<LogMessage> msgs = null;

                        // wait for messages
                        lock (this)
                        {
                            while (true)
                            {
                                if (messageQueue.Count > 0)
                                {
                                    msgs = messageQueue;
                                    messageQueue = new LinkedList<LogMessage>();
                                    break;
                                }

                                // entering idle mode
                                if (delayWarningSent)
                                {
                                    this.LogNotice(string.Format(res.LogReceiver_BacklogRecovered, receiver));
                                    delayWarningSent = false;
                                    continue;
                                }
                                isIdle = true;

                                // wait for pulse
                                while (true)
                                {
                                    Monitor.Wait(this, 1000);
                                    if (receiver.Closed)
                                    {
                                        return;
                                    }

                                    break;
                                }
                                isIdle = false;
                            }
                        }

                        foreach (LogMessage msg in msgs)
                        {
                            long delayTicks = (DateTime.UtcNow - msg.DateTime.ToUniversalTime()).Ticks;
                            currentDelayMilliSeconds = (int)(delayTicks / TimeSpan.TicksPerMillisecond);

                            // do we have late messages ?
                            if (currentDelayMilliSeconds > receiver.LateMessageMilliSeconds)
                            {
                                // yes, opportune logging ?
                                if (receiver.Mode == LogReceiverMode.Opportune)
                                {
                                    // discard old notifications
                                    if (delayTicks / TimeSpan.TicksPerMillisecond > receiver.LateMessageMilliSeconds)
                                    {
                                        discardedCount++;
                                        continue;
                                    }
                                }
                                else
                                {
                                    // no continous logging -> warn user
                                    if ((msgs.Count > receiver.LateMessageTreshold) && (DateTime.UtcNow > nextWarningUtc))
                                    {
                                        string warning = string.Format(res.LogReceiver_Backlog, receiver, msgs.Count, StringExtensions.FormatTime(TimeSpan.FromMilliseconds(currentDelayMilliSeconds)));

                                        // warn all
                                        this.LogWarning(warning);

                                        // warn self (direct write)
                                        receiver.Write(new LogMessage(receiver.LogSourceName, DateTime.Now, LogLevel.Warning, null, warning, null));

                                        // calc next
                                        nextWarningUtc = DateTime.UtcNow + receiver.TimeBetweenWarnings;
                                        delayWarningSent = true;
                                    }
                                }
                            }
                            if (receiver.Closed)
                            {
                                break;
                            }

                            if (msg.Level > receiver.Level)
                            {
                                continue;
                            }

                            receiver.Write(msg);
                        }
                        if (discardedCount > 0)
                        {
                            if (DateTime.UtcNow > nextWarningUtc)
                            {
                                string warning = string.Format(res.LogReceiver_Discarded, receiver, discardedCount);
                                receiver.Write(new LogMessage(receiver.LogSourceName, DateTime.Now, LogLevel.Warning, null, warning, null));
                                discardedCount = 0;
                                nextWarningUtc = DateTime.UtcNow + receiver.TimeBetweenWarnings;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.LogEmergency(string.Format(res.Error_FatalExceptionAt, receiver.LogSourceName), ex);
                    receiver.Close();
                }
            }

            public void AddMessages(IEnumerable<LogMessage> messages)
            {
                if (!Monitor.TryEnter(this, 1000))
                {
                    Send("Logger", LogLevel.Emergency, null, "Deadlock of logger worker queue {0} detected. Disabling receiver!", receiver);
                    receiver.Close();
                    return;
                }
                foreach (LogMessage msg in messages)
                {
                    messageQueue.AddLast(msg);
                }
                Monitor.Pulse(this);
                Monitor.Exit(this);
            }

            public bool Idle { get { lock (this) { return isIdle; } } }

            public string LogSourceName => "Logger.DistributeWorker";

            public DistributeWorker(ILogReceiver receiver)
            {
                this.receiver = receiver;
                new Thread(Worker).Start();
            }

            public void Close() { receiver.Close(); }
        }
        #endregion

        #region static class

        static readonly Dictionary<ILogReceiver, DistributeWorker> distributeWorkers = new Dictionary<ILogReceiver, DistributeWorker>();
        static LinkedList<LogMessage> masterQueue = new LinkedList<LogMessage>();
        static object masterSync = new object();
        static volatile bool masterIdle;
        static Thread logThread;

        private static void MasterWorker()
        {
            while (true)
            {
                LinkedList<LogMessage> items;
                lock (masterSync)
                {
                    if (masterQueue.Count == 0)
                    {
                        masterIdle = true;
                        Monitor.Wait(masterSync);
                        masterIdle = false;
                    }
                    items = masterQueue;
                    masterQueue = new LinkedList<LogMessage>();
                }
                lock (distributeWorkers)
                {
                    foreach (DistributeWorker worker in distributeWorkers.Values.ToArray())
                    {
                        worker.AddMessages(items);
                    }
                }
            }
        }

        /// <summary>
        /// Adds the debugreceiver if a debugger is attached.
        /// </summary>
        static Logger()
        {
            if (Debugger.IsAttached)
            {
                DebugReceiver = new LogDebugReceiver();
            }
            logThread = new Thread(MasterWorker)
            {
                IsBackground = true,
                Name = "Logger.MasterWorker",
            };
            logThread.Start();
        }

        /// <summary>
        /// Provides access to the <see cref="LogDebugReceiver"/> instance.
        /// This is only set if a debugger was attached during startup.
        /// </summary>
        public static LogDebugReceiver DebugReceiver { get; private set; }

        /// <summary>
        /// Registers an <see cref="ILogReceiver"/>.
        /// </summary>
        /// <param name="logReceiver">The <see cref="ILogReceiver"/> to register.</param>
        public static void Register(ILogReceiver logReceiver)
        {
            if (logReceiver == null)
            {
                throw new ArgumentNullException(nameof(logReceiver));
            }

            if (logReceiver.Closed)
            {
                throw new ArgumentException(string.Format("Receiver {0} was already closed!", logReceiver));
            }

            lock (distributeWorkers)
            {
                if (distributeWorkers.ContainsKey(logReceiver))
                {
                    throw new InvalidOperationException(string.Format("LogReceiver {0} is already registered!", logReceiver));
                }

                distributeWorkers.Add(logReceiver, new DistributeWorker(logReceiver));
            }
        }

        /// <summary>
        /// Unregisters a receiver.
        /// </summary>
        public static void Unregister(ILogReceiver logReceiver)
        {
            if (logReceiver == null)
            {
                throw new ArgumentNullException("logReceiver");
            }
            lock (distributeWorkers)
            {
                // remove if present
                distributeWorkers.Remove(logReceiver);
            }
        }

        /// <summary>
        /// Log to <see cref="Trace"/>. This setting is false by default.
        /// </summary>
        public static bool LogToTrace { get => DebugReceiver.LogToTrace; set => DebugReceiver.LogToTrace = value; }

        /// <summary>
        /// Log to <see cref="Debug"/>. This setting is true on debug compiles by default.
        /// </summary>
        public static bool LogToDebug { get => DebugReceiver.LogToDebug; set => DebugReceiver.LogToDebug = value; }

        /// <summary>Creates and writes a new <see cref="LogMessage" /> synchronously (slow) to the logging system.</summary>
        /// <param name="msg"></param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public static void Send(LogMessage msg)
        {
            lock (masterSync)
            {
                masterQueue.AddLast(msg);
                if (masterIdle)
                {
                    Monitor.Pulse(masterSync);
                }
            }
        }

        /// <summary>Creates and writes a new <see cref="LogMessage" /> synchronously (slow) to the logging system.</summary>
        /// <param name="messages"></param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public static void Send(params LogMessage[] messages)
        {
            lock (masterSync)
            {
                foreach (LogMessage msg in messages)
                {
                    masterQueue.AddLast(msg);
                }

                if (masterIdle)
                {
                    Monitor.Pulse(masterSync);
                }
            }
        }

        /// <summary>Creates and writes a new <see cref="LogMessage" /> synchronously (slow) to the logging system.</summary>
        /// <param name="source">The source.</param>
        /// <param name="level">The level.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="content">The content.</param>
        /// <param name="args">The arguments.</param>
        [MethodImpl((MethodImplOptions)0x0100)]
        public static void Send(string source, LogLevel level, Exception ex, XT content, params object[] args)
        {
            Send(new LogMessage(source, DateTime.Now, level, ex, content, args));
        }

        /// <summary>
        /// Waits until all notifications are sent.
        /// </summary>
        public static void Flush()
        {
            while (true)
            {
                Thread.Sleep(1);
                lock (masterSync)
                {
                    if (distributeWorkers.Count == 0)
                    {
                        return;
                    }

                    if (!masterIdle)
                    {
                        continue;
                    }
                }
                lock (distributeWorkers)
                {
                    if (distributeWorkers.Values.Any(w => !w.Idle))
                    {
                        continue;
                    }

                    lock (masterSync)
                    {
                        if (!masterIdle)
                        {
                            continue;
                        }
                    }
                }

                // all idle
                break;
            }
        }

        /// <summary>
        /// Closes all receivers, does not flush or wait.
        /// </summary>
        public static void CloseAll()
        {
            DistributeWorker[] workers;
            lock (distributeWorkers)
            {
                workers = distributeWorkers.Values.ToArray();
                distributeWorkers.Clear();
            }
            foreach (DistributeWorker worker in workers)
            {
                worker.Close();
            }
        }

        #region static string logging methods

        /// <summary>(8) Transmits a <see cref="LogLevel.Verbose" /> message.</summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogVerbose(string source, XT msg, params object[] args)
        {
            Send(source, LogLevel.Verbose, null, msg, args);
        }

        /// <summary>
        /// (7) Transmits a <see cref="LogLevel.Debug"/> message.
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogDebug(string source, XT msg, params object[] args)
        {
            Send(source, LogLevel.Debug, null, msg, args);
        }

        /// <summary>
        /// (6) Transmits a <see cref="LogLevel.Information"/> message.
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogInfo(string source, XT msg, params object[] args)
        {
            Send(source, LogLevel.Information, null, msg, args);
        }

        /// <summary>
        /// (5) Transmits a <see cref="LogLevel.Notice"/> message.
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogNotice(string source, XT msg, params object[] args)
        {
            Send(source, LogLevel.Notice, null, msg, args);
        }

        /// <summary>
        /// (4) Transmits a <see cref="LogLevel.Warning"/> message.
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogWarning(string source, XT msg, params object[] args)
        {
            Send(source, LogLevel.Warning, null, msg, args);
        }

        /// <summary>
        /// (3) Transmits a <see cref="LogLevel.Error"/> message.
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogError(string source, XT msg, params object[] args)
        {
            Send(source, LogLevel.Error, null, msg, args);
        }

        /// <summary>
        /// (2) Transmits a <see cref="LogLevel.Critical"/> message.
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogCritical(string source, XT msg, params object[] args)
        {
            Send(source, LogLevel.Critical, null, msg, args);
        }

        /// <summary>
        /// (1) Transmits a <see cref="LogLevel.Alert"/> message.
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogAlert(string source, XT msg, params object[] args)
        {
            Send(source, LogLevel.Alert, null, msg, args);
        }

        /// <summary>
        /// (0) Transmits a <see cref="LogLevel.Emergency"/> message.
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogEmergency(string source, XT msg, params object[] args)
        {
            Send(source, LogLevel.Emergency, null, msg, args);
        }

        /// <summary>
        /// (7) Transmits a <see cref="LogLevel.Debug"/> message.
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogDebug(string source, Exception ex, XT msg, params object[] args)
        {
            Send(source, LogLevel.Debug, ex, msg, args);
        }

        /// <summary>
        /// (8) Transmits a <see cref="LogLevel.Verbose"/> message.
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogVerbose(string source, Exception ex, XT msg, params object[] args)
        {
            Send(source, LogLevel.Verbose, ex, msg, args);
        }

        /// <summary>
        /// (6) Transmits a <see cref="LogLevel.Information"/> message.
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogInfo(string source, Exception ex, XT msg, params object[] args)
        {
            Send(source, LogLevel.Information, ex, msg, args);
        }

        /// <summary>
        /// (5) Transmits a <see cref="LogLevel.Notice"/> message.
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogNotice(string source, Exception ex, XT msg, params object[] args)
        {
            Send(source, LogLevel.Notice, ex, msg, args);
        }

        /// <summary>
        /// (4) Transmits a <see cref="LogLevel.Warning"/> message.
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogWarning(string source, Exception ex, XT msg, params object[] args)
        {
            Send(source, LogLevel.Warning, ex, msg, args);
        }

        /// <summary>
        /// (3) Transmits a <see cref="LogLevel.Error"/> message.
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogError(string source, Exception ex, XT msg, params object[] args)
        {
            Send(source, LogLevel.Error, ex, msg, args);
        }

        /// <summary>
        /// (2) Transmits a <see cref="LogLevel.Critical"/> message.
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogCritical(string source, Exception ex, XT msg, params object[] args)
        {
            Send(source, LogLevel.Critical, ex, msg, args);
        }

        /// <summary>
        /// (1) Transmits a <see cref="LogLevel.Alert"/> message.
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogAlert(string source, Exception ex, XT msg, params object[] args)
        {
            Send(source, LogLevel.Alert, ex, msg, args);
        }

        /// <summary>
        /// (0) Transmits a <see cref="LogLevel.Emergency"/> message.
        /// </summary>
        /// <param name="source">The source of the message.</param>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogEmergency(string source, Exception ex, XT msg, params object[] args)
        {
            Send(source, LogLevel.Emergency, ex, msg, args);
        }
        #endregion
        #endregion

        #region logger class

        /// <summary>Initializes a new instance of the <see cref="Logger"/> class.</summary>
        /// <param name="name">Name of the log source.</param>cd \\jenni
        ///
        public Logger(string name)
        {
            LogSourceName = name;
        }

        /// <summary>Gets the name of the log source.</summary>
        /// <value>The name of the log source.</value>
        public string LogSourceName { get; set; }

        #region string logging methods

        /// <summary>Writes a message with the specified level.</summary>
        /// <param name="level">The level.</param>
        /// <param name="msg">The message.</param>
        /// <param name="args">The arguments.</param>
        public void Write(LogLevel level, XT msg, params object[] args)
        {
            Write(level, msg, args);
        }

        /// <summary>Writes a message with the specified level.</summary>
        /// <param name="level">The level.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="msg">The message.</param>
        /// <param name="args">The arguments.</param>
        public void Write(LogLevel level, Exception ex, XT msg, params object[] args)
        {
            Send(LogSourceName, level, ex, msg, args);
        }

        /// <summary>(8) Transmits a <see cref="LogLevel.Verbose" /> message.</summary>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        public void LogVerbose(XT msg, params object[] args)
        {
            Send(LogSourceName, LogLevel.Verbose, null, msg, args);
        }

        /// <summary>
        /// (7) Transmits a <see cref="LogLevel.Debug"/> message.
        /// </summary>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        public void LogDebug(XT msg, params object[] args)
        {
            Send(LogSourceName, LogLevel.Debug, null, msg, args);
        }

        /// <summary>
        /// (6) Transmits a <see cref="LogLevel.Information"/> message.
        /// </summary>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        public void LogInfo(XT msg, params object[] args)
        {
            Send(LogSourceName, LogLevel.Information, null, msg, args);
        }

        /// <summary>
        /// (5) Transmits a <see cref="LogLevel.Notice"/> message.
        /// </summary>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        public void LogNotice(XT msg, params object[] args)
        {
            Send(LogSourceName, LogLevel.Notice, null, msg, args);
        }

        /// <summary>
        /// (4) Transmits a <see cref="LogLevel.Warning"/> message.
        /// </summary>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="args">The message arguments.</param>
        public void LogWarning(XT msg, params object[] args)
        {
            Send(LogSourceName, LogLevel.Warning, null, msg, args);
        }

        /// <summary>
        /// (3) Transmits a <see cref="LogLevel.Error"/> message.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        public void LogError(XT msg, params object[] args)
        {
            Send(LogSourceName, LogLevel.Error, null, msg, args);
        }

        /// <summary>
        /// (2) Transmits a <see cref="LogLevel.Critical"/> message.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        public void LogCritical(XT msg, params object[] args)
        {
            Send(LogSourceName, LogLevel.Critical, null, msg, args);
        }

        /// <summary>
        /// (1) Transmits a <see cref="LogLevel.Alert"/> message.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        public void LogAlert(XT msg, params object[] args)
        {
            Send(LogSourceName, LogLevel.Alert, null, msg, args);
        }

        /// <summary>
        /// (0) Transmits a <see cref="LogLevel.Emergency"/> message.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="args">The message arguments.</param>
        public void LogEmergency(XT msg, params object[] args)
        {
            Send(LogSourceName, LogLevel.Emergency, null, msg, args);
        }

        /// <summary>
        /// (7) Transmits a <see cref="LogLevel.Debug"/> message.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public void LogDebug(Exception ex, XT msg, params object[] args)
        {
            Send(LogSourceName, LogLevel.Debug, ex, msg, args);
        }

        /// <summary>
        /// (8) Transmits a <see cref="LogLevel.Verbose"/> message.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public void LogVerbose(Exception ex, XT msg, params object[] args)
        {
            Send(LogSourceName, LogLevel.Verbose, ex, msg, args);
        }

        /// <summary>
        /// (6) Transmits a <see cref="LogLevel.Information"/> message.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public void LogInfo(Exception ex, XT msg, params object[] args)
        {
            Send(LogSourceName, LogLevel.Information, ex, msg, args);
        }

        /// <summary>
        /// (5) Transmits a <see cref="LogLevel.Notice"/> message.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public void LogNotice(Exception ex, XT msg, params object[] args)
        {
            Send(LogSourceName, LogLevel.Notice, ex, msg, args);
        }

        /// <summary>
        /// (4) Transmits a <see cref="LogLevel.Warning"/> message.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public void LogWarning(Exception ex, XT msg, params object[] args)
        {
            Send(LogSourceName, LogLevel.Warning, ex, msg, args);
        }

        /// <summary>
        /// (3) Transmits a <see cref="LogLevel.Error"/> message.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public void LogError(Exception ex, XT msg, params object[] args)
        {
            Send(LogSourceName, LogLevel.Error, ex, msg, args);
        }

        /// <summary>
        /// (2) Transmits a <see cref="LogLevel.Critical"/> message.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public void LogCritical(Exception ex, XT msg, params object[] args)
        {
            Send(LogSourceName, LogLevel.Critical, ex, msg, args);
        }

        /// <summary>
        /// (1) Transmits a <see cref="LogLevel.Alert"/> message.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public void LogAlert(Exception ex, XT msg, params object[] args)
        {
            Send(LogSourceName, LogLevel.Alert, ex, msg, args);
        }

        /// <summary>
        /// (0) Transmits a <see cref="LogLevel.Emergency"/> message.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        /// <param name="ex">Exception to write.</param>
        /// <param name="args">The message arguments.</param>
        public void LogEmergency(Exception ex, XT msg, params object[] args)
        {
            Send(LogSourceName, LogLevel.Emergency, ex, msg, args);
        }
        #endregion
        #endregion
    }
}
