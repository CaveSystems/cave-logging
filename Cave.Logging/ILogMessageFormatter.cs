using System;
using System.Collections.Generic;
using System.Globalization;

namespace Cave.Logging;

/// <summary>
/// Provides an interface for message formatting
/// </summary>
public interface ILogMessageFormatter
{
    /// <summary>
    /// Gets or sets the format provider
    /// </summary>
    IFormatProvider FormatProvider { get; set; }

    /// <summary>
    /// Gets or sets the exception mode for the formatter
    /// </summary>
    LogExceptionMode ExceptionMode { get; set; } 

    /// <summary>
    /// Gets or sets the date time format
    /// </summary>
    string DateTimeFormat { get; set; }

    /// <summary>
    /// Gets or sets the log message format
    /// </summary>
    string MessageFormat { get; set; }

    /// <summary>Formats a message</summary>
    /// <param name="message">Message to format.</param>
    /// <returns>Returns a list of formatted <see cref="ILogText"/> instances</returns>
    IList<ILogText> FormatMessage(LogMessage message);
}
