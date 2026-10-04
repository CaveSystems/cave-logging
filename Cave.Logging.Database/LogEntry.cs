using Cave.Data;
using Cave.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Cave.Logging.Database;

/// <summary>
/// Provides a log entry
/// </summary>
[DebuggerDisplay("{ToShortString()}")]
[Table("Logs")]
public struct LogEntry : IEquatable<LogEntry>
{
    /// <summary>Implements the operator ==.</summary>
    /// <param name="logEntry1">The log entry1.</param>
    /// <param name="logEntry2">The log entry2.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator ==(LogEntry logEntry1, LogEntry logEntry2)
    {
        return logEntry1.ID == logEntry2.ID
            && logEntry1.Content == logEntry2.Content
            && logEntry1.DateTime.ToUniversalTime() == logEntry2.DateTime.ToUniversalTime()
            && logEntry1.HostName == logEntry2.HostName
            && logEntry1.Level == logEntry2.Level
            && logEntry1.ProcessName == logEntry2.ProcessName
            && logEntry1.Source == logEntry2.Source;
    }

    /// <summary>Implements the operator !=.</summary>
    /// <param name="logEntry1">The log entry1.</param>
    /// <param name="logEntry2">The log entry2.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator !=(LogEntry logEntry1, LogEntry logEntry2)
    {
        return logEntry1.ID != logEntry2.ID
            || logEntry1.Content != logEntry2.Content
            || logEntry1.DateTime.ToUniversalTime() != logEntry2.DateTime.ToUniversalTime()
            || logEntry1.HostName != logEntry2.HostName
            || logEntry1.Level != logEntry2.Level
            || logEntry1.ProcessName != logEntry2.ProcessName
            || logEntry1.Source != logEntry2.Source;
    }

    /// <summary>Parses the specified string for a LogEntry. This can only parse the result of a LogEntry.ToString() call.</summary>
    /// <param name="text">The string to parse.</param>
    /// <returns>Returns a new logentry</returns>
    public static LogEntry Parse(string text)
    {
        if (text == null) throw new ArgumentNullException("text");
        try
        {
            var parts = text.Split('\t');
            return new LogEntry()
            {
                DateTime = DateTime.ParseExact(parts[0], StringExtensions.InteropDateTimeFormat, CultureInfo.InvariantCulture),
                Level = (LogLevel)Enum.Parse(typeof(LogLevel), parts[1]),
                HostName = parts[2],
                ProcessName = parts[3],
                Source = parts[4],
                Content = parts[5].Replace("\\r\\n", "\r\n"),
            };
        }
        catch (Exception ex)
        {
            throw new ArgumentException("The specified string does not contain a valid log entry!", "text", ex);
        }
    }

    /// <summary>Creates a new instance from a message.</summary>
    /// <param name="hostName">The name of the host.</param>
    /// <param name="msg">The message.</param>
    /// <returns>A new log entry instance.</returns>
    public static LogEntry FromMessage(string hostName, LogMessage msg)
    {
        var p = process ?? (process = Process.GetCurrentProcess());
        return new LogEntry()
        {
            Content = msg.Content?.ToString() ?? string.Empty,
            DateTime = msg.DateTime,
            HostName = hostName,
            Level = msg.Level,
            ProcessName = p.ProcessName,
            Source = msg.SenderSource ?? string.Empty,
        };
    }

    static Process? process;

    /// <summary>
    /// ID of the message
    /// </summary>
    [Field(Flags = FieldFlags.ID | FieldFlags.AutoIncrement)]
    public long ID;

    /// <summary>
    /// DateTime of the message
    /// </summary>
    [Field]
    [DateTimeFormat(DateTimeKind.Utc, DateTimeType.BigIntHumanReadable)]
    public DateTime DateTime;

    /// <summary>
    /// Provides the level of the message
    /// </summary>
    [Field(Flags = FieldFlags.Index)]
    public LogLevel Level;

    /// <summary>
    /// The system this message was sent from
    /// </summary>
    [Field(Flags = FieldFlags.Index)]
    [StringFormat(StringEncoding.ASCII)]
    public string HostName;

    /// <summary>
    /// The process that generated this message
    /// </summary>
    [Field(Flags = FieldFlags.Index)]
    [StringFormat(StringEncoding.ASCII)]
    public string ProcessName;

    /// <summary>
    /// The source of the message
    /// </summary>
    [Field(Flags = FieldFlags.Index)]
    [StringFormat(StringEncoding.ASCII)]
    public string Source;

    /// <summary>
    /// The content of the message
    /// </summary>
    [Field]
    [StringFormat(StringEncoding.UTF8)]
    public string Content;

    /// <summary>
    /// Returns a text describing the content. The output of this function can be read by <see cref="Parse(string)"/>
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append(DateTime.ToUniversalTime().ToString(StringExtensions.InteropDateTimeFormat));
        result.Append('\t');
        result.Append(Level.ToString());
        result.Append('\t');
        if (!string.IsNullOrEmpty(HostName)) result.Append(HostName.Replace('\t', ' ')); else result.Append('-');
        result.Append('\t');
        if (!string.IsNullOrEmpty(ProcessName)) result.Append(ProcessName.Replace('\t', ' ')); else result.Append('-');
        result.Append('\t');
        if (!string.IsNullOrEmpty(Source)) result.Append(Source.Replace('\t', ' ')); else result.Append('-');
        result.Append('\t');
        if (Content != null) result.Append(Content.ReplaceNewLine("\\r\\n").Replace('\t', ' '));
        return result.ToString();
    }

    /// <summary>
    /// Returns a short human readable string without any style and color information
    /// </summary>
    /// <returns></returns>
    public string ToShortString()
    {
        var result = new StringBuilder();
        result.Append(Level.ToString().ForceLength(12));
        result.Append(' ');
        if (DateTime > DateTime.MinValue)
        {
            result.Append(DateTime.ToLocalTime().ToString(StringExtensions.ShortTimeFormat));
            result.Append(' ');
        }
        if (Source != null)
        {

            result.Append(Source.Replace(':', ' '));
            result.Append(": ");
        }
        if (Content != null)
        {
            result.Append(Content.Trim());
        }
        return result.ToString();
    }

    /// <summary>Determines whether the specified <see cref="object" />, is equal to this instance.</summary>
    /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
    /// <returns><c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) => obj is LogEntry logEntry && Equals(logEntry);

    /// <summary>Determines whether the specified <see cref="LogEntry" />, is equal to this instance.</summary>
    /// <param name="other">The <see cref="LogEntry" /> to compare with this instance.</param>
    /// <returns><c>true</c> if the specified <see cref="LogEntry" /> is equal to this instance; otherwise, <c>false</c>.</returns>
    public bool Equals(LogEntry other) => other.Content == Content
            && other.DateTime.ToUniversalTime() == DateTime.ToUniversalTime()
            && other.HostName == HostName
            && other.Level == Level
            && other.ProcessName == ProcessName
            && other.Source == Source;

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hashCode = -239580956;
        hashCode = hashCode * -1521134295 + DateTime.GetHashCode();
        hashCode = hashCode * -1521134295 + Level.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(HostName);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ProcessName);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Source);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Content);
        return hashCode;
    }
}
