namespace Cave.Logging;

/// <summary>Provides syslog severity constants.</summary>
public enum SyslogSeverity
{
    /// <summary>System is unusable</summary>
    Emergency = 0,

    /// <summary>Action must be taken immediately</summary>
    Alert = 1,

    /// <summary>Critical conditions</summary>
    Critical = 2,

    /// <summary>Error conditions</summary>
    Error = 3,

    /// <summary>Warning conditions</summary>
    Warning = 4,

    /// <summary>Normal but significant condition</summary>
    Notice = 5,

    /// <summary>Informational</summary>
    Information = 6,

    /// <summary>Debug-level messages</summary>
    Debug = 7
}