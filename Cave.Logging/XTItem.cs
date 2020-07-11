using System;
using System.Text;

namespace Cave
{
    /// <summary>
    /// Provides very simple html style extended text attributes.
    /// </summary>
    public class XTItem
    {
        /// <summary>Implements the operator +.</summary>
        /// <param name="x1">The first item to add.</param>
        /// <param name="x2">The second item to add.</param>
        /// <returns>The result of the operator.</returns>
        public static XT operator +(XTItem x1, XTItem x2)
        {
            return new XT(x1, x2);
        }

        /// <summary>Implements the operator ==.</summary>
        /// <param name="x1">The first item.</param>
        /// <param name="x2">The second item.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(XTItem x1, XTItem x2) => x1?.ToString() == x2?.ToString();

        /// <summary>Implements the operator !=.</summary>
        /// <param name="x1">The first item.</param>
        /// <param name="x2">The second item.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(XTItem x1, XTItem x2) => x1?.ToString() != x2?.ToString();

        /// <summary>
        /// Provides a new line item.
        /// </summary>
        public static readonly XTItem NewLine = new XTItem(XTColor.Default, XTStyle.Default, Environment.NewLine);

        /// <summary>
        /// Initializes a new instance of the <see cref="XTItem"/> class.
        /// </summary>
        /// <param name="color">Color of the item.</param>
        /// <param name="style">Style of the item.</param>
        /// <param name="text">Text.</param>
        public XTItem(XTColor color, XTStyle style, string text)
        {
            Color = color;
            Style = style;
            Text = text;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XTItem"/> class.
        /// </summary>
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

        /// <summary>
        /// Gets the Color of the item.
        /// </summary>
        public readonly XTColor Color;

        /// <summary>
        /// Gets the Style of the item.
        /// </summary>
        public readonly XTStyle Style;

        /// <summary>
        /// Gets the text of the item.
        /// </summary>
        public readonly string Text;

        /// <summary>
        /// Gets the full data text repesentation of the item containing style color and text.
        /// </summary>
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

        /// <summary>Determines whether the specified <see cref="object" />, is equal to this instance.</summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj) =>
            obj is XTItem other
            ? other.Color == Color && other.Style == Style && other.Text == Text
            : false;

        /// <summary>Returns a hash code for this instance.</summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. </returns>
        public override int GetHashCode() => Text.GetHashCode() ^ Style.GetHashCode() ^ Color.GetHashCode();
    }
}
