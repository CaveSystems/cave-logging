#if NET5_0_OR_GREATER || NETSTANDARD2_0_OR_GREATER || NET462_OR_GREATER

using System;

namespace Cave.Logging;

/// <summary>Provides logging for new core and via <see cref="Microsoft.Extensions.Logging.ILoggerProvider"/> interface.</summary>
public class CaveLoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider
{
    #region Public Methods

    /// <inheritdoc/>
    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => new CaveLogger(categoryName);

    /// <inheritdoc/>
    public void Dispose() => GC.SuppressFinalize(this);

    #endregion Public Methods
}

#endif
