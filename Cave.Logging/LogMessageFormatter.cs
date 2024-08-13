using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

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
    {
        var content = message.Content;
        if (content is FormattableString formattableString)
        {
            var updatedArgs = formattableString.GetArguments().Select(argument => OnFormatArgument(message, argument)).ToArray();
            content = FormattableStringFactory.Create(formattableString.Format, updatedArgs);
        }

        if (content is IFormattable formattable)
        {
            list.AddRange(LogText.Parse(formattable.ToString(format, FormatProvider)));
        }
        else
        {
            list.AddRange(LogText.Parse("-"));
        }
    }

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
            result.Add(new LogFormatItem(value[i..]));
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
            => list.Add(new LogText(message.Level.ToString()[..1]));

    void SourceFile(IList<ILogText> list, LogMessage message, string? format)
            => list.Add(new LogText(message.SourceFile ?? "-"));

    void SourceLine(IList<ILogText> list, LogMessage message, string? format)
            => list.Add(new LogText(message.SourceLine.ToString()));

    void SourceMember(IList<ILogText> list, LogMessage message, string? format) => list.Add(new LogText(message.SourceMember ?? "-"));

    void UtcDate(IList<ILogText> list, LogMessage message, string? format) => list.Add(new LogText(message.DateTime.ToUniversalTime().Date.ToString(format ?? DateTimeFormat, FormatProvider)));

    void UtcDateTime(IList<ILogText> list, LogMessage message, string? format) => list.Add(new LogText(message.DateTime.ToUniversalTime().ToString(format ?? DateTimeFormat, FormatProvider)));

    void UtcTime(IList<ILogText> list, LogMessage message, string? format) => list.Add(new LogText(message.DateTime.ToUniversalTime().TimeOfDay.ToString(format ?? DateTimeFormat)));

    #endregion Private Methods

    #region Protected Methods

    /// <summary>Calls the <see cref="FormatArgument"/> function to format each argument of a formattable string.</summary>
    /// <param name="message">Message to be formatted.</param>
    /// <param name="argument">Argument of content part to be formatted.</param>
    /// <returns>Returns the formatted string.</returns>
    protected virtual string OnFormatArgument(LogMessage message, object? argument)
    {
        var formatter = FormatArgument;
        if (formatter != null) return formatter(message, argument);

        if (!UseArgumentColors)
        {
            if (argument is IFormattable f) return f.ToString(null, FormatProvider);
            return argument?.ToString() ?? string.Empty;
        }
        switch (argument)
        {
            case null: return $"<inverse><{message.Level.GetLogLevelColor()}>null<reset>";
            case string s: break;
            case bool b: return (b == true ? $"<green>{b.ToString(FormatProvider)}<reset>" : $"<red>{b.ToString(FormatProvider)}<reset>");
            case IFormattable f: return $"<{message.Level.GetLogLevelColor()}>{f.ToString(null, FormatProvider)}<reset>";
            case IConvertible c: try { return c.ToDouble(FormatProvider) > 0 ? $"<green>{c.ToString(FormatProvider)}<reset>" : $"<red>{c.ToString(FormatProvider)}<reset>"; } catch { break; }
        }
        return $"<{message.Level.GetLogLevelColor()}>{argument}<reset>";
    }

    #endregion Protected Methods

    #region Public Fields

    /// <summary>Default message format without colors and style.</summary>
    public const string Default = "{DateTime}: {Level} {Sender}> {Content}\n";

    /// <summary>Default message format using colors.</summary>
    public const string DefaultColored = "<inverse>{LevelColor}{DateTime} {Level} {Sender}><reset> {Content}\n";

    /// <summary>Extended message format without colors and style.</summary>
    public const string Extended = "{DateTime}: {Level} {Sender}> '{Content}' @{SourceFile}({SourceLine}): {SourceMember}\n";

    /// <summary>Extended message format using colors.</summary>
    public const string ExtendedColored = "<inverse>{LevelColor}{DateTime} {Level} {Sender}><reset> '{Content}' <inverse><blue>@{SourceFile}({SourceLine}): {SourceMember}\n";

    /// <summary>Short message format without colors and style.</summary>
    public const string Short = "{ShortLevel} {DateTime} {Sender}> {Content}\n";

    /// <summary>Short message format using colors.</summary>
    public const string ShortColored = "<inverse>{LevelColor}{ShortLevel} {DateTime} {Sender}><reset> {Content}\n";

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

    /// <summary>Provides per argument formatting for formattable strings.</summary>
    public Func<LogMessage, object?, string>? FormatArgument { get; set; }

    /// <inheritdoc/>
    public IFormatProvider FormatProvider { get; set; } = CultureInfo.CurrentCulture;

    /// <inheritdoc/>
    public string MessageFormat
    {
        get => format;
        set => ParseFormat(value, out format, out items);
    }

    /// <summary>Use colors for argument formatting</summary>
    public bool UseArgumentColors { get; set; } = true;

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
        if (message.Exception is Exception ex && ExceptionMode != LogExceptionMode.None)
        {
            result.AddRange(ex.ToLogText(ExceptionMode.HasFlag(LogExceptionMode.StackTrace), ExceptionMode.HasFlag(LogExceptionMode.IncludeChildren)));
        }
        return result.AsReadOnly();
    }

    #endregion Public Methods
}
