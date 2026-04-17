using System;

namespace Cave.Logging;

/// <summary>Represents an object that can be rendered as formatted log text with color and styling information.</summary>
public interface ILogText : IEquatable<ILogText>
{
    #region Public Properties

    /// <summary>Gets the display color for the log text.</summary>
    LogColor Color { get; }

    /// <summary>Gets the text styling for the log output.</summary>
    LogStyle Style { get; }

    /// <summary>Gets the content text to be displayed.</summary>
    string Text { get; }

    #endregion Public Properties
}
