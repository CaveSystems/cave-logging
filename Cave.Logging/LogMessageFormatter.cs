using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Cave.Logging;

/// <summary>Provides an implementation of the <see cref="ILogMessageFormatter"/> interface. This class allows to define the layout of the formatted messages.</summary>
public class LogMessageFormatter : ILogMessageFormatter
{
    #region Private Classes

    [DebuggerDisplay("{Text}")]
    class LogFormatItem
    {
        #region Public Constructors

        public LogFormatItem(string text) => Text = text;

        public LogFormatItem(LogMessageFormatter formatter, string text)
        {
            Text = text;
            var parts = text.Unbox('{', '}').Split(new[] { ':' }, 2);
            Format = parts.Length > 1 ? parts[1] : null;
            Func = parts[0].ToLowerInvariant() switch
            {
                "content" => formatter.Content,
                "datetime" or "localdatetime" => formatter.LocalDateTime,
                "date" or "localdate" => formatter.LocalDate,
                "time" or "localtime" => formatter.LocalTime,
                "utcdatetime" => formatter.UtcDateTime,
                "utcdate" => formatter.UtcDate,
                "utctime" => formatter.UtcTime,
                "sender" or "sendername" => formatter.SenderName,
                "sendertype" => formatter.SenderType,
                "file" or "sourcefile" => formatter.SourceFile,
                "line" or "sourceline" => formatter.SourceLine,
                "member" or "sourcemember" => formatter.SourceMember,
                "fullexception" => formatter.FullException,
                "debugexception" => formatter.DebugException,
                "exception" => formatter.ExceptionMessage,
                "level" => formatter.Level,
                "levelcolor" => formatter.LevelColor,
                "shortlevel" => formatter.ShortLevel,
                "levelnumber" => formatter.LevelNumber,
                _ => null,
            };
        }

        #endregion Public Constructors

        #region Public Properties

        public string? Format { get; }
        public FormatFunction? Func { get; }
        public string Text { get; }

        #endregion Public Properties
    }

    #endregion Private Classes

    #region Private Fields

    string format;

    IList<LogFormatItem> items;

    #endregion Private Fields

    #region Private Delegates

    delegate void FormatFunction(List<ILogText> list, LogMessage message, string? format);

    #endregion Private Delegates

    #region Private Methods

    void Content(List<ILogText> list, LogMessage message, string? format)
            => list.AddRange(LogText.Parse(message.Content?.ToString(format, FormatProvider) ?? "-"));

    void DebugException(List<ILogText> list, LogMessage message, string? format)
            => list.AddRange(message.Exception?.ToLogText(true) ?? LogText.Empty);

    void ExceptionMessage(IList<ILogText> list, LogMessage message, string? format) => list.Add(new LogText(message.Exception?.Message ?? "-"));

    void FullException(List<ILogText> list, LogMessage message, string? format)
            => list.AddRange(message.Exception?.ToLogText() ?? LogText.Empty);

    void Level(IList<ILogText> list, LogMessage message, string? format) => list.Add(new LogText($"{message.Level}"));

    void LevelColor(IList<ILogText> list, LogMessage message, string? format) => list.Add(new LogText(string.Empty, message.Level.GetLogLevelColor()));

    void LevelNumber(IList<ILogText> list, LogMessage message, string? format) => list.Add(new LogText($"{(int)message.Level}"));

    void LocalDate(IList<ILogText> list, LogMessage message, string? format) => list.Add(new LogText(message.DateTime.ToLocalTime().Date.ToString(format ?? DateTimeFormat, FormatProvider)));

    void LocalDateTime(IList<ILogText> list, LogMessage message, string? format) => list.Add(new LogText(message.DateTime.ToLocalTime().ToString(format ?? DateTimeFormat, FormatProvider)));

    void LocalTime(IList<ILogText> list, LogMessage message, string? format) => list.Add(new LogText(message.DateTime.ToLocalTime().TimeOfDay.ToString(format ?? DateTimeFormat, FormatProvider)));

    void ParseFormat(string value, out string format, out IList<LogFormatItem> items)
    {
        var result = new List<LogFormatItem>(value.Length);
        var i = 0;
        while (i < value.Length)
        {
            var start = value.IndexOf('{', i);
            if (start == -1) break;
            var end = value.IndexOf('}', start);
            if (end == -1) break;
            var len = end - start + 1;
            var prefixLen = start - i;

            if (prefixLen > 0)
            {
                //add text
                var text = value.Substring(i, prefixLen);
                result.Add(new LogFormatItem(text));
            }

            var tag = value.Substring(start, len);
            result.Add(new LogFormatItem(this, tag));
            i = end + 1;
        }
        if (i < value.Length)
        {
            result.Add(new LogFormatItem(value.Substring(i)));
        }
        items = result.AsReadOnly();
        format = value;
    }

    void SenderName(IList<ILogText> list, LogMessage message, string? format)
            => list.Add(new LogText(message.SenderName));

    void SenderType(IList<ILogText> list, LogMessage message, string? format) => list.Add(new LogText(format switch
    {
        "F" or "f" => message.SenderType?.FullName ?? string.Empty,
        "A" or "a" => message.SenderType?.AssemblyQualifiedName ?? string.Empty,
        null or "" or _ => message.SenderType?.Name ?? string.Empty,
    }));

    void ShortLevel(IList<ILogText> list, LogMessage message, string? format)
            => list.Add(new LogText(message.Level.ToString().Substring(0, 1)));

    void SourceFile(IList<ILogText> list, LogMessage message, string? format)
            => list.Add(new LogText(message.SourceFile ?? "-"));

    void SourceLine(IList<ILogText> list, LogMessage message, string? format)
            => list.Add(new LogText(message.SourceLine.ToString()));

    void SourceMember(IList<ILogText> list, LogMessage message, string? format) => list.Add(new LogText(message.SourceMember ?? "-"));

    void UtcDate(IList<ILogText> list, LogMessage message, string? format) => list.Add(new LogText(message.DateTime.ToUniversalTime().Date.ToString(format ?? DateTimeFormat, FormatProvider)));

    void UtcDateTime(IList<ILogText> list, LogMessage message, string? format) => list.Add(new LogText(message.DateTime.ToUniversalTime().ToString(format ?? DateTimeFormat, FormatProvider)));

    void UtcTime(IList<ILogText> list, LogMessage message, string? format) => list.Add(new LogText(message.DateTime.ToUniversalTime().TimeOfDay.ToString(format ?? DateTimeFormat)));

    #endregion Private Methods

    #region Public Fields

    /// <summary>Default message format without colors and style.</summary>
    public const string Default = "{DateTime}: {Level} {Sender}> {Content}\n";

    /// <summary>Default message format using colors.</summary>
    public const string DefaultColored = "<inverse>{LevelColor}{DateTime} {Level} {Sender}<reset>> {Content}\n";

    /// <summary>Extended message format without colors and style.</summary>
    public const string Extended = "{DateTime}: {Level} {Sender}> '{Content}' @{SourceFile}({SourceLine}): {SourceMember}\n";

    /// <summary>Extended message format using colors.</summary>
    public const string ExtendedColored = "<inverse>{LevelColor}{DateTime} {Level} {Sender}<reset>> '{Content}' @<inverse><blue>{SourceFile}({SourceLine}): {SourceMember}\n";

    /// <summary>Short message format without colors and style.</summary>
    public const string Short = "{ShortLevel} {DateTime} {Sender}> {Content}\n";

    /// <summary>Short message format using colors.</summary>
    public const string ShortColored = "<inverse>{LevelColor}{ShortLevel} {DateTime} {Sender}<reset>> {Content}\n";

    #endregion Public Fields

    #region Public Constructors

    /// <summary>Creates a new instance using the <see cref="Default"/> message format. It can be changed using the <see cref="MessageFormat"/> property.</summary>
    public LogMessageFormatter() => ParseFormat(Default, out format, out items);

    #endregion Public Constructors

    #region Public Properties

    /// <inheritdoc/>
    public string DateTimeFormat { get; set; } = StringExtensions.DisplayDateTimeFormat;

    /// <inheritdoc/>
    public LogExceptionMode ExceptionMode { get; set; } = LogExceptionMode.Full;

    /// <inheritdoc/>
    public IFormatProvider FormatProvider { get; set; } = CultureInfo.CurrentCulture;

    /// <inheritdoc/>
    public string MessageFormat
    {
        get => format;
        set => ParseFormat(value, out format, out items);
    }

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc/>
    public virtual IList<ILogText> FormatMessage(LogMessage message)
    {
        List<ILogText> result = new();
        foreach (var item in items)
        {
            if (item.Func != null)
            {
                item.Func(result, message, item.Format);
                continue;
            }
            if (item.Text is not null)
            {
                result.AddRange(LogText.Parse(item.Text));
            }
        }
        return result.AsReadOnly();
    }

    #endregion Public Methods
}
