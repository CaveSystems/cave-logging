#if NET5_0_OR_GREATER || NETSTANDARD2_0_OR_GREATER || NET462_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;

namespace Cave.Logging;

/// <summary>Provides a <see cref="Microsoft.Extensions.Logging.ILoggerFactory"/> implementation using cave logging.</summary>
public class CaveLoggerFactory : Microsoft.Extensions.Logging.ILoggerFactory
{
    #region Public Properties

    /// <summary>Gets all providers this factory uses.</summary>
    public IEnumerable<Microsoft.Extensions.Logging.ILoggerProvider> Providers { get; } = [new CaveLoggerProvider()];

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc/>
    public void AddProvider(Microsoft.Extensions.Logging.ILoggerProvider provider) => throw new NotSupportedException();

    /// <inheritdoc/>
    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => Providers.First().CreateLogger(categoryName);

    /// <inheritdoc/>
    public void Dispose() => GC.SuppressFinalize(this);

    #endregion Public Methods
}

#endif
