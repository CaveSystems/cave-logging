namespace Cave.Logging;

/// <summary>Provides logging for new core and via <see cref="Microsoft.Extensions.Logging.ILoggerProvider"/> interface.</summary>
public class CaveLoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider
{
    #region Public Methods

    /// <inheritdoc/>
    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => new CaveLogger(categoryName);

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    #endregion Public Methods
}
