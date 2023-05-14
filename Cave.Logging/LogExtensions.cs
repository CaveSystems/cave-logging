using System;
using System.Collections.Generic;
using System.Globalization;
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
    public static string ToHtml(this IEnumerable<ILogText> items, CultureInfo cultureInfo)
    {
        var sb = new StringBuilder();
        sb.Append("<html>");
        sb.Append("<body>");
        items.WriteHtml(sb, cultureInfo);
        sb.Append("</body>");
        sb.Append("</html>");
        return sb.ToString();
    }

    /// <summary>Retrieves the <see cref="LogText"/> instance as HTML.</summary>
    /// <param name="items">The extended text.</param>
    /// <returns>Returns the html code.</returns>
    public static string ToHtml(this IEnumerable<LogText> items, CultureInfo cultureInfo)
    {
        var sb = new StringBuilder();
        sb.Append("<html>");
        sb.Append("<body>");
        items.WriteHtml(sb, cultureInfo);
        sb.Append("</body>");
        sb.Append("</html>");
        return sb.ToString();
    }

    /// <summary>Gets the eXtended Text for a KeyValuePair.</summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="item">The item.</param>
    /// <returns>Returns a new <see cref="LogText"/> instance.</returns>
    public static LogText ToLogText<TKey, TValue>(this KeyValuePair<TKey, TValue> item) => new LogText($"{item.Key}={item.Value}");

    /// <summary>Gets the eXtended Text for a KeyValuePair enumeration.</summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="items">The items.</param>
    /// <returns>Returns a new <see cref="LogText"/> instance.</returns>
    public static LogText ToLogText<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        var x = new LogTextBuilder();
        foreach (var item in items)
        {
            x.Append(item.ToLogText());
        }

        return x;
    }

    /// <summary>Gets the eXtended Text for a value.</summary>
    /// <param name="timeSpan">The time span.</param>
    /// <returns>Returns a new <see cref="LogText"/> instance.</returns>
    public static LogText ToLogText(this TimeSpan timeSpan) =>
        timeSpan.Ticks > 0
            ? new LogText($"{timeSpan.FormatTime()}", LogColor.Cyan)
            : timeSpan.Ticks < 0
                ? new LogText(timeSpan.FormatTime(), LogColor.Magenta, LogStyle.Unchanged)
                : new LogText(timeSpan.FormatTime(), LogColor.White, LogStyle.Unchanged);

    /// <summary>Converts a exception to a loggable text message.</summary>
    /// <param name="ex">The <see cref="Exception"/>.</param>
    /// <param name="debug">Include debug information (stacktrace, data).</param>
    /// <returns>Returns a new <see cref="LogText"/> instance.</returns>
    public static LogText ToLogText(this Exception ex, bool debug = false)
    {
        // ignore AggregateException
        if (ex is AggregateException)
        {
            return ToLogText(ex.InnerException, debug);
        }

        if (ex == null)
        {
            return new LogText(string.Empty);
        }

        var result = new List<LogTextItem>();
        var spacer = new LogTextItem(LogColor.Unchanged, LogStyle.Default, "  ");
        result.Add(new LogTextItem(ex.GetType().Name, LogColor.Red, LogStyle.Bold));
        if (debug)
        {
            result.Add(LogTextItem.NewLine);
            result.Add(new LogTextItem("Message:", LogColor.White, LogStyle.Bold));
            result.Add(LogTextItem.NewLine);
        }
        else
        {
            result.Add(new LogTextItem(": "));
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

            result.Add(new LogTextItem(str, LogColor.Unchanged, LogStyle.Unchanged));
            result.Add(LogTextItem.NewLine);
        }

        if (debug)
        {
            if (!string.IsNullOrEmpty(ex.Source))
            {
                result.Add(new LogTextItem("Source:", LogColor.White, LogStyle.Bold));
                result.Add(LogTextItem.NewLine);
                foreach (var str in ex.Source.SplitNewLine())
                {
                    if ((str.Trim().Length == 0) || !ASCII.IsClean(str))
                    {
                        continue;
                    }

                    result.Add(spacer);
                    result.Add(new LogTextItem(str, LogColor.Unchanged, LogStyle.Unchanged));
                    result.Add(LogTextItem.NewLine);
                }
            }

            if (ex.Data.Count > 0)
            {
                result.Add(new LogTextItem("Data:", LogColor.White, LogStyle.Bold));
                result.Add(LogTextItem.NewLine);
                foreach (var key in ex.Data.Keys)
                {
                    result.Add(new LogTextItem($"  {key}: {ex.Data[key]}\n", LogColor.Unchanged, LogStyle.Unchanged));
                    result.Add(LogTextItem.NewLine);
                }
            }

            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                result.Add(new LogTextItem("StackTrace:", LogColor.White, LogStyle.Bold));
                result.Add(LogTextItem.NewLine);
                foreach (var str in ex.StackTrace.SplitNewLine())
                {
                    if ((str.Trim().Length == 0) || !ASCII.IsClean(str))
                    {
                        continue;
                    }

                    result.Add(spacer);
                    result.Add(new LogTextItem(str, LogColor.Unchanged, LogStyle.Unchanged));
                    result.Add(LogTextItem.NewLine);
                }
            }
        }

        if (ex.InnerException != null)
        {
            if (debug)
            {
                result.Add(new LogTextItem("---", LogColor.White, LogStyle.Bold));
                result.Add(LogTextItem.NewLine);
            }

            result.AddRange(ToLogText(ex.InnerException, debug).Items);
        }

        if (ex is ReflectionTypeLoadException exception)
        {
            foreach (var inner in exception.LoaderExceptions)
            {
                if (debug)
                {
                    result.Add(new LogTextItem("---", LogColor.White, LogStyle.Bold));
                    result.Add(LogTextItem.NewLine);
                }

                result.AddRange(ToLogText(inner, debug).Items);
            }
        }

        while ((result.Count > 0) && (result[result.Count - 1] == LogTextItem.NewLine))
        {
            result.RemoveAt(result.Count - 1);
        }

        return new LogText(result.ToArray());
    }

    /// <summary>Writes the <see cref="LogText"/> instance as HTML to the specified <see cref="StringBuilder"/>.</summary>
    /// <param name="x">The extended text.</param>
    /// <param name="sb">The StringBuilder.</param>
    public static void WriteHtml(this LogText x, StringBuilder sb, CultureInfo cultureInfo)
    {
        var color = LogColor.Unchanged;
        var style = LogStyle.Unchanged;
        foreach (var item in x.Items)
        {
            if (item.Color != color)
            {
                // close old span
                if (color != LogColor.Unchanged)
                {
                    sb.Append("</span>");
                }

                // set new color
                color = item.Color;

                // open new span
                if (color != LogColor.Unchanged)
                {
                    sb.Append($"<span style=\"color:{color.ToString().ToLower()}\">");
                }
            }

            if (item.Style != style)
            {
                // close old tags
                if (style != LogStyle.Unchanged)
                {
                    if (style.HasFlag(LogStyle.Strikeout))
                    {
                        sb.Append("</span>");
                    }

                    if (style.HasFlag(LogStyle.Underline))
                    {
                        sb.Append("</span>");
                    }

                    if (style.HasFlag(LogStyle.Italic))
                    {
                        sb.Append("</em>");
                    }

                    if (style.HasFlag(LogStyle.Bold))
                    {
                        sb.Append("</strong>");
                    }
                }

                // set new style
                style = item.Style;

                // open new span
                if (style.HasFlag(LogStyle.Bold))
                {
                    sb.Append("<strong>");
                }

                if (style.HasFlag(LogStyle.Italic))
                {
                    sb.Append("<em>");
                }

                if (style.HasFlag(LogStyle.Underline))
                {
                    sb.Append("<span style=\"text-decoration:underline;\">");
                }

                if (style.HasFlag(LogStyle.Strikeout))
                {
                    sb.Append("<span style=\"text-decoration:line-through;\">");
                }
            }

            if (item.Formattable is not null)
            {
                sb.Append(item.Formattable.ToString(null, cultureInfo).ReplaceNewLine("<br/>"));
            }
        }
    }

    /// <summary>Writes the <see cref="LogText"/> instance as HTML to the specified <see cref="StringBuilder"/>.</summary>
    /// <param name="x">The extended text.</param>
    /// <param name="sb">The StringBuilder.</param>
    public static void WriteHtml(this ILogText x, StringBuilder sb, CultureInfo cultureInfo) => x.ToLogText().WriteHtml(sb, cultureInfo);

    /// <summary>Writes the <see cref="LogText"/> instance as HTML to the specified <see cref="StringBuilder"/>.</summary>
    /// <param name="items">The extended text items.</param>
    /// <param name="sb">The StringBuilder.</param>
    public static void WriteHtml(this IEnumerable<LogText> items, StringBuilder sb, CultureInfo cultureInfo)
    {
        foreach (var item in items)
        {
            item.WriteHtml(sb, cultureInfo);
        }
    }

    /// <summary>Writes the <see cref="LogText"/> instance as HTML to the specified <see cref="StringBuilder"/>.</summary>
    /// <param name="items">The extended text items.</param>
    /// <param name="sb">The StringBuilder.</param>
    public static void WriteHtml(this IEnumerable<ILogText> items, StringBuilder sb, CultureInfo cultureInfo)
    {
        foreach (var item in items)
        {
            item.WriteHtml(sb, cultureInfo);
        }
    }

    #endregion Static
}
