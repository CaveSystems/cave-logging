namespace Cave.Logging;

/// <summary>Provides available modes for <see cref="LogReceiver"/>.</summary>
public enum LogReceiverMode
{
    /// <summary>The undefined mode</summary>
    Undefined = 0,

    /// <summary>Opportune logging: The <see cref="LogReceiver"/> may discard old messages to keep up</summary>
    Opportune,

    /// <summary>Continuous logging: The <see cref="LogReceiver"/> caches all messages and write them out whenever possible</summary>
    Continuous
}
