using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Cave
{
    /// <summary>
    /// Provides very simple html style eXtended Text attributes. Valid attributes are.
    /// <list type="table">
    /// <listheader>
    /// <term>Attributetype</term>
    /// <description>Valid values for the attribute:</description>
    /// </listheader>
    /// <item>
    /// <term>&lt;color&gt;</term>
    /// <description>Any of the <see cref="XTColor"/> values</description>
    /// </item>
    /// <item>
    /// <term>&lt;style&gt;</term>
    /// <description>Any of the <see cref="XTStyle"/> values</description>
    /// </item>
    /// </list>
    /// <example>This is a &lt;red&gt;red &lt;bold&gt; text &lt;default&gt;!</example>
    /// </summary>
    public sealed class XT : IXT, IEquatable<XT>
    {
        #region Static

        /// <summary>
        /// Unboxes a token.
        /// </summary>
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

        /// <summary>
        /// Gets the first valid color.
        /// </summary>
        public const XTColor FirstColor = XTColor.Black;

        /// <summary>
        /// Gets the last valid color.
        /// </summary>
        public const XTColor LastColor = XTColor.White;

        /// <summary>
        /// Gets all defined <see cref="Color"/> s.
        /// </summary>
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

        /// <summary>
        /// Gets all defined <see cref="Color"/> s.
        /// </summary>
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

        /// <summary>
        /// Implements the operator +.
        /// </summary>
        /// <param name="x1">The first item to add.</param>
        /// <param name="x2">The second item to add.</param>
        /// <returns>The result of the operator.</returns>
        public static XT Add(XT x1, XT x2) => new(x1, x2);

        /// <summary>
        /// Formats the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>Returns an array of XTItems.</returns>
        public static XTItem[] Format(XT text, params object[] args)
        {
            var result = new List<XTItem>();
            foreach (var item in text.Items)
            {
                if (item.Text.StartsWith("{"))
                {
                    if (int.TryParse(item.Text.Unbox("{", "}"), out var i))
                    {
                        object obj;
                        try
                        {
                            obj = args[i];
                        }
                        catch
                        {
                            obj = null;
                        }

                        {
                            var x = obj as XTItem;
                            if (x != null)
                            {
                                if ((item.Color == XTColor.Default) && (item.Style == XTStyle.Default))
                                {
                                    result.Add(x);
                                }
                                else
                                {
                                    // override sub style
                                    result.Add(new XTItem(item.Color, item.Style, x.Text));
                                }

                                continue;
                            }
                        }
                        {
                            if (obj is IXT x)
                            {
                                if ((item.Color == XTColor.Default) && (item.Style == XTStyle.Default))
                                {
                                    result.AddRange(x.ToXT().Items);
                                }
                                else
                                {
                                    foreach (var sub in x.ToXT().Items)
                                    {
                                        // override sub style
                                        result.Add(new XTItem(item.Color, item.Style, sub.Text));
                                    }
                                }

                                continue;
                            }
                        }
                        if (obj is null)
                        {
                            result.Add(new XTItem(item.Color, item.Style, "<null>"));
                            continue;
                        }

                        XTItem newItem;
                        if (obj is bool)
                        {
                            newItem = new XTItem((bool)obj ? XTColor.Green : XTColor.Red, item.Style, obj.ToString());
                        }
                        else if (obj is double)
                        {
                            newItem = Format(item, (double)obj);
                        }
                        else if (obj is decimal)
                        {
                            newItem = Format(item, (decimal)obj);
                        }
                        else if (obj is float)
                        {
                            newItem = Format(item, (float)obj);
                        }
                        else if (obj is int)
                        {
                            newItem = Format(item, (int)obj);
                        }
                        else if (obj is long)
                        {
                            newItem = Format(item, (long)obj);
                        }
                        else if (obj is short)
                        {
                            newItem = Format(item, (short)obj);
                        }
                        else if (obj is sbyte)
                        {
                            newItem = Format(item, (sbyte)obj);
                        }
                        else if (obj is string)
                        {
                            var x = new XT((string)obj).Items;
                            var n = 0;
                            while (n < x.Length)
                            {
                                if ((x[n].Color != XTColor.Default) && (x[n].Style != XTStyle.Default))
                                {
                                    break;
                                }

                                var y = new XTItem(item.Color, item.Style, x[n].Text);
                                result.Add(y);
                                n++;
                            }

                            while (n < x.Length)
                            {
                                result.Add(x[n++]);
                            }

                            continue;
                        }
                        else if (obj is IEnumerable)
                        {
                            var first = true;
                            foreach (var o in (IEnumerable)obj)
                            {
                                if (first)
                                {
                                    first = false;
                                }
                                else
                                {
                                    result.Add(new XTItem(XTColor.Default, ","));
                                }

                                result.Add(new XTItem(XTColor.Cyan, item.Style, StringExtensions.ToString(o)));
                            }

                            continue;
                        }
                        else
                        {
                            newItem = new XTItem(item.Color, item.Style, StringExtensions.ToString(obj));
                        }

                        result.Add(newItem);
                        continue;
                    }
                }

                result.Add(item);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Formats the specified double.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="d">The double.</param>
        /// <returns>Returns a new XTItem.</returns>
        public static XTItem Format(XTItem source, double d)
        {
            // maximum 5 digits
            var value = (long)Math.Round((d % 1) * 100000);
            var color = source.Color;
            if (color == XTColor.Default)
            {
                color = value > 0 ? XTColor.Cyan : value < 0 ? XTColor.Magenta : XTColor.White;
            }

            if ((value % 100) != 0)
            {
                // need all (5) digits
                return new XTItem(color, source.Style, d.ToString("N5"));
            }

            if ((value % 1000) != 0)
            {
                // need 3 digits
                return new XTItem(color, source.Style, d.ToString("N3"));
            }

            if (value != 0)
            {
                // need 2 digits
                return new XTItem(color, source.Style, d.ToString("N2"));
            }

            // no digits at all
            return new XTItem(color, source.Style, d.ToString("N0"));
        }

        /// <summary>
        /// Formats the specified decimal.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="d">The decimal.</param>
        /// <returns>Returns a new XTItem.</returns>
        public static XTItem Format(XTItem source, decimal d)
        {
            // maximum 5 digits
            var value = (long)Math.Round((d % 1) * 100000);
            var color = source.Color;
            if (color == XTColor.Default)
            {
                color = value > 0 ? XTColor.Cyan : value < 0 ? XTColor.Magenta : XTColor.White;
            }

            if ((value % 100) != 0)
            {
                // need all (5) digits
                return new XTItem(color, d.ToString("N5"));
            }

            if ((value % 1000) != 0)
            {
                // need 3 digits
                return new XTItem(color, d.ToString("N3"));
            }

            if (value != 0)
            {
                // need 2 digits
                return new XTItem(color, d.ToString("N2"));
            }

            // no digits at all
            return new XTItem(color, d.ToString("N0"));
        }

        /// <summary>
        /// Formats the specified value.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="value">The value.</param>
        /// <returns>Returns a new XTItem.</returns>
        public static XTItem Format(XTItem source, long value)
        {
            var color = source.Color;
            if (color == XTColor.Default)
            {
                color = value > 0 ? XTColor.Cyan : value < 0 ? XTColor.Magenta : XTColor.White;
            }

            return new XTItem(color, value.ToString());
        }

        /// <summary>
        /// Formats the specified value.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="value">The value.</param>
        /// <returns>Returns a new XTItem.</returns>
        public static XTItem Format(XTItem source, int value)
        {
            var color = source.Color;
            if (color == XTColor.Default)
            {
                color = value > 0 ? XTColor.Cyan : value < 0 ? XTColor.Magenta : XTColor.White;
            }

            return new XTItem(color, value.ToString());
        }

        /// <summary>
        /// Gets the <see cref="XTColor"/> for the specified string.
        /// </summary>
        /// <param name="color">The color name.</param>
        /// <returns>Returns the color.</returns>
        public static XTColor GetColor(string color)
        {
            if (color == null)
            {
                throw new ArgumentNullException(nameof(color));
            }

            var unboxed = Unbox(color);
            var result = (XTColor)Enum.Parse(typeof(XTColor), unboxed, true);
            if ((result != XTColor.Default) && ((result > LastColor) || (result < FirstColor)))
            {
                throw new ArgumentOutOfRangeException(nameof(color));
            }

            return result;
        }

        /// <summary>
        /// Gets the <see cref="XTStyle"/> for the specified string.
        /// </summary>
        /// <param name="style">The style name.</param>
        /// <returns>Returns the style.</returns>
        public static XTStyle GetStyle(string style)
        {
            if (style == null)
            {
                throw new ArgumentNullException("style");
            }

            var unboxed = Unbox(style);
            return (XTStyle)Enum.Parse(typeof(XTStyle), unboxed, true);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="string"/> to <see cref="XT"/>.
        /// </summary>
        /// <param name="text">The string containing extended text.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator XT(string text) => new(text);

        /// <summary>
        /// Performs an implicit conversion from <see cref="XTItem"/> array to <see cref="XT"/>.
        /// </summary>
        /// <param name="items">The extended text items.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator XT(XTItem[] items) => new(items);

        /// <summary>
        /// Checks whether a specified string is a valid <see cref="XTColor"/>.
        /// </summary>
        /// <param name="color">Name of the color.</param>
        /// <returns>Returns true if the specified string is a valid color.</returns>
        public static bool IsColor(string color)
        {
            var name = Unbox(color).ToLower();
            switch (name)
            {
                case "default":
                case "black":
                case "gray":
                case "blue":
                case "green":
                case "cyan":
                case "red":
                case "magenta":
                case "yellow":
                case "white":
                return true;

                default:
                return false;
            }
        }

        /// <summary>
        /// Checks whether a specified string is a valid <see cref="XTStyle"/>.
        /// </summary>
        /// <param name="style">The style name.</param>
        /// <returns>Returns true if the specified string is a valid style name.</returns>
        public static bool IsStyle(string style)
        {
            var name = Unbox(style).ToLower();
            switch (name)
            {
                case "default":
                case "bold":
                case "italic":
                case "underline":
                case "stikeout":
                return true;

                default:
                return false;
            }
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="x1">The first item.</param>
        /// <param name="x2">The second item.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(XT x1, XT x2) => x1?.ToString() != x2?.ToString();

        /// <summary>
        /// Implements the operator +.
        /// </summary>
        /// <param name="x1">The first item to add.</param>
        /// <param name="x2">The second item to add.</param>
        /// <returns>The result of the operator.</returns>
        public static XT operator +(XT x1, XT x2) => new(x1, x2);

        /// <summary>
        /// Implements the operator +.
        /// </summary>
        /// <param name="x1">The first item to add.</param>
        /// <param name="x2">The second item to add.</param>
        /// <returns>The result of the operator.</returns>
        public static XT operator +(XT x1, XTItem x2)
        {
            var items = new List<XTItem>();
            items.AddRange(x1.Items);
            items.Add(x2);
            return new XT(items.ToArray());
        }

        /// <summary>
        /// Implements the operator +.
        /// </summary>
        /// <param name="x1">The first item to add.</param>
        /// <param name="x2">The second item to add.</param>
        /// <returns>The result of the operator.</returns>
        public static XT operator +(XTItem x1, XT x2)
        {
            var items = new List<XTItem> { x1 };
            items.AddRange(x2.Items);
            return new XT(items.ToArray());
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="x1">The first item.</param>
        /// <param name="x2">The second item.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(XT x1, XT x2) => x1?.ToString() == x2?.ToString();

        /// <summary>
        /// Converts the specified <see cref="XTColor"/> to a <see cref="Color"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">An Exception is thrown if an invalid color string is given.</exception>
        /// <param name="color">The color.</param>
        /// <returns>Returns the matching color code.</returns>
        public static Color ToColor(XTColor color)
        {
            try
            {
                return PaletteColors[(int)color - (int)FirstColor];
            }
            catch (Exception e)
            {
                throw new ArgumentOutOfRangeException(string.Format("Invalid or unknown XTColor '{0}'!", color), e);
            }
        }

        /// <summary>
        /// Converts the specified <see cref="XTColor"/> to a <see cref="ConsoleColor"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">An Exception is thrown if an invalid color is given.</exception>
        /// <param name="color">The color.</param>
        /// <returns>Returns the matching console color.</returns>
        public static ConsoleColor ToConsoleColor(XTColor color)
        {
            try
            {
                return PaletteConsoleColors[(int)color - (int)FirstColor];
            }
            catch
            {
                throw new ArgumentOutOfRangeException(string.Format("Invalid or unknown XTColor '{0}'!", color));
            }
        }

        /// <summary>
        /// Gets the string for a specified color.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns>Returns the color token.</returns>
        public static string ToString(XTColor color) => "<" + color + ">";

        /// <summary>
        /// Gets the string for a specified style.
        /// </summary>
        /// <param name="style">The style.</param>
        /// <returns>Returns the style token.</returns>
        public static string ToString(XTStyle style) => "<" + style + ">";

        #endregion Static

        #region Private Fields

        string data;
        XTItem[] items;
        string text;

        #endregion Private Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XT"/> class.
        /// </summary>
        /// <param name="data">Data string.</param>
        public XT(string data) => this.data = data ?? throw new ArgumentNullException(nameof(data));

        /// <summary>
        /// Initializes a new instance of the <see cref="XT"/> class.
        /// </summary>
        /// <param name="color">Color of the text.</param>
        /// <param name="style">The style.</param>
        /// <param name="text">The text.</param>
        public XT(XTColor color, XTStyle style, string text)
            : this(new XTItem(color, style, text))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XT"/> class.
        /// </summary>
        /// <param name="items">The items.</param>
        public XT(params XTItem[] items) => this.items = items;

        /// <summary>
        /// Initializes a new instance of the <see cref="XT"/> class.
        /// </summary>
        /// <param name="xT1">The first item to add.</param>
        /// <param name="xT2">The second item to add.</param>
        public XT(XT xT1, XT xT2)
        {
            if (xT1 == null)
            {
                throw new ArgumentNullException("xT1");
            }

            if (xT2 == null)
            {
                throw new ArgumentNullException("xT2");
            }

            var items = new List<XTItem>();
            items.AddRange(xT1.Items);
            items.AddRange(xT2.Items);
            this.items = items.ToArray();
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets the plain data containing all style and color informations.
        /// </summary>
        public string Data
        {
            get
            {
                if (data == null)
                {
                    var sb = new StringBuilder();
                    var col = XTColor.Default;
                    var syl = XTStyle.Default;
                    foreach (var item in Items)
                    {
                        if (item.Color == XTColor.Default)
                        {
                            if (col != XTColor.Default)
                            {
                                sb.Append("<default>");
                            }
                        }
                        else if (item.Style == XTStyle.Default)
                        {
                            if (syl != XTStyle.Default)
                            {
                                sb.Append("<default>");
                            }
                        }

                        sb.Append(item);
                        col = item.Color;
                        syl = item.Style;
                    }

                    data = sb.ToString();
                }

                return data;
            }
        }

        /// <summary>
        /// Gets the item count.
        /// </summary>
        /// <value>The item count.</value>
        public int ItemCount
        {
            get
            {
                if (items == null)
                {
                    ParseData();
                }

                return items.Length;
            }
        }

        /// <summary>
        /// Gets all <see cref="XT"/> Items at this instance.
        /// </summary>
        public XTItem[] Items
        {
            get
            {
                if (items == null)
                {
                    ParseData();
                }

                return (XTItem[])items.Clone();
            }
        }

        /// <summary>
        /// Gets the plain text without any style and color informations.
        /// </summary>
        public string Text
        {
            get
            {
                if (text == null)
                {
                    if (items == null)
                    {
                        ParseData();
                    }

                    if (text == null)
                    {
                        var result = new StringBuilder();
                        foreach (var item in items)
                        {
                            result.Append(item.Text);
                        }

                        text = result.ToString();
                    }
                }

                return text;
            }
        }

        #endregion Properties

        #region IEquatable<XT> Members

        /// <summary>
        /// Gibt an, ob das aktuelle Objekt einem anderen Objekt des gleichen Typs entspricht.
        /// </summary>
        /// <param name="other">Ein Objekt, das mit diesem Objekt verglichen werden soll.</param>
        /// <returns>true, wenn das aktuelle Objekt gleich dem <paramref name="other"/>-Parameter ist, andernfalls false.</returns>
        public bool Equals(XT other) => Text == other?.Text;

        #endregion IEquatable<XT> Members

        #region IXT Members

        /// <summary>
        /// Provides an eXtended Text string for this object.
        /// </summary>
        /// <returns>Returns a new XT instance with the description of this object.</returns>
        public XT ToXT() => this;

        #endregion IXT Members

        #region Overrides

        /// <summary>
        /// Determines whether the specified <see cref="object"/>, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj) => Equals(obj as XT);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode() => Text.GetHashCode();

        /// <summary>
        /// Gets the parsable data of the xt.
        /// </summary>
        /// <returns>Returns <see cref="Data"/>.</returns>
        public override string ToString() => Data;

        #endregion Overrides

        #region Members

        void ParseData()
        {
            if (data == null)
            {
                throw new Exception("Data is unset!");
            }

            var plainText = new StringBuilder();
            var lines = data.SplitNewLine();
            var items = new List<XTItem>();
            items.Clear();
            for (var i = 0; i < lines.Length; i++)
            {
                var color = XTColor.Default;
                var style = XTStyle.Default;
                if (i > 0)
                {
                    plainText.AppendLine();
                    items.Add(XTItem.NewLine);
                }

                var currentLine = lines[i];
                var textStart = 0;
                var currentText = string.Empty;
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

                    if ((tokenStart < 0) || (tokenEnd < 0))
                    {
                        currentText = currentLine.Substring(textStart);
                        if (currentText.Length > 0)
                        {
                            plainText.Append(currentText);
                            items.Add(new XTItem(color, style, currentText));
                        }

                        break;
                    }

                    currentText = currentLine.Substring(textStart, tokenStart - textStart);
                    if (currentText.Length > 0)
                    {
                        plainText.Append(currentText);
                        items.Add(new XTItem(color, style, currentText));
                    }

                    var token = currentLine.Substring(tokenStart, ++tokenEnd - tokenStart);
                    if (IsColor(token))
                    {
                        color = GetColor(token);
                    }
                    else if (IsStyle(token))
                    {
                        var newStyle = GetStyle(token);
                        if (newStyle == XTStyle.Default)
                        {
                            style = XTStyle.Default;
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
                        items.Add(new XTItem(color, style, token));
                    }

                    textStart = tokenEnd;
                }
            }

            if (data.EndsWith("\r") || data.EndsWith("\n"))
            {
                items.Add(XTItem.NewLine);
                plainText.AppendLine();
            }

            if ((items.Count > 0) && (items[items.Count - 1].Color != XTColor.Default))
            {
                items.Add(new XTItem(XTColor.Default, string.Empty));
                data += "<default>";
            }

            text = plainText.ToString();
            this.items = items.ToArray();
        }

        #endregion Members
    }
}
