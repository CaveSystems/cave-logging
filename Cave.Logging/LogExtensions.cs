using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Cave.Logging;

/// <summary>Provides extension functions for <see cref="LogText"/>.</summary>
public static class LogExtensions
{
    #region Static

    /// <summary>Retrieves the <see cref="LogText"/> instance as HTML.</summary>
    /// <param name="items">The extended text.</param>
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
        var spacer = new LogText("  ", LogColor.Default, LogStyle.Unchanged);
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

            result.Add(new LogText(str, LogColor.Default, LogStyle.Unchanged));
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
                    result.Add(new LogText(str, LogColor.Default, LogStyle.Unchanged));
                    result.Add(LogText.NewLine);
                }
            }

            if (ex.Data.Count > 0)
            {
                result.Add(new LogText("Data:", LogColor.White, LogStyle.Bold));
                result.Add(LogText.NewLine);
                foreach (var key in ex.Data.Keys)
                {
                    result.Add(new LogText($"  {key}: {ex.Data[key]}\n", LogColor.Default, LogStyle.Unchanged));
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
                    result.Add(new LogText(str, LogColor.Default, LogStyle.Unchanged));
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

        if (ex is ReflectionTypeLoadException exception)
        {
            foreach (var inner in exception.LoaderExceptions)
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
    public static void WriteHtml(this IEnumerable<ILogText> logTextItems, StringBuilder target, IFormatProvider formatProvider)
    {
        var color = LogColor.Default;
        var style = LogStyle.Unchanged;
        foreach (var logText in logTextItems)
        {
            if (logText.Color != color)
            {
                // close old span
                if (color != LogColor.Default)
                {
                    target.Append("</span>");
                }

                // set new color
                color = logText.Color;

                // open new span
                if (color != LogColor.Default)
                {
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

    #endregion Static
}
