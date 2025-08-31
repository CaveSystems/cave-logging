using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Cave.Logging;

/// <summary>Provides an immutable log message.</summary>
[DebuggerDisplay("LogMessage: {ToString()}")]
public sealed class LogMessage
{
    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="LogMessage"/> class.</summary>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    public LogMessage([CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
    {
        SenderName = "Unknown";
        DateTime = MonotonicTime.Now;
        Level = LogLevel.Verbose;
        SourceMember = member;
        SourceFile = file;
        SourceLine = line;
    }

    /// <summary>Initializes an old instance of the <see cref="LogMessage"/> class.</summary>
    /// <param name="dateTime">The date and time the message was created.</param>
    /// <param name="senderName">Sender name of the message.</param>
    /// <param name="senderType">Sender type of the message (optional).</param>
    /// <param name="level">The level.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    public LogMessage(DateTime dateTime, string senderName, Type senderType, LogLevel level, IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
    {
        DateTime = dateTime;
        SenderName = senderName;
        SenderType = senderType;
        Content = content;
        Exception = exception;
        Level = level;
        SourceMember = member;
        SourceFile = file;
        SourceLine = line;
    }

    /// <summary>Initializes a new instance of the <see cref="LogMessage"/> class.</summary>
    /// <param name="sender">Sender of the message.</param>
    /// <param name="level">The level.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    public LogMessage(Logger sender, LogLevel level, IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
    {
        DateTime = MonotonicTime.Now;
        SenderName = sender.SenderName;
        SenderType = sender.SenderType;
        SenderSource = sender.SenderSource;
        Content = content;
        Exception = exception;
        Level = level;
        SourceMember = member;
        SourceFile = file;
        SourceLine = line;
    }

    /// <summary>Initializes a new instance of the <see cref="LogMessage"/> class.</summary>
    /// <param name="senderName">Sender name of the message.</param>
    /// <param name="senderType">Sender type of the message (optional).</param>
    /// <param name="level">The level.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="content">The message content.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    public LogMessage(string senderName, Type? senderType, LogLevel level, IFormattable content, Exception? exception = null, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
    {
        DateTime = MonotonicTime.Now;
        SenderName = senderName;
        SenderType = senderType;
        Content = content;
        Exception = exception;
        Level = level;
        SourceMember = member;
        SourceFile = file;
        SourceLine = line;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets or sets the default formatter used at <see cref="ToString"/>. This is a global setting.</summary>
    /// <remarks>
    /// <see cref="LogReceiver"/> implementations should not rely on <see cref="ToString"/>. Instead the personal instance of <see cref="LogMessageFormatter"/>
    /// at <see cref="LogReceiver.MessageFormatter"/> should be used to format messages for the implemented receiver.
    /// </remarks>
    public static LogMessageFormatter ToStringFormatter { get; set; } = new LogMessageFormatter();

    /// <summary>Gets the current age of the message.</summary>
    public TimeSpan Age => MonotonicTime.UtcNow - DateTime.ToUniversalTime();

    /// <summary>Gets the message content.</summary>
    public IFormattable? Content { get; init; }

    /// <summary>Gets the date time.</summary>
    public DateTime DateTime { get; init; }

    /// <summary>Gets the exception.</summary>
    public Exception? Exception { get; init; }

    /// <summary>Gets the level.</summary>
    public LogLevel Level { get; init; }

    /// <summary>Gets the sender name.</summary>
    public string SenderName { get; init; }

    /// <summary>Gets the sender source.</summary>
    /// <remarks>This might be null when running obfuscated binaries.</remarks>
    public string? SenderSource { get; init; }

    /// <summary>Gets the sender type.</summary>
    public Type? SenderType { get; init; }

    /// <summary>Gets file path at which the message was created at the time of compile.</summary>
    /// <remarks>This might be null when running obfuscated binaries.</remarks>
    public string? SourceFile { get; init; }

    /// <summary>Gets the line number in the source file at which the message was created.</summary>
    /// <remarks>This might be null when running obfuscated binaries.</remarks>
    public int SourceLine { get; init; }

    /// <summary>Gets the method or property name of the sender.</summary>
    /// <remarks>This might be null when running obfuscated binaries.</remarks>
    public string? SourceMember { get; init; }

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc/>
    public override int GetHashCode() => DefaultHashingFunction.Combine(SenderName, SenderType, DateTime, Level, Exception, Content, SourceMember, SourceFile, SourceLine);

    /// <inheritdoc/>
    public override string ToString() => ToStringFormatter.FormatMessage(this).GetPlainText();

    #endregion Public Methods
}
