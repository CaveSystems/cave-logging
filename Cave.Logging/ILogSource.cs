namespace Cave.Logging
{
    /// <summary>
    /// Defines a log source name for classes implementing this interface.
    /// This is needed when using the logger with obfuscated assemblies (since the type names are garbage).
    /// </summary>
    public interface ILogSource
    {
        /// <summary>Gets the name of the log source.</summary>
        /// <value>The name of the log source.</value>
        string LogSourceName { get; }
    }
}
