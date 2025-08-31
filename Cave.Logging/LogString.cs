using System;

namespace Cave.Logging;

/// <summary>Log string helper class, to provide IFormattable strings at older frameworks.</summary>
public sealed class LogString : IFormattable
{
    #region Private Fields

    readonly string content;

    #endregion Private Fields

    #region Private Constructors

    LogString() => content = string.Empty;

    #endregion Private Constructors

    #region Internal Constructors

    internal LogString(string? content) => this.content = content ?? string.Empty;

    #endregion Internal Constructors

    #region Public Properties

    /// <summary>Provides an empty log string instance.</summary>
    public static LogString Empty { get; } = new LogString();

    #endregion Public Properties

    #region Public Methods

    /// <summary>Converts from string.</summary>
    /// <param name="s">String to convert</param>
    public static explicit operator LogString(string s) => new(s);

    /// <inheritdoc/>
    public override int GetHashCode() => DefaultHashingFunction.Calculate(content);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => content;

    /// <inheritdoc/>
    public override string ToString() => content;

    #endregion Public Methods
}
