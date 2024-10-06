using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
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
public record LogText : ILogText, IEquatable<LogText>
{
    static readonly char[] TokenStartSeparator = ['\n', '<', '{'];

    /// <summary>Unboxes a token.</summary>
    /// <param name="token">Token to unbox.</param>
    /// <returns>Returns the unboxed token name.</returns>
    static string Unbox(string token)
    {
        var text = token;
        if (text.StartsWith("<") && text.EndsWith(">"))
        {
            text = text[1..^1];
        }

        return text;
    }

    /// <summary>Gets the first valid color.</summary>
    public const LogColor FirstColor = LogColor.Black;

    /// <summary>Gets the last valid color.</summary>
    public const LogColor LastColor = LogColor.White;

    /// <summary>Provides a new line item.</summary>
    public static readonly LogText NewLine = new("\n", style: LogStyle.Reset);

    /// <summary>Initializes a new instance of the <see cref="LogText"/> class.</summary>
    /// <param name="color">Color of the text.</param>
    /// <param name="style">The style.</param>
    /// <param name="text">The text.</param>
    public LogText(string text, LogColor color = 0, LogStyle style = 0)
    {
        Text = text;
        Color = color;
        Style = style;
    }

    /// <summary>Provides an empty log message instance</summary>
    public static LogText[] Empty { get; } = new LogText[0];

    /// <summary>Gets all defined <see cref="Color"/> s.</summary>
    public static Color[] PaletteColors =>
        new[]
        {
            System.Drawing.Color.Black,
            System.Drawing.Color.Gray,
            System.Drawing.Color.Blue,
            System.Drawing.Color.Green,
            System.Drawing.Color.Cyan,
            System.Drawing.Color.Red,
            System.Drawing.Color.Magenta,
            System.Drawing.Color.Yellow,
            System.Drawing.Color.White
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

    /// <inheritdoc/>
    public LogColor Color { get; }

    /// <inheritdoc/>
    public LogStyle Style { get; }

    /// <inheritdoc/>
    public string Text { get; }

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
            result = 0;
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

        var unboxed = Unbox(style);
        if (string.Equals(unboxed, "default", StringComparison.OrdinalIgnoreCase))
        {
            return LogStyle.Reset;
        }
        if (!unboxed.TryParse<LogStyle>(out var result))
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
    public static bool IsStyle(string style)
    {
        var unboxed = Unbox(style);
        return string.Equals(unboxed, "default", StringComparison.OrdinalIgnoreCase) || unboxed.TryParse<LogStyle>(out var result);
    }

    /// <summary>Parses the specified text</summary>
    /// <param name="text">Text to parse.</param>
    /// <returns>Returns a list of <see cref="ILogText"/> items.</returns>
    /// <exception cref="Exception"></exception>
    public static IList<ILogText> Parse(string text)
    {
        if (text == null)
        {
            throw new Exception("Data is unset!");
        }

        var items = new List<ILogText>();
        if (text.Contains("\r")) text = text.ReplaceNewLine("\n");

        var color = (LogColor)0;
        var style = LogStyle.Unchanged;

        var textStart = 0;
        while (true)
        {
            // find a complete token
            var tokenStart = text.IndexOfAny(TokenStartSeparator, textStart);
            var tokenEnd = -1;

            // while we got a valid start
            while (tokenStart > -1)
            {
                // get end
                switch (text[tokenStart])
                {
                    case '\n': tokenEnd = tokenStart; break;
                    case '<': tokenEnd = text.IndexOf('>', tokenStart); break;
                    case '{': tokenEnd = text.IndexOf('}', tokenStart); break;
                }

                // check for another start in between
                var nextTokenStart = text.IndexOfAny(TokenStartSeparator, tokenStart + 1);

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
                currentText = text[textStart..];
                if (currentText.Length > 0)
                {
                    items.Add(new LogText(currentText, color, style));
                }

                break;
            }

            currentText = text[textStart..tokenStart];
            if (currentText.Length > 0)
            {
                items.Add(new LogText(currentText, color, style));
                if (style == LogStyle.Reset) style = LogStyle.Unchanged;
            }

            var token = text[tokenStart..++tokenEnd];
            if (token == "\n")
            {
                color = 0;
                style = LogStyle.Reset;
                items.Add(LogText.NewLine);
            }
            else if (IsColor(token))
            {
                color = GetColor(token);
            }
            else if (IsStyle(token))
            {
                var newStyle = GetStyle(token);
                if (newStyle == LogStyle.Reset)
                {
                    style = LogStyle.Reset;
                    color = 0;
                }
                else
                {
                    style |= newStyle;
                }
            }
            else
            {
                // invalid token or parameter, ignore
                items.Add(new LogText(token, color, style));
            }

            textStart = tokenEnd;
        }

        //add color and style changes to a new item if there are any
        if (items.Count > 0)
        {
            var lastItem = items[^1];
            if (!lastItem.Equals(LogText.NewLine) && (lastItem.Color != color || lastItem.Style != style))
            {
                items.Add(new LogText("", color, style));
            }
        }
        else
        {
            if (color != 0 || style != 0)
            {
                items.Add(new LogText("", color, style));
            }
        }
        return items;
    }

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

    /// <inheritdoc/>
    public virtual bool Equals(LogText? other) => Equals(Text, other?.Text) && Equals(Color, other.Color) && Equals(Style, other.Style);

    /// <inheritdoc/>
    public virtual bool Equals(ILogText? other) => Equals(Text, other?.Text) && Equals(Color, other.Color) && Equals(Style, other.Style);

    /// <summary>Returns a hash code for this instance.</summary>
    /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
    public override int GetHashCode() => DefaultHashingFunction.Combine(Text, Color, Style);

    /// <summary>Gets the plain text without any style and color information using <see cref="ToString()"/> for <see cref="CultureInfo.CurrentCulture"/>.</summary>
    public override string ToString()
    {
        StringBuilder sb = new();
        if (Style != default) sb.Append(ToString(Style));
        if (Color != default) sb.Append(ToString(Color));
        sb.Append(Text);
        return sb.ToString();
    }
}
