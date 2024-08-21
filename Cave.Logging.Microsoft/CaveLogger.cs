#if NET5_0_OR_GREATER || NETSTANDARD2_0_OR_GREATER || NET462_OR_GREATER

using System;
using System.Diagnostics;
using System.Linq;

namespace Cave.Logging;

/// <summary>Implements the <see cref="Microsoft.Extensions.Logging.ILogger"/> interface using cave logging.</summary>
public class CaveLogger : Microsoft.Extensions.Logging.ILogger
{
    #region Public Constructors

    /// <summary>Creates a new instance</summary>
    /// <param name="category">Category to display (this equals the source field at cave logging)</param>
    public CaveLogger(string category) => Category = category;

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets the Category this logger logs to.</summary>
    public string Category { get; }

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    /// <inheritdoc/>
    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel == Microsoft.Extensions.Logging.LogLevel.None) return;
        var level = logLevel switch
        {
            Microsoft.Extensions.Logging.LogLevel.Warning => LogLevel.Warning,
            Microsoft.Extensions.Logging.LogLevel.Information => LogLevel.Information,
            Microsoft.Extensions.Logging.LogLevel.Error => LogLevel.Error,
            Microsoft.Extensions.Logging.LogLevel.Critical => LogLevel.Critical,
            Microsoft.Extensions.Logging.LogLevel.Debug => LogLevel.Debug,
            _ => LogLevel.Verbose,
        };

        var senderMethod = new StackTrace().GetFrames().Select(f => f.GetMethod()?.DeclaringType).SkipWhile(t => t?.FullName != "Microsoft.Extensions.Logging.Logger").First(t => t?.FullName == "Microsoft.Extensions.Logging.Logger");
        var senderType = senderMethod?.DeclaringType;
        var msg = new LogMessage(Category, senderType, level, $"{formatter.Invoke(state, exception)}", exception, null, null, 0);
        Logger.Send(msg);
    }

    #endregion Public Methods
}

#endif
