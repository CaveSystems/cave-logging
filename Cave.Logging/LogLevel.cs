namespace Cave.Logging
{
    /// <summary>Loglevels used to seperate between logged and not logged messages (logsensibility).</summary>
    public enum LogLevel
    {
        /// <summary>(0) An emergency that has to be handled immediatly by the user</summary>
        Emergency = 0x0,

        /// <summary>(1) An alert that has to be handled by the user</summary>
        Alert = 0x1,

        /// <summary>(2) Critical errors that definitly broke the current operation</summary>
        Critical = 0x2,

        /// <summary>(3) Errors that have to be handled</summary>
        Error = 0x3,

        /// <summary>(4) Warnings that could be handled but should not happen</summary>
        Warning = 0x4,

        /// <summary>(5) Notices the user should see</summary>
        Notice = 0x5,

        /// <summary>(6) Informational messages not really needed</summary>
        Information = 0x6,

        /// <summary>(7) Debugmessages</summary>
        Debug = 0x7,

        /// <summary>(8) Any available messages (Debug very verbose)</summary>
        Verbose = 0x8
    }
}
