using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace Cave.Logging;

/// <summary>Provides a log receiver using the posix syslog (libc) implementation.</summary>
public sealed class LogSyslog : LogReceiver
{
    #region Private Fields

    static readonly object SyncRoot = new();
    static LogSyslog? instance;

    #endregion Private Fields

    #region Private Constructors

    /// <summary>Initializes a new instance of the <see cref="LogSyslog"/> class.</summary>
    LogSyslog() { }

    #endregion Private Constructors

    #region Public Properties

    /// <summary>Gets or sets the message encoding culture.</summary>
    public CultureInfo CultureInfo { get; set; } = CultureInfo.InvariantCulture;

    /// <summary>Gets or sets facility to use.</summary>
    public SyslogFacility Facility { get; set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Creates a new instance.</summary>
    /// <returns>The new syslog instance.</returns>
    /// <exception cref="Exception">Only one instance allowed!.</exception>
    public static LogSyslog Create()
    {
        lock (SyncRoot)
        {
            if (instance == null)
            {
                Syslog.Init();
                instance = new LogSyslog();
                instance.Start();
                new Logger().Debug($"Start logging to libc:Syslog");
            }
            else
            {
                throw new Exception("Only one instance allowed!");
            }
            return instance;
        }
    }

    /// <summary>Closes the <see cref="T:Cave.Logging.LogReceiver"/>.</summary>
    public override void Close()
    {
        base.Close();
        lock (SyncRoot)
        {
            instance?.Close();
            instance = null;
            Syslog.Close();
        }
    }

    /// <summary>Writes a message directly to the syslog. This does not use <see cref="LogReceiver.MessageFormatter"/> nor <see cref="LogReceiver.Writer"/>.</summary>
    /// <param name="message"></param>
    public override void Write(LogMessage message)
    {
        var severity = (SyslogSeverity)Math.Min((int)message.Level, (int)SyslogSeverity.Debug);

        void WriteLines(string lines)
        {
            foreach (var line in lines.SplitNewLine())
            {
                if (line.Trim().Length == 0) continue;
                Syslog.Write(severity, Facility, $"{message.SenderName}: {line}");
            }
        }

        var debugInfo = (Level >= LogLevel.Debug);
        var content = message.Content?.ToString(null, CultureInfo);
        if (content != null)
        {
            WriteLines(content);
        }
        if (message.Exception is Exception ex)
        {
            WriteLines(ex.ToText(debugInfo));
        }
    }

    #endregion Public Methods
}
