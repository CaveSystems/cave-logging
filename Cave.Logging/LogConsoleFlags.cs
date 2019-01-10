using System;

namespace Cave.Logging
{
    /// <summary>
    /// Provides flags for use with the <see cref="LogConsole"/> class.
    /// </summary>
    [Flags]
    public enum LogConsoleFlags
    {
        /// <summary>The default setting: with long level text, timestamp and source</summary>
        Default = DisplayLongLevel | DisplayTimeStamp | DisplaySource,

        /// <summary>
        /// Short default setting: one letter level and timestamp
        /// </summary>
        DefaultShort = DisplayOneLetterLevel | DisplayTimeStamp,

        /// <summary>
        /// Do not use any flags. This simply displays the messages without timestamp, level, source, ...
        /// </summary>
        None = 0,

        /// <summary>
        /// Display the creation timestamp of each message
        /// </summary>
        DisplayTimeStamp = 1,

        /// <summary>
        /// Display the log level of each message
        /// </summary>
        DisplayOneLetterLevel = 2,

        /// <summary>
        /// Display the source of each message
        /// </summary>
        DisplaySource = 4,

        /// <summary>
        /// Display the log level of each message (full name)
        /// </summary>
        DisplayLongLevel = 8,
    }
}
