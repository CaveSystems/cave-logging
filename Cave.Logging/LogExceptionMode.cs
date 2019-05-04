using System;

namespace Cave.Logging
{
    /// <summary>
    /// Provides exception logging modes.
    /// </summary>
    [Flags]
    public enum LogExceptionMode
    {
        /// <summary>No exception logging</summary>
        None = 0,

        /// <summary>Log exceptions with full message (messages of all exceptions including inner exeptions).</summary>
        IncludeChildren = 1,

        /// <summary>Log exceptions with the same level the error message is logged with</summary>
        SameLevel = 2,

        /// <summary>Add a full stacktrace to the exception log</summary>
        StackTrace = 4,

        /// <summary>full logging</summary>
        Full = 0xffff,
    }
}
