using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace Cave.Logging;

/// <summary>Use this class to write messages directly to a utf-8 logfile.</summary>
public sealed class LogFile : LogFileBase
{
    #region Private Fields

    Logger logger;
    StreamWriter writer;

    #endregion Private Fields

    #region Properties

    /// <summary>Gets the number of notifications logged.</summary>
    public int Counter { get; set; }

    #endregion Properties

    #region Overrides

    /// <summary>Writes the specified log message.</summary>
    /// <param name="dateTime">The date time.</param>
    /// <param name="level">The level.</param>
    /// <param name="source">The source.</param>
    /// <param name="content">The content.</param>
    protected override void Write(DateTime dateTime, LogLevel level, string source, LogText content)
    {
        var text =
            dateTime.ToString(StringExtensions.InterOpDateTimeFormat) + " " +
            level + " " +
            source + ": " +
            content.Text;
        lock (this)
        {
            if (writer == null)
            {
                return;
            }

            writer.WriteLine(text);
            writer.Flush();
            Counter++;
        }
    }

    /// <summary>Closes the <see cref="LogReceiver"/>.</summary>
    public override void Close()
    {
        lock (this)
        {
            if (writer != null)
            {
                writer.Close();
                writer = null;
            }
        }

        base.Close();
    }

    #endregion Overrides

    #region default log files

    /// <summary>Gets or sets the used file extension for the logs.</summary>
    [SuppressMessage("Usage", "CA2211", Justification = "Compatibility")]
    public static string FileExtension = ".log";

    /// <summary>Returns a <see cref="LogFile"/> instance for the local machine.</summary>
    public static LogFile CreateLocalMachineLogFile(LogFileFlags flags, string? additionalPath = null) => new(GetLocalMachineLogFileName(flags, additionalPath) + FileExtension);

    /// <summary>Returns a <see cref="LogFile"/> instance for the local user.</summary>
    public static LogFile CreateLocalUserLogFile(LogFileFlags flags, string? additionalPath = null) => new(GetLocalUserLogFileName(flags, additionalPath) + FileExtension);

    /// <summary>
    /// Returns a <see cref="LogFile"/> instance for the current running program in the programs startup directory. This should only be used for
    /// administration processes. Attention do nut use this for service processes!.
    /// </summary>
    /// <returns></returns>
    public static LogFile CreateProgramLogFile(LogFileFlags flags, string? additionalPath = null) => new(GetProgramLogFileName(flags, additionalPath) + FileExtension);

    /// <summary>Returns a <see cref="LogFile"/> instance for the current (roaming) user.</summary>
    public static LogFile CreateUserLogFile(LogFileFlags flags, string? additionalPath = null) => new(GetUserLogFileName(flags, additionalPath) + FileExtension);

    #endregion default log files

    #region constructors

    void Init(string fileName)
    {
        if (writer != null)
        {
            throw new InvalidOperationException("LogFile already opened!");
        }

        var fullFilePath = Path.GetFullPath(fileName);
        logger = new Logger($"LogFile:{Path.GetFileName(fileName)}");
        logger.Debug("Prepare logging to file <cyan>{0}", fullFilePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullFilePath));
        Stream stream = File.Open(fullFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.End);
        }

        writer = new StreamWriter(stream, Encoding.UTF8);
    }

    /// <summary>Opens the specified logfile.</summary>
    /// <param name="fileName"></param>
    public LogFile(string fileName)
        : base(fileName) =>
        Init(fileName);

    /// <summary>Opens the specified logfile.</summary>
    /// <param name="level">The LogLevel to use initially.</param>
    /// <param name="fileName"></param>
    public LogFile(LogLevel level, string fileName)
        : base(fileName)
    {
        Init(fileName);
        Level = level;
    }

    #endregion constructors
}
