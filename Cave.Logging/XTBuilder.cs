using System;
using System.Collections.Generic;

namespace Cave
{
    /// <summary>
    /// Provides building of an eXtended Text.
    /// </summary>
    /// <seealso cref="IXT" />
    public sealed class XTBuilder : IXT
    {
        /// <summary>Performs an implicit conversion from <see cref="XTBuilder"/> to <see cref="XT"/>.</summary>
        /// <param name="xb">The XTBuilder.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator XT(XTBuilder xb)
        {
            return xb.ToXT();
        }

        List<XTItem> items = new List<XTItem>();

        /// <summary>Appends the specified object.</summary>
        /// <param name="obj">The object.</param>
        public void Append(IXT obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            Append(obj.ToXT());
        }

        /// <summary>Appends text with the specified color.</summary>
        /// <param name="color">The color.</param>
        /// <param name="text">The text.</param>
        public void Append(XTColor color, string text)
        {
            items.Add(new XTItem(color, text));
        }

        /// <summary>Appends the specified item.</summary>
        /// <param name="item">The item.</param>
        public void Append(XTItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            items.Add(item);
        }

        /// <summary>Appends the specified text.</summary>
        /// <param name="text">The text.</param>
        public void Append(XT text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            items.AddRange(text.Items);
        }

        /// <summary>Appends the specified text.</summary>
        /// <param name="text">The text.</param>
        /// <param name="args">The arguments.</param>
        public void Append(string text, params object[] args)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            Append(XT.Format(text, args));
        }

        /// <summary>Appends the line.</summary>
        public void AppendLine()
        {
            items.Add(XTItem.NewLine);
        }

        /// <summary>Appends the specified object and a newline.</summary>
        /// <param name="obj">The object.</param>
        public void AppendLine(IXT obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            AppendLine(obj.ToXT());
        }

        /// <summary>Appends the specified item and a newline.</summary>
        /// <param name="item">The item.</param>
        public void AppendLine(XTItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            items.Add(item);
            items.Add(XTItem.NewLine);
        }

        /// <summary>Appends the specified text and a newline.</summary>
        /// <param name="text">The text.</param>
        public void AppendLine(XT text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            items.AddRange(text.Items);
            items.Add(XTItem.NewLine);
        }

        /// <summary>Appends the specified text and a newline.</summary>
        /// <param name="text">The text.</param>
        /// <param name="args">The arguments.</param>
        public void AppendLine(string text, params object[] args)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            Append(XT.Format(text, args));
            items.Add(XTItem.NewLine);
        }

        /// <summary>Gets the items.</summary>
        /// <value>The items.</value>
        public XTItem[] Items => items.ToArray();

        /// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
        /// <returns>A <see cref="string" /> that represents this instance.</returns>
        public override string ToString()
        {
            return ToXT().ToString();
        }

        /// <summary>Provides an eXtended Text string for this object.</summary>
        /// <returns>Returns a new XT instance with the description of this object.</returns>
        public XT ToXT()
        {
            return Items;
        }
    }
}
