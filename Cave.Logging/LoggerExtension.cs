using System.Runtime.CompilerServices;

namespace Cave.Logging;

/// <summary>Provides extension to the <see cref="Logger"/> class.</summary>
public static class LoggerExtension
{
    #region Public Methods

    /// <summary>Transmits a specified content as log message.</summary>
    /// <param name="logger">Logger to use as sender</param>
    /// <param name="level">Level to use for the new message</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public static void Log(this Logger logger, LogLevel level, string content, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        => Logger.Send(new(logger.SenderName, logger.SenderType, level, $"{content}", exception: null, member, file, line));

    #endregion Public Methods
}
