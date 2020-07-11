namespace Cave.Logging
{
    /// <summary>
    /// Provides log extensions.
    /// </summary>
    public static class LogExtension
    {
        static string GetLogSourceName(object source)
        {
            if (source == null)
            {
                return "null";
            }

            if (source is string text)
            {
                return text;
            }

            return source.GetType().Name;
        }

        /// <summary>
        /// Obtains the color of a specified loglevel.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static XTColor GetLogLevelColor(this LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Emergency:
                case LogLevel.Alert:
                case LogLevel.Critical:
                case LogLevel.Error:
                    return XTColor.Red;
                case LogLevel.Warning:
                    return XTColor.Yellow;
                case LogLevel.Notice:
                    return XTColor.Green;
                case LogLevel.Information:
                    return XTColor.Cyan;
                case LogLevel.Debug:
                    return XTColor.Default;
                default:
                    return XTColor.Blue;
            }
        }
    }
}
