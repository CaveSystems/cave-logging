namespace Cave.Logging;

/// <summary>Provides log extensions.</summary>
public static class LogExtension
{
    #region Static

    /// <summary>Obtains the color of a specified loglevel.</summary>
    /// <param name="level"></param>
    /// <returns></returns>
    public static LogColor GetLogLevelColor(this LogLevel level) => level switch
    {
        LogLevel.Emergency or LogLevel.Alert or LogLevel.Critical or LogLevel.Error => LogColor.Red,
        LogLevel.Warning => LogColor.Yellow,
        LogLevel.Notice => LogColor.Green,
        LogLevel.Information => LogColor.Cyan,
        LogLevel.Debug => LogColor.Gray,
        _ => LogColor.Blue,
    };

    #endregion Static
}
