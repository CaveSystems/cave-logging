using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Cave.IO;

namespace Cave.Logging;

/// <summary>Use this class to write messages directly to a utf-8 logfile.</summary>
public sealed class LogFile : LogFileBase
{
    #region Private Fields

    readonly Stream? stream;

    #endregion Private Fields

    #region Public Fields

    /// <summary>Gets or sets the used file extension for the logs.</summary>
    [SuppressMessage("Usage", "CA2211", Justification = "Compatibility")]
    public static string FileExtension = ".log";

    #endregion Public Fields

    #region Public Constructors

    /// <summary>Opens the specified logfile.</summary>
    /// <param name="fileName">filename to write to</param>
    /// <param name="encoding">Encoding to use (default = utf8)</param>
    /// <param name="newLineMode">New line mode (default = lf)</param>
    /// <param name="endian">Endianess (default = little endian)</param>
    public LogFile(string fileName, StringEncoding encoding = StringEncoding.UTF_8, NewLineMode newLineMode = NewLineMode.LF, EndianType endian = EndianType.LittleEndian)
        : base(fileName)
    {
        if (stream != null)
        {
            throw new InvalidOperationException("LogFile already opened!");
        }

        var fullFilePath = Path.GetFullPath(fileName) ?? throw new ArgumentNullException(nameof(fileName));
        Log.Debug($"Prepare logging to file <cyan>{fullFilePath}");
        var folder = Path.GetDirectoryName(fullFilePath);
        if (folder is not null) Directory.CreateDirectory(folder);
        stream = File.Open(fullFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.End);
        }
        Writer = new LogFileWriter(new DataWriter(stream, encoding, newLineMode, endian));
    }

    #endregion Public Constructors

    #region Public Methods

    /// <summary>Starts a <see cref="LogFile"/> instance for the local machine.</summary>
    public static LogFile StartLocalMachineLogFile(LogFileFlags flags, string? additionalPath = null) => Start(new LogFile(GetLocalMachineLogFileName(flags, additionalPath) + FileExtension));

    /// <summary>Starts a <see cref="LogFile"/> instance for the local user.</summary>
    public static LogFile StartLocalUserLogFile(LogFileFlags flags, string? additionalPath = null) => Start(new LogFile(GetLocalUserLogFileName(flags, additionalPath) + FileExtension));

    /// <summary>Starts a new <see cref="LogFile"/> instance.</summary>
    public static LogFile StartLogFile(string fileName) => Start(new LogFile(fileName));

    /// <summary>
    /// Starts a <see cref="LogFile"/> instance for the current running program in the programs startup directory. This should only be used for administration
    /// processes. Attention do nut use this for service processes!.
    /// </summary>
    /// <returns></returns>
    public static LogFile StartProgramLogFile(LogFileFlags flags, string? additionalPath = null) => Start(new LogFile(GetProgramLogFileName(flags, additionalPath) + FileExtension));

    /// <summary>Starts a <see cref="LogFile"/> instance for the current (roaming) user.</summary>
    public static LogFile StartUserLogFile(LogFileFlags flags, string? additionalPath = null) => Start(new LogFile(GetUserLogFileName(flags, additionalPath) + FileExtension));

    /// <summary>Closes the <see cref="LogReceiver"/>.</summary>
    public override void Close()
    {
        lock (this)
        {
            Writer.Close();
        }

        base.Close();
    }

    #endregion Public Methods
}
