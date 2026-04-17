using System;
using System.Collections.Generic;
using System.Globalization;

namespace Cave.Logging;

/// <summary>Provides an interface for message formatting</summary>
/// <remarks>Converts <see cref="LogMessage"/> instances into formatted <see cref="ILogText"/> parts.</remarks>
public interface ILogMessageFormatter
{
    #region Public Properties

    /// <summary>Gets or sets the date format</summary>
    /// <remarks>Standard .NET date format string.</remarks>
    string DateFormat { get; set; }

    /// <summary>Gets or sets the date time format</summary>
    /// <remarks>Standard .NET date time format string.</remarks>
    string DateTimeFormat { get; set; }

    /// <summary>Gets or sets the exception mode for the formatter</summary>
    LogExceptionMode ExceptionMode { get; set; }

    /// <summary>Gets or sets the format provider</summary>
    /// <remarks>Culture-specific formatting. If null, implementations should use <see cref="CultureInfo.InvariantCulture"/>.</remarks>
    IFormatProvider FormatProvider { get; set; }

    /// <summary>Gets or sets the log message format</summary>
    /// <remarks>Pattern used to compose the formatted message output.</remarks>
    string MessageFormat { get; set; }

    /// <summary>Gets or sets the time format</summary>
    /// <remarks>Standard .NET time format string.</remarks>
    string TimeFormat { get; set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Formats a message</summary>
    /// <param name="message">Message to format.</param>
    /// <returns>Returns a list of formatted <see cref="ILogText"/> instances</returns>
    IList<ILogText> FormatMessage(LogMessage message);

    #endregion Public Methods
}
