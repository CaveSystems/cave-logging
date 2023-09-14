using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Cave.IO;

namespace Cave.Logging;

/// <summary>Provides extension functions for <see cref="LogText"/>.</summary>
public static class LogExtensions
{
    #region Public Methods

    /// <summary>Obtains the color of a specified loglevel.</summary>
    /// <param name="level"></param>
    /// <returns></returns>
    public static LogColor GetLogLevelColor(this LogLevel level) => level switch
    {
        LogLevel.Emergency or LogLevel.Alert or LogLevel.Critical => LogColor.Magenta,
        LogLevel.Error => LogColor.Red,
        LogLevel.Warning => LogColor.Yellow,
        LogLevel.Notice => LogColor.Green,
        LogLevel.Information => LogColor.Cyan,
        LogLevel.Debug => LogColor.Gray,
        _ => LogColor.Blue,
    };

    /// <summary>Gets the plain text of the specified log text items</summary>
    /// <param name="items">Items to get text from.</param>
    /// <param name="newLineMode">
    /// (Optional) new line mode. Default is to keep all newlines with local system newline characters. Set this to <see cref="NewLineMode.Undefined"/> or 0 to
    /// remove all newline characters.
    /// </param>
    /// <returns>Returns a new string with the plain text content.</returns>
    public static string GetPlainText(this IEnumerable<ILogText> items, NewLineMode? newLineMode = null)
    {
        StringBuilder sb = new();
        foreach (var item in items)
        {
            if (ReferenceEquals(item, LogText.NewLine))
            {
                switch (newLineMode)
                {
                    default: continue;
                    case null: sb.AppendLine(); continue;
                    case NewLineMode.CR: sb.Append('\r'); continue;
                    case NewLineMode.LF: sb.Append('\n'); continue;
                    case NewLineMode.CRLF: sb.Append("\r\n"); continue;
                }
            }
            sb.Append(item.Text);
        }
        return sb.ToString();
    }

    /// <summary>Gets the plain text of the specified log text items</summary>
    /// <param name="items">Items to get text from.</param>
    /// <returns>Returns a new array of plain text lines.</returns>
    public static string[] GetPlainTextLines(this IEnumerable<ILogText> items)
    {
        List<string> lines = new(64);
        StringBuilder sb = new();
        foreach (var item in items)
        {
            if (ReferenceEquals(item, LogText.NewLine))
            {
                lines.Add(sb.ToString());
                sb = new();
                continue;
            }
            sb.Append(item.Text);
        }
        if (sb.Length > 0) lines.Add(sb.ToString());
        return lines.ToArray();
    }

    /// <summary>Retrieves the <see cref="LogText"/> instance as HTML.</summary>
    /// <param name="items">The extended text.</param>
    /// <param name="formatProvider"></param>
    /// <returns>Returns the html code.</returns>
    public static string ToHtml(this IEnumerable<ILogText> items, IFormatProvider formatProvider)
    {
        var target = new StringBuilder();
        target.Append("<html>");
        target.Append("<body>");
        items.WriteHtml(target, formatProvider);
        target.Append("</body>");
        target.Append("</html>");
        return target.ToString();
    }

    /// <summary>Gets the eXtended Text for a KeyValuePair.</summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="item">The item.</param>
    /// <returns>Returns a new <see cref="LogText"/> instance.</returns>
    public static ILogText ToLogText<TKey, TValue>(this KeyValuePair<TKey, TValue> item) => new LogText($"{item.Key}={item.Value}");

    /// <summary>Gets the eXtended Text for a KeyValuePair enumeration.</summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="items">The items.</param>
    /// <returns>Returns a new <see cref="LogText"/> instance.</returns>
    public static IEnumerable<ILogText> ToLogText<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> items) => items.Select(i => i.ToLogText());

    /// <summary>Gets the eXtended Text for a value.</summary>
    /// <param name="timeSpan">The time span.</param>
    /// <returns>Returns a new <see cref="LogText"/> instance.</returns>
    public static ILogText ToLogText(this TimeSpan timeSpan) =>
        timeSpan.Ticks > 0
            ? new LogText($"{timeSpan.FormatTime()}", LogColor.Cyan)
            : timeSpan.Ticks < 0
                ? new LogText(timeSpan.FormatTime(), LogColor.Magenta, LogStyle.Unchanged)
                : new LogText(timeSpan.FormatTime(), LogColor.White, LogStyle.Unchanged);

    /// <summary>Converts a exception to a loggable text message.</summary>
    /// <param name="ex">The <see cref="Exception"/>.</param>
    /// <param name="debug">Include debug information (stacktrace, data).</param>
    /// <returns>Returns a new <see cref="LogText"/> instance.</returns>
    public static IEnumerable<ILogText> ToLogText(this Exception ex, bool debug = false)
    {
        // ignore AggregateException
        if (ex is AggregateException && ex.InnerException is not null)
        {
            return ToLogText(ex.InnerException, debug);
        }

        if (ex is null)
        {
            return new[] { new LogText(string.Empty) };
        }

        var result = new List<ILogText>();
        var spacer = new LogText("  ", 0, LogStyle.Unchanged);
        result.Add(new LogText(ex.GetType().Name, LogColor.Red, LogStyle.Bold));
        if (debug)
        {
            result.Add(LogText.NewLine);
            result.Add(new LogText("Message:", LogColor.White, LogStyle.Bold));
            result.Add(LogText.NewLine);
        }
        else
        {
            result.Add(new LogText(": "));
        }

        foreach (var str in ex.Message.SplitNewLine())
        {
            if (str.Trim().Length == 0)
            {
                continue;
            }

            if (debug)
            {
                result.Add(spacer);
            }

            result.Add(new LogText(str, 0, LogStyle.Unchanged));
            result.Add(LogText.NewLine);
        }

        if (debug)
        {
            if (!string.IsNullOrEmpty(ex.Source))
            {
                result.Add(new LogText("Source:", LogColor.White, LogStyle.Bold));
                result.Add(LogText.NewLine);
                foreach (var str in ex.Source.SplitNewLine())
                {
                    if ((str.Trim().Length == 0) || !ASCII.IsClean(str))
                    {
                        continue;
                    }

                    result.Add(spacer);
                    result.Add(new LogText(str, 0, LogStyle.Unchanged));
                    result.Add(LogText.NewLine);
                }
            }

            if (ex.Data.Count > 0)
            {
                result.Add(new LogText("Data:", LogColor.White, LogStyle.Bold));
                result.Add(LogText.NewLine);
                foreach (var key in ex.Data.Keys)
                {
                    result.Add(new LogText($"  {key}: {ex.Data[key!]}\n", 0, LogStyle.Unchanged));
                    result.Add(LogText.NewLine);
                }
            }

            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                result.Add(new LogText("StackTrace:", LogColor.White, LogStyle.Bold));
                result.Add(LogText.NewLine);
                foreach (var str in ex.StackTrace.SplitNewLine())
                {
                    if ((str.Trim().Length == 0) || !ASCII.IsClean(str))
                    {
                        continue;
                    }

                    result.Add(spacer);
                    result.Add(new LogText(str, 0, LogStyle.Unchanged));
                    result.Add(LogText.NewLine);
                }
            }
        }

        if (ex.InnerException is not null)
        {
            if (debug)
            {
                result.Add(new LogText("---", LogColor.White, LogStyle.Bold));
                result.Add(LogText.NewLine);
            }

            result.AddRange(ToLogText(ex.InnerException, debug));
        }

        if (ex is ReflectionTypeLoadException rtle && rtle.LoaderExceptions != null)
        {
            foreach (var inner in rtle.LoaderExceptions)
            {
                if (inner is null) continue;
                if (debug)
                {
                    result.Add(new LogText("---", LogColor.White, LogStyle.Bold));
                    result.Add(LogText.NewLine);
                }

                result.AddRange(ToLogText(inner, debug));
            }
        }

        return result;
    }

    /// <summary>Writes the <see cref="LogText"/> instance as HTML to the specified <see cref="StringBuilder"/>.</summary>
    /// <param name="logTextItems">The log text items.</param>
    /// <param name="target">The target to write to.</param>
    /// <param name="formatProvider"></param>
    public static void WriteHtml(this IEnumerable<ILogText> logTextItems, StringBuilder target, IFormatProvider formatProvider)
    {
        var color = (LogColor)0;
        var style = LogStyle.Unchanged;
        foreach (var logText in logTextItems)
        {
            if (logText.Color != color)
            {
                // close old span
                if (color != 0)
                {
                    target.Append("</span>");
                }

                // set new color
                color = logText.Color;

                // open new span
                if (color != 0)
                {
                    color = LogColor.Gray;
                    target.Append($"<span style=\"color:{color.ToString().ToLower()}\">");
                }
            }

            if (logText.Style != style)
            {
                // close old tags
                if (style != LogStyle.Unchanged)
                {
                    if (style.HasFlag(LogStyle.Strikeout))
                    {
                        target.Append("</span>");
                    }

                    if (style.HasFlag(LogStyle.Underline))
                    {
                        target.Append("</span>");
                    }

                    if (style.HasFlag(LogStyle.Italic))
                    {
                        target.Append("</em>");
                    }

                    if (style.HasFlag(LogStyle.Bold))
                    {
                        target.Append("</strong>");
                    }
                }

                // set new style
                style = logText.Style;

                // open new span
                if (style.HasFlag(LogStyle.Bold))
                {
                    target.Append("<strong>");
                }

                if (style.HasFlag(LogStyle.Italic))
                {
                    target.Append("<em>");
                }

                if (style.HasFlag(LogStyle.Underline))
                {
                    target.Append("<span style=\"text-decoration:underline;\">");
                }

                if (style.HasFlag(LogStyle.Strikeout))
                {
                    target.Append("<span style=\"text-decoration:line-through;\">");
                }
            }

            if (logText.Text is not null)
            {
                target.Append(logText.Text.ReplaceNewLine("<br/>"));
            }
        }
    }

    #endregion Public Methods
}
