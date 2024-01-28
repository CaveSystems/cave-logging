using System;
using System.Collections.Generic;
using System.Linq;

namespace Cave.Logging;

/// <summary>Provides a <see cref="Microsoft.Extensions.Logging.ILoggerFactory"/> implementation using cave logging.</summary>
public class CaveLoggerFactory : Microsoft.Extensions.Logging.ILoggerFactory
{
    #region Public Properties

    /// <summary>Gets all providers this factory uses.</summary>
    public IEnumerable<Microsoft.Extensions.Logging.ILoggerProvider> Providers { get; } = new[] { new CaveLoggerProvider() };

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc/>
    public void AddProvider(Microsoft.Extensions.Logging.ILoggerProvider provider) => throw new NotSupportedException();

    /// <inheritdoc/>
    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => Providers.First().CreateLogger(categoryName);

    /// <inheritdoc/>
    public void Dispose() { }

    #endregion Public Methods
}
