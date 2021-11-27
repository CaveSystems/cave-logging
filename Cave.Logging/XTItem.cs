using System;
using System.Text;

namespace Cave
{
    /// <summary>Provides very simple html style extended text attributes.</summary>
    public class XTItem : IEquatable<XTItem>
    {
        #region Static

        /// <summary>Provides a new line item.</summary>
        public static readonly XTItem NewLine = new(XTColor.Default, XTStyle.Default, Environment.NewLine);

        /// <summary>Implements the operator !=.</summary>
        /// <param name="x1">The first item.</param>
        /// <param name="x2">The second item.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(XTItem x1, XTItem x2) => x1?.ToString() != x2?.ToString();

        /// <summary>Implements the operator +.</summary>
        /// <param name="x1">The first item to add.</param>
        /// <param name="x2">The second item to add.</param>
        /// <returns>The result of the operator.</returns>
        public static XT operator +(XTItem x1, XTItem x2) => new(x1, x2);

        /// <summary>Implements the operator ==.</summary>
        /// <param name="x1">The first item.</param>
        /// <param name="x2">The second item.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(XTItem x1, XTItem x2) => x1?.ToString() == x2?.ToString();

        #endregion Static

        #region Public Fields

        /// <summary>Gets the Color of the item.</summary>
        public readonly XTColor Color;

        /// <summary>Gets the Style of the item.</summary>
        public readonly XTStyle Style;

        /// <summary>Gets the text of the item.</summary>
        public readonly string Text;

        #endregion Public Fields

        #region Constructors

        /// <summary>Initializes a new instance of the <see cref="XTItem"/> class.</summary>
        /// <param name="color">Color of the item.</param>
        /// <param name="style">Style of the item.</param>
        /// <param name="text">Text.</param>
        public XTItem(XTColor color, XTStyle style, string text)
        {
            Color = color;
            Style = style;
            Text = text;
        }

        /// <summary>Initializes a new instance of the <see cref="XTItem"/> class.</summary>
        /// <param name="color">Color of the item.</param>
        /// <param name="text">Text.</param>
        public XTItem(XTColor color, string text)
        {
            Color = color;
            Style = XTStyle.Default;
            Text = text;
        }

        /// <summary>Initializes a new instance of the <see cref="XTItem"/> class.</summary>
        /// <param name="text">The text.</param>
        public XTItem(string text)
        {
            Color = XTColor.Default;
            Style = XTStyle.Default;
            Text = text;
        }

        #endregion Constructors

        #region Overrides

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is XTItem other && Equals(other);

        /// <inheritdoc/>
        public bool Equals(XTItem other) => (other.Color == Color) && (other.Style == Style) && (other.Text == Text);

        /// <inheritdoc/>
        public override int GetHashCode() => Text.GetHashCode() ^ Style.GetHashCode() ^ Color.GetHashCode();

        /// <summary>Gets the full data text repesentation of the item containing style color and text.</summary>
        /// <returns>Returns a parsable string.</returns>
        public override string ToString()
        {
            var result = new StringBuilder();
            if (Style != XTStyle.Default)
            {
                result.Append('<');
                result.Append(Style);
                result.Append('>');
            }

            if (Color != XTColor.Default)
            {
                result.Append('<');
                result.Append(Color);
                result.Append('>');
            }

            result.Append(Text);
            return result.ToString();
        }

        #endregion Overrides
    }
}
