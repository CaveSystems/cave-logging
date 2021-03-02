using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Cave
{
    /// <summary>
    /// Provides extension functions for <see cref="XT"/>.
    /// </summary>
    public static class XTExtension
    {
        #region Static

        /// <summary>
        /// Retrieves the <see cref="XT"/> instance as HTML.
        /// </summary>
        /// <param name="items">The extended text.</param>
        /// <returns>Returns the html code.</returns>
        public static string ToHtml(this IEnumerable<IXT> items)
        {
            var sb = new StringBuilder();
            sb.Append("<html>");
            sb.Append("<body>");
            items.WriteHtml(sb);
            sb.Append("</body>");
            sb.Append("</html>");
            return sb.ToString();
        }

        /// <summary>
        /// Retrieves the <see cref="XT"/> instance as HTML.
        /// </summary>
        /// <param name="items">The extended text.</param>
        /// <returns>Returns the html code.</returns>
        public static string ToHtml(this IEnumerable<XT> items)
        {
            var sb = new StringBuilder();
            sb.Append("<html>");
            sb.Append("<body>");
            items.WriteHtml(sb);
            sb.Append("</body>");
            sb.Append("</html>");
            return sb.ToString();
        }

        /// <summary>
        /// Gets the eXtended Text for a KeyValuePair.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="item">The item.</param>
        /// <returns>Returns a new <see cref="XT"/> instance.</returns>
        public static XT ToXT<TKey, TValue>(this KeyValuePair<TKey, TValue> item) => XT.Format("{0}={1}", item.Key, item.Value);

        /// <summary>
        /// Gets the eXtended Text for a KeyValuePair enumeration.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="items">The items.</param>
        /// <returns>Returns a new <see cref="XT"/> instance.</returns>
        public static XT ToXT<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            var x = new XTBuilder();
            foreach (var item in items)
            {
                x.Append(item.ToXT());
            }

            return x;
        }

        /// <summary>
        /// Gets the eXtended Text for a value.
        /// </summary>
        /// <param name="timeSpan">The time span.</param>
        /// <returns>Returns a new <see cref="XT"/> instance.</returns>
        public static XT ToXT(this TimeSpan timeSpan) =>
            timeSpan.Ticks > 0
                ? new XT(XTColor.Cyan, XTStyle.Default, timeSpan.FormatTime())
                : timeSpan.Ticks < 0
                    ? new XT(XTColor.Magenta, XTStyle.Default, timeSpan.FormatTime())
                    : new XT(XTColor.White, XTStyle.Default, timeSpan.FormatTime());

        /// <summary>
        /// Converts a exception to a loggable text message.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/>.</param>
        /// <param name="debug">Include debug information (stacktrace, data).</param>
        /// <returns>Returns a new <see cref="XT"/> instance.</returns>
        public static XT ToXT(this Exception ex, bool debug = false)
        {
            // ignore AggregateException
            if (ex is AggregateException)
            {
                return ToXT(ex.InnerException, debug);
            }

            if (ex == null)
            {
                return new XT(string.Empty);
            }

            var result = new List<XTItem>();
            var spacer = new XTItem(XTColor.Default, XTStyle.Default, "  ");
            result.Add(new XTItem(XTColor.Red, XTStyle.Bold, ex.GetType().Name));
            if (debug)
            {
                result.Add(XTItem.NewLine);
                result.Add(new XTItem(XTColor.White, XTStyle.Bold, "Message:"));
                result.Add(XTItem.NewLine);
            }
            else
            {
                result.Add(new XTItem(": "));
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

                result.Add(new XTItem(XTColor.Default, XTStyle.Default, str));
                result.Add(XTItem.NewLine);
            }

            if (debug)
            {
                if (!string.IsNullOrEmpty(ex.Source))
                {
                    result.Add(new XTItem(XTColor.White, XTStyle.Bold, "Source:"));
                    result.Add(XTItem.NewLine);
                    foreach (var str in ex.Source.SplitNewLine())
                    {
                        if ((str.Trim().Length == 0) || !ASCII.IsClean(str))
                        {
                            continue;
                        }

                        result.Add(spacer);
                        result.Add(new XTItem(XTColor.Default, XTStyle.Default, str));
                        result.Add(XTItem.NewLine);
                    }
                }

                if (ex.Data.Count > 0)
                {
                    result.Add(new XTItem(XTColor.White, XTStyle.Bold, "Data:"));
                    result.Add(XTItem.NewLine);
                    foreach (var key in ex.Data.Keys)
                    {
                        result.Add(new XTItem(XTColor.Default, XTStyle.Default, string.Format("  {0}: {1}\n", key, ex.Data[key])));
                        result.Add(XTItem.NewLine);
                    }
                }

                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    result.Add(new XTItem(XTColor.White, XTStyle.Bold, "StackTrace:"));
                    result.Add(XTItem.NewLine);
                    foreach (var str in ex.StackTrace.SplitNewLine())
                    {
                        if ((str.Trim().Length == 0) || !ASCII.IsClean(str))
                        {
                            continue;
                        }

                        result.Add(spacer);
                        result.Add(new XTItem(XTColor.Default, XTStyle.Default, str));
                        result.Add(XTItem.NewLine);
                    }
                }
            }

            if (ex.InnerException != null)
            {
                if (debug)
                {
                    result.Add(new XTItem(XTColor.White, XTStyle.Bold, "---"));
                    result.Add(XTItem.NewLine);
                }

                result.AddRange(ToXT(ex.InnerException, debug).Items);
            }

            if (ex is ReflectionTypeLoadException)
            {
                foreach (var inner in ((ReflectionTypeLoadException)ex).LoaderExceptions)
                {
                    if (debug)
                    {
                        result.Add(new XTItem(XTColor.White, XTStyle.Bold, "---"));
                        result.Add(XTItem.NewLine);
                    }

                    result.AddRange(ToXT(inner, debug).Items);
                }
            }

            while ((result.Count > 0) && (result[result.Count - 1] == XTItem.NewLine))
            {
                result.RemoveAt(result.Count - 1);
            }

            return new XT(result.ToArray());
        }

        /// <summary>
        /// Writes the <see cref="XT"/> instance as HTML to the specified <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="x">The extended text.</param>
        /// <param name="sb">The StringBuilder.</param>
        public static void WriteHtml(this XT x, StringBuilder sb)
        {
            var color = XTColor.Default;
            var style = XTStyle.Default;
            foreach (var item in x.Items)
            {
                if (item.Color != color)
                {
                    // close old span
                    if (color != XTColor.Default)
                    {
                        sb.Append("</span>");
                    }

                    // set new color
                    color = item.Color;

                    // open new span
                    if (color != XTColor.Default)
                    {
                        sb.Append($"<span style=\"color:{color.ToString().ToLower()}\">");
                    }
                }

                if (item.Style != style)
                {
                    // close old tags
                    if (style != XTStyle.Default)
                    {
                        if (style.HasFlag(XTStyle.Strikeout))
                        {
                            sb.Append("</span>");
                        }

                        if (style.HasFlag(XTStyle.Underline))
                        {
                            sb.Append("</span>");
                        }

                        if (style.HasFlag(XTStyle.Italic))
                        {
                            sb.Append("</em>");
                        }

                        if (style.HasFlag(XTStyle.Bold))
                        {
                            sb.Append("</strong>");
                        }
                    }

                    // set new style
                    style = item.Style;

                    // open new span
                    if (style.HasFlag(XTStyle.Bold))
                    {
                        sb.Append("<strong>");
                    }

                    if (style.HasFlag(XTStyle.Italic))
                    {
                        sb.Append("<em>");
                    }

                    if (style.HasFlag(XTStyle.Underline))
                    {
                        sb.Append("<span style=\"text-decoration:underline;\">");
                    }

                    if (style.HasFlag(XTStyle.Strikeout))
                    {
                        sb.Append("<span style=\"text-decoration:line-through;\">");
                    }
                }

                if (!string.IsNullOrEmpty(item.Text))
                {
                    sb.Append(item.Text.ReplaceNewLine("<br/>"));
                }
            }
        }

        /// <summary>
        /// Writes the <see cref="XT"/> instance as HTML to the specified <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="x">The extended text.</param>
        /// <param name="sb">The StringBuilder.</param>
        public static void WriteHtml(this IXT x, StringBuilder sb) => x.ToXT().WriteHtml(sb);

        /// <summary>
        /// Writes the <see cref="XT"/> instance as HTML to the specified <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="items">The extended text items.</param>
        /// <param name="sb">The StringBuilder.</param>
        public static void WriteHtml(this IEnumerable<XT> items, StringBuilder sb)
        {
            foreach (var item in items)
            {
                item.WriteHtml(sb);
            }
        }

        /// <summary>
        /// Writes the <see cref="XT"/> instance as HTML to the specified <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="items">The extended text items.</param>
        /// <param name="sb">The StringBuilder.</param>
        public static void WriteHtml(this IEnumerable<IXT> items, StringBuilder sb)
        {
            foreach (var item in items)
            {
                item.WriteHtml(sb);
            }
        }

        #endregion Static
    }
}
