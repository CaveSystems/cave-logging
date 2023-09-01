namespace Cave.Logging;

/// <summary>Provides available modes for <see cref="ILogReceiver"/>.</summary>
public enum LogReceiverMode
{
    /// <summary>The undefined mode</summary>
    Undefined = 0,

    /// <summary>Opportune logging: The <see cref="ILogReceiver"/> may discard old messages to keep up</summary>
    Opportune,

    /// <summary>Continuous logging: The <see cref="ILogReceiver"/> caches all messages and write them out whenever possible</summary>
    Continuous
}