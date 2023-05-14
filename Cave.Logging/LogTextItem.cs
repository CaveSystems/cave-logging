using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Cave.CodeGen;
using Cave.Logging;

namespace Cave.Logging;

/// <summary>Provides very simple html style extended text attributes.</summary>
public class LogTextItem : IEquatable<LogTextItem>, IFormattable
{
    #region Static

    /// <summary>Provides a new line item.</summary>
    public static readonly LogTextItem NewLine = new($"{Environment.NewLine}");

    /// <summary>Implements the operator !=.</summary>
    /// <param name="x1">The first item.</param>
    /// <param name="x2">The second item.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator !=(LogTextItem x1, LogTextItem x2) => x1?.ToString() != x2?.ToString();

    /// <summary>Implements the operator +.</summary>
    /// <param name="x1">The first item to add.</param>
    /// <param name="x2">The second item to add.</param>
    /// <returns>The result of the operator.</returns>
    public static LogText operator +(LogTextItem x1, LogTextItem x2) => new(x1, x2);

    /// <summary>Implements the operator ==.</summary>
    /// <param name="x1">The first item.</param>
    /// <param name="x2">The second item.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator ==(LogTextItem x1, LogTextItem x2) => Equals(x1, x2);

    #endregion Static

    #region Public Fields

    /// <summary>Gets the Color of the item.</summary>
    public readonly LogColor Color;

    /// <summary>Gets the Style of the item.</summary>
    public readonly LogStyle Style;

    /// <summary>Gets the text of the item.</summary>
    public readonly IFormattable Formattable;

    #endregion Public Fields

    #region Constructors

    /// <summary>Initializes a new instance of the <see cref="LogTextItem"/> class.</summary>
    /// <param name="color">Color of the item.</param>
    /// <param name="style">Style of the item.</param>
    /// <param name="formattable">Text.</param>
    public LogTextItem(IFormattable formattable, LogColor color = 0, LogStyle style = 0)
    {
        Color = color;
        Style = style;
        Formattable = formattable;
    }

    #endregion Constructors

    #region Overrides

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is LogTextItem other && Equals(other);

    /// <inheritdoc/>
    public bool Equals(LogTextItem other) => (other.Color == Color) && (other.Style == Style) && (other.Formattable == Formattable);

    /// <inheritdoc/>
    public override int GetHashCode() => UserHashingFunction.Combine(Formattable, Style, Color);

    /// <inheritdoc/>
    public override string ToString() => throw new NotSupportedException();

    /// <inheritdoc/>
    public string ToString(string format, IFormatProvider? formatProvider)
    {
        formatProvider ??= CultureInfo.CurrentCulture;
        var result = new StringBuilder();
        if (Style != LogStyle.Unchanged)
        {
            result.Append('<');
            result.Append(Style);
            result.Append('>');
        }
        if (Color != LogColor.Unchanged)
        {
            result.Append('<');
            result.Append(Color);
            result.Append('>');
        }

        if (Formattable is FormattableString formattableString)
        {
            result.Append(formattableString.ToString(formatProvider));
        }
        else
        {
            result.Append(Formattable.ToString(null, formatProvider));
        }
        return result.ToString();
    }

    public string ToString(IFormatProvider? formatProvider) => ToString(null, formatProvider);

    #endregion Overrides
}
