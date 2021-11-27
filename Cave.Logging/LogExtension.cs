namespace Cave.Logging
{
    /// <summary>Provides log extensions.</summary>
    public static class LogExtension
    {
        #region Static

        /// <summary>Obtains the color of a specified loglevel.</summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static XTColor GetLogLevelColor(this LogLevel level) => level switch
        {
            LogLevel.Emergency or LogLevel.Alert or LogLevel.Critical or LogLevel.Error => XTColor.Red,
            LogLevel.Warning => XTColor.Yellow,
            LogLevel.Notice => XTColor.Green,
            LogLevel.Information => XTColor.Cyan,
            LogLevel.Debug => XTColor.Default,
            _ => XTColor.Blue,
        };

        #endregion Static
    }
}
