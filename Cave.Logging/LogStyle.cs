using System;

namespace Cave.Logging;

/// <summary>Provides available logging colors.</summary>
[Flags]
public enum LogStyle : byte
{
    /// <summary>No change</summary>
    Unchanged = 0,

    /// <summary>Reset style (=will reset color and style to system default)</summary>
    Reset = 1 << 0,

    /// <summary>Bold font</summary>
    Bold = 1 << 1,

    /// <summary>Italic font</summary>
    Italic = 1 << 2,

    /// <summary>Underline</summary>
    Underline = 1 << 3,

    /// <summary>Strikeout</summary>
    Strikeout = 1 << 4,

    /// <summary>Inverse colors (sets bg color instead of fg color)</summary>
    Inverse = 1 << 5,
}
