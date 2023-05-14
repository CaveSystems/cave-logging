using System;
using System.Runtime.CompilerServices;

namespace Cave.Logging;

/// <summary>Provides an immutable log message.</summary>
public sealed class LogMessage
{
    #region Constructors

    /// <summary>Initializes a new instance of the <see cref="LogMessage"/> class.</summary>
    /// <param name="sender">Sender of the message.</param>
    /// <param name="level">The level.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <param name="member">Optional: method or property name of the sender.</param>
    /// <param name="file">Optional: file path at which the message was created at the time of compile.</param>
    /// <param name="line">Optional: the line number in the source file at which the message was created.</param>
    public LogMessage(string sender, LogLevel level, Exception? exception, LogText? message, [CallerMemberName] string? member = null, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
    {
        Sender = sender;
        Message = message;
        Exception = exception;
        Level = level;
        SourceMember = member;
        SourceFile = file;
        SourceLine = line;
    }

    #endregion Constructors

    #region Properties

    /// <summary>Gets the sender.</summary>
    public string Sender { get; }

    /// <summary>Gets the date time.</summary>
    public DateTime DateTime { get; } = DateTime.Now;

    /// <summary>Gets the level.</summary>
    public LogLevel Level { get; }

    /// <summary>Gets the exception.</summary>
    public Exception? Exception { get; }

    /// <summary>Gets the message.</summary>
    public LogText? Message { get; }

    /// <summary>Gets the method or property name of the sender.</summary>
    /// <remarks>This might be null when running obfuscated binaries.</remarks>
    public string? SourceMember { get; }

    /// <summary>Gets file path at which the message was created at the time of compile.</summary>
    /// <remarks>This might be null when running obfuscated binaries.</remarks>
    public string? SourceFile { get; }

    /// <summary>Gets the line number in the source file at which the message was created.</summary>
    /// <remarks>This might be null when running obfuscated binaries.</remarks>
    public int SourceLine { get; }

    #endregion Properties
}