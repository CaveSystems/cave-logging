using System;

namespace Cave.Logging;

/// <summary>Provides an interface for structs / objects supporting the ToLogText() method.</summary>
public interface ILogText
{
    /// <summary>Gets the Color of the item.</summary>
    LogColor Color { get; }

    /// <summary>Gets the Style of the item.</summary>
    LogStyle Style { get; }

    /// <summary>Gets the text of the item.</summary>
    string Text { get; }
}
