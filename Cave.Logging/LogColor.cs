namespace Cave.Logging;

/// <summary>Provides available logging colors.</summary>
public enum LogColor : byte
{
    /// <summary>Unchanged color</summary>
    Unchanged = 0,

    /// <summary>Reset color to system default</summary>
    Reset = 1,

    /// <summary>black</summary>
    Black = 100,

    /// <summary>gray</summary>
    Gray = 101,

    /// <summary>blue</summary>
    Blue = 102,

    /// <summary>green</summary>
    Green = 103,

    /// <summary>cyan</summary>
    Cyan = 104,

    /// <summary>red</summary>
    Red = 105,

    /// <summary>magenta</summary>
    Magenta = 106,

    /// <summary>yellow</summary>
    Yellow = 107,

    /// <summary>white</summary>
    White = 108
}
