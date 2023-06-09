using System;
using System.Collections.Generic;
using Cave.Logging;

namespace Cave;

/// <summary>Provides building of an eXtended Text.</summary>
/// <seealso cref="ILogText"/>
public sealed class LogTextBuilder : ILogText
{
    #region Static

    /// <summary>Performs an implicit conversion from <see cref="LogTextBuilder"/> to <see cref="LogText"/>.</summary>
    /// <param name="xb">The LogTextBuilder.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator LogText(LogTextBuilder xb) => xb.ToLogText();

    #endregion Static

    #region Private Fields

    readonly List<LogTextItem> items = new();

    #endregion Private Fields

    #region Properties

    /// <summary>Gets the items.</summary>
    /// <value>The items.</value>
    public LogTextItem[] Items => items.ToArray();

    #endregion Properties

    #region IXT Members

    /// <summary>Provides an eXtended Text string for this object.</summary>
    /// <returns>Returns a new LogText instance with the description of this object.</returns>
    public LogText ToLogText() => Items;

    #endregion IXT Members

    #region Overrides

    /// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
    /// <returns>A <see cref="string"/> that represents this instance.</returns>
    public override string ToString() => ToLogText().ToString();

    #endregion Overrides

    #region Members

    /// <summary>Appends the specified object.</summary>
    /// <param name="obj">The object.</param>
    public void Append(ILogText obj)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        Append(obj.ToLogText());
    }

    /// <summary>Appends text with the specified color.</summary>
    /// <param name="color">The color.</param>
    /// <param name="text">The text.</param>
    public void Append(LogColor color, string text) => items.Add(new LogTextItem(color, text));

    /// <summary>Appends the specified item.</summary>
    /// <param name="item">The item.</param>
    public void Append(LogTextItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        items.Add(item);
    }

    /// <summary>Appends the specified text.</summary>
    /// <param name="text">The text.</param>
    public void Append(LogText text)
    {
        if (text is null)
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

        Append(LogText.Format(text, args));
    }

    /// <summary>Appends the line.</summary>
    public void AppendLine() => items.Add(LogTextItem.NewLine);

    /// <summary>Appends the specified object and a newline.</summary>
    /// <param name="obj">The object.</param>
    public void AppendLine(ILogText obj)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        AppendLine(obj.ToLogText());
    }

    /// <summary>Appends the specified item and a newline.</summary>
    /// <param name="item">The item.</param>
    public void AppendLine(LogTextItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        items.Add(item);
        items.Add(LogTextItem.NewLine);
    }

    /// <summary>Appends the specified text and a newline.</summary>
    /// <param name="text">The text.</param>
    public void AppendLine(LogText text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        items.AddRange(text.Items);
        items.Add(LogTextItem.NewLine);
    }

    /// <summary>Appends the specified text and a newline.</summary>
    /// <param name="text">The text.</param>
    /// <param name="args">The arguments.</param>
    public void AppendLine(string text, params object[] args)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        Append(LogText.Format(text, args));
        items.Add(LogTextItem.NewLine);
    }

    #endregion Members
}
