using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Cave.Logging;

/// <summary>
/// Provides very simple html style color and style attributes. Valid attributes are.
/// <list type="table">
/// <listheader>
/// <term>Type</term>
/// <description>Valid values for the attribute:</description>
/// </listheader>
/// <item>
/// <term>&lt;color&gt;</term>
/// <description>Any of the <see cref="LogColor"/> values</description>
/// </item>
/// <item>
/// <term>&lt;style&gt;</term>
/// <description>Any of the <see cref="LogStyle"/> values</description>
/// </item>
/// </list>
/// <example>This is a &lt;red&gt;red &lt;bold&gt; text &lt;default&gt;!</example>
/// </summary>
public sealed class LogText : ILogText, IEquatable<LogText>
{
    #region Static

    /// <summary>Unboxes a token.</summary>
    /// <param name="token">Token to unbox.</param>
    /// <returns>Returns the unboxed token name.</returns>
    static string Unbox(string token)
    {
        var text = token;
        if (text.StartsWith("<") && text.EndsWith(">"))
        {
            text = text.Substring(1, text.Length - 2);
        }

        return text;
    }

    /// <summary>Gets the first valid color.</summary>
    public const LogColor FirstColor = LogColor.Black;

    /// <summary>Gets the last valid color.</summary>
    public const LogColor LastColor = LogColor.White;

    /// <summary>Gets all defined <see cref="Color"/> s.</summary>
    public static Color[] PaletteColors =>
        new[]
        {
            Color.Black,
            Color.Gray,
            Color.Blue,
            Color.Green,
            Color.Cyan,
            Color.Red,
            Color.Magenta,
            Color.Yellow,
            Color.White
        };

    /// <summary>Gets all defined <see cref="Color"/> s.</summary>
    public static ConsoleColor[] PaletteConsoleColors =>
        new[]
        {
            ConsoleColor.Black,
            ConsoleColor.Gray,
            ConsoleColor.Blue,
            ConsoleColor.Green,
            ConsoleColor.Cyan,
            ConsoleColor.Red,
            ConsoleColor.Magenta,
            ConsoleColor.Yellow,
            ConsoleColor.White
        };

    /// <summary>Implements the operator +.</summary>
    /// <param name="x1">The first item to add.</param>
    /// <param name="x2">The second item to add.</param>
    /// <returns>The result of the operator.</returns>
    public static LogText Add(LogText x1, LogText x2) => new(x1, x2);

    /// <summary>Formats the specified double.</summary>
    /// <param name="source">The source.</param>
    /// <param name="d">The double.</param>
    /// <returns>Returns a new LogTextItem.</returns>
    public static LogTextItem Format(LogTextItem source, double d)
    {
        var value = (long)Math.Round((d % 1) * 100000);
        var color = source.Color;
        if ((int)color < 100)
        {
            color = value > 0 ? LogColor.Cyan : value < 0 ? LogColor.Magenta : LogColor.White;
        }
        return new LogTextItem($"{d:N}", color, source.Style);
    }

    /// <summary>Formats the specified decimal.</summary>
    /// <param name="source">The source.</param>
    /// <param name="d">The decimal.</param>
    /// <returns>Returns a new LogTextItem.</returns>
    public static LogTextItem Format(LogTextItem source, decimal d)
    {
        var value = (long)Math.Round((d % 1) * 100000);
        var color = source.Color;
        if ((int)color < 100)
        {
            color = value > 0 ? LogColor.Cyan : value < 0 ? LogColor.Magenta : LogColor.White;
        }
        return new LogTextItem($"{d:N}", color, source.Style);
    }

    /// <summary>Formats the specified value.</summary>
    /// <param name="source">The source.</param>
    /// <param name="value">The value.</param>
    /// <returns>Returns a new LogTextItem.</returns>
    public static LogTextItem Format(LogTextItem source, long value)
    {
        var color = source.Color;
        if ((int)color < 100)
        {
            color = value > 0 ? LogColor.Cyan : value < 0 ? LogColor.Magenta : LogColor.White;
        }
        return new LogTextItem($"{value:N}", color, source.Style);
    }

    /// <summary>Formats the specified value.</summary>
    /// <param name="source">The source.</param>
    /// <param name="value">The value.</param>
    /// <returns>Returns a new LogTextItem.</returns>
    public static LogTextItem Format(LogTextItem source, int value)
    {
        var color = source.Color;
        if ((int)color < 100)
        {
            color = value > 0 ? LogColor.Cyan : value < 0 ? LogColor.Magenta : LogColor.White;
        }

        return new LogTextItem($"{value}", color, source.Style);
    }

    /// <summary>Gets the <see cref="LogColor"/> for the specified string.</summary>
    /// <param name="color">The color name.</param>
    /// <returns>Returns the color.</returns>
    public static LogColor GetColor(string color)
    {
        if (color == null)
        {
            throw new ArgumentNullException(nameof(color));
        }

        if (!Unbox(color).TryParse<LogColor>(out var result))
        {
            result = LogColor.Unchanged;
        }
        return result;
    }

    /// <summary>Gets the <see cref="LogStyle"/> for the specified string.</summary>
    /// <param name="style">The style name.</param>
    /// <returns>Returns the style.</returns>
    public static LogStyle GetStyle(string style)
    {
        if (style == null)
        {
            throw new ArgumentNullException(nameof(style));
        }

        if (!Unbox(style).TryParse<LogStyle>(out var result))
        {
            result = LogStyle.Unchanged;
        }
        return result;
    }

    /// <summary>Checks whether a specified string is a valid <see cref="LogColor"/>.</summary>
    /// <param name="color">Name of the color.</param>
    /// <returns>Returns true if the specified string is a valid color.</returns>
    public static bool IsColor(string color) => Unbox(color).TryParse<LogColor>(out var result);

    /// <summary>Checks whether a specified string is a valid <see cref="LogStyle"/>.</summary>
    /// <param name="style">The style name.</param>
    /// <returns>Returns true if the specified string is a valid style name.</returns>
    public static bool IsStyle(string style) => Unbox(style).TryParse<LogStyle>(out var result);

    /// <summary>Implements the operator !=.</summary>
    /// <param name="x1">The first item.</param>
    /// <param name="x2">The second item.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator !=(LogText x1, LogText x2) => x1?.ToString() != x2?.ToString();

    /// <summary>Implements the operator +.</summary>
    /// <param name="x1">The first item to add.</param>
    /// <param name="x2">The second item to add.</param>
    /// <returns>The result of the operator.</returns>
    public static LogText operator +(LogText x1, LogText x2) => new(x1, x2);

    /// <summary>Implements the operator +.</summary>
    /// <param name="x1">The first item to add.</param>
    /// <param name="x2">The second item to add.</param>
    /// <returns>The result of the operator.</returns>
    public static LogText operator +(LogText x1, LogTextItem x2)
    {
        var items = new List<LogTextItem>();
        items.AddRange(x1.Items);
        items.Add(x2);
        return new LogText(items.ToArray());
    }

    /// <summary>Implements the operator +.</summary>
    /// <param name="x1">The first item to add.</param>
    /// <param name="x2">The second item to add.</param>
    /// <returns>The result of the operator.</returns>
    public static LogText operator +(LogTextItem x1, LogText x2)
    {
        var items = new List<LogTextItem> { x1 };
        items.AddRange(x2.Items);
        return new LogText(items.ToArray());
    }

    /// <summary>Implements the operator ==.</summary>
    /// <param name="x1">The first item.</param>
    /// <param name="x2">The second item.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator ==(LogText x1, LogText x2) => x1?.ToString() == x2?.ToString();

    /// <summary>Converts the specified <see cref="LogColor"/> to a <see cref="Color"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An Exception is thrown if an invalid color string is given.</exception>
    /// <param name="color">The color.</param>
    /// <returns>Returns the matching color code.</returns>
    public static Color ToColor(LogColor color)
    {
        try
        {
            return PaletteColors[(int)color - (int)FirstColor];
        }
        catch (Exception e)
        {
            throw new ArgumentOutOfRangeException($"Invalid or unknown LogColor '{color}'!", e);
        }
    }

    /// <summary>Converts the specified <see cref="LogColor"/> to a <see cref="ConsoleColor"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An Exception is thrown if an invalid color is given.</exception>
    /// <param name="color">The color.</param>
    /// <returns>Returns the matching console color.</returns>
    public static ConsoleColor ToConsoleColor(LogColor color)
    {
        try
        {
            return PaletteConsoleColors[(int)color - (int)FirstColor];
        }
        catch
        {
            throw new ArgumentOutOfRangeException($"Invalid or unknown LogColor '{color}'!");
        }
    }

    /// <summary>Gets the string for a specified color.</summary>
    /// <param name="color">The color.</param>
    /// <returns>Returns the color token.</returns>
    public static string ToString(LogColor color) => "<" + color + ">";

    /// <summary>Gets the string for a specified style.</summary>
    /// <param name="style">The style.</param>
    /// <returns>Returns the style token.</returns>
    public static string ToString(LogStyle style) => "<" + style + ">";

    #endregion Static

    #region Constructors

    /// <summary>Initializes a new instance of the <see cref="LogText"/> class.</summary>
    /// <param name="color">Color of the text.</param>
    /// <param name="style">The style.</param>
    /// <param name="text">The text.</param>
    public LogText(IFormattable text, LogColor color = 0, LogStyle style = 0)
        : this(new LogTextItem(text, color, style))
    {
    }

    /// <summary>Initializes a new instance of the <see cref="LogText"/> class.</summary>
    /// <param name="items">The items.</param>
    public LogText(params LogTextItem[] items) => Items = items.AsReadOnly();

    /// <summary>Initializes a new instance of the <see cref="LogText"/> class.</summary>
    /// <param name="items">The items.</param>
    public LogText(params LogText[] items)
    {
        Items = items.SelectMany(i => i.Items).ToList().AsReadOnly();
    }

    #endregion Constructors

    #region Properties

    /// <summary>Gets all <see cref="LogText"/> Items at this instance.</summary>
    public ICollection<LogTextItem> Items { get; }

    /// <summary>Gets the plain text without any style and color information.</summary>
    public string ToString(IFormatProvider provider)
    {
        var result = new StringBuilder();
        foreach (var item in Items)
        {
            result.Append(item.Formattable.ToString(null, provider));
        }

        return result.ToString();
    }

    #endregion Properties

    #region IEquatable<LogText> Members

    /// <inheritdoc />
    public bool Equals(LogText other) => Items.SequenceEqual(other.Items);

    #endregion IEquatable<LogText> Members

    #region IXT Members

    /// <inheritdoc/>
    public LogText ToLogText() => this;

    #endregion IXT Members

    #region Overrides

    /// <summary>Determines whether the specified <see cref="object"/>, is equal to this instance.</summary>
    /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
    /// <returns><c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
    public override bool Equals(object obj) => obj is LogText other && Equals(other);

    /// <summary>Returns a hash code for this instance.</summary>
    /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
    public override int GetHashCode() => DefaultHashingFunction.Combine(Items);

    /// <summary>Gets the plain text without any style and color information using <see cref="ToString(IFormatProvider)"/> for <see cref="CultureInfo.CurrentCulture"/>.</summary>
    public override string ToString() => ToString(CultureInfo.CurrentCulture);

    #endregion Overrides

    #region Members

    public static LogText Parse(string data)
    {
        if (data == null)
        {
            throw new Exception("Data is unset!");
        }

        var plainText = new StringBuilder();
        var lines = data.SplitNewLine();
        var items = new List<LogTextItem>();
        items.Clear();
        for (var i = 0; i < lines.Length; i++)
        {
            var color = LogColor.Unchanged;
            var style = LogStyle.Unchanged;
            if (i > 0)
            {
                plainText.AppendLine();
                items.Add(LogTextItem.NewLine);
            }

            var currentLine = lines[i];
            var textStart = 0;
            while (true)
            {
                // find a complete token
                var tokenStart = currentLine.IndexOfAny(new[]
                {
                    '<',
                    '{'
                }, textStart);
                var tokenEnd = -1;

                // while we got a valid start
                while (tokenStart > -1)
                {
                    // get end
                    switch (currentLine[tokenStart])
                    {
                        case '<':
                            tokenEnd = currentLine.IndexOf('>', tokenStart);
                            break;

                        case '{':
                            tokenEnd = currentLine.IndexOf('}', tokenStart);
                            break;
                    }

                    // check for another start in between
                    var nextTokenStart = currentLine.IndexOfAny(new[]
                    {
                        '<',
                        '{'
                    }, tokenStart + 1);

                    // no additional start ? -> exit loop
                    if (nextTokenStart < 0)
                    {
                        break;
                    }

                    // additional token, if current does not end, end it.
                    if (tokenEnd < 0)
                    {
                        tokenEnd = nextTokenStart - 1;
                        break;
                    }

                    if (nextTokenStart > tokenEnd)
                    {
                        break;
                    }

                    // additional start -> skip the first
                    tokenStart = nextTokenStart;
                }

                string currentText;
                if ((tokenStart < 0) || (tokenEnd < 0))
                {
                    currentText = currentLine.Substring(textStart);
                    if (currentText.Length > 0)
                    {
                        plainText.Append(currentText);
                        items.Add(new LogTextItem(currentText, color, style));
                    }

                    break;
                }

                currentText = currentLine.Substring(textStart, tokenStart - textStart);
                if (currentText.Length > 0)
                {
                    plainText.Append(currentText);
                    items.Add(new LogTextItem(currentText, color, style));
                }

                var token = currentLine.Substring(tokenStart, ++tokenEnd - tokenStart);
                if (IsColor(token))
                {
                    color = GetColor(token);
                }
                else if (IsStyle(token))
                {
                    var newStyle = GetStyle(token);
                    if (newStyle == LogStyle.Reset)
                    {
                        style = LogStyle.Reset;
                    }
                    else
                    {
                        style |= newStyle;
                    }
                }
                else
                {
                    // invalid token or parameter, ignore
                    plainText.Append(token);
                    items.Add(new LogTextItem(token, color, style));
                }

                textStart = tokenEnd;
            }
        }

        if (data.EndsWith("\r") || data.EndsWith("\n"))
        {
            items.Add(LogTextItem.NewLine);
            plainText.AppendLine();
        }

        if ((items.Count > 0) && (items[items.Count - 1].Color != LogColor.Unchanged))
        {
            items.Add(new LogTextItem(string.Empty, LogColor.Default));
        }

        return new(items.ToArray());
    }

    #endregion Members
}
