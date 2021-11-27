namespace Cave.Logging
{
    /// <summary>Provides flags for log file creation.</summary>
    public enum LogFileFlags
    {
        /// <summary>No Flags</summary>
        None = 0,

        /// <summary>Use rotation before creating new logfile.</summary>
        UseRotation = 1,

        /// <summary>Use a fileName based on the date and time. (This disables UseRotation)</summary>
        UseDateTimeFileName = 2,

        /// <summary>Use a company name in the path.</summary>
        UseCompanyName = 4
    }
}
