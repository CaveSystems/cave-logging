namespace Cave.Logging
{
    /// <summary>Provides a list of valid syslog facilities.</summary>
    public enum SyslogFacility
    {
        /// <summary>Kernel messages</summary>
        Kernel = 0,

        /// <summary>Random user-level messages</summary>
        User = 1,

        /// <summary>Mail system</summary>
        Mail = 2,

        /// <summary>System daemons</summary>
        System = 3,

        /// <summary>Security/authorization messages</summary>
        Security = 4,

        /// <summary>Messages generated internally by syslog deamon/server</summary>
        Syslog = 5,

        /// <summary>Line printer subsystem</summary>
        Printer = 6,

        /// <summary>Network news subsystem</summary>
        Network = 7,

        /// <summary>UUCP subsystem</summary>
        UUCP = 8,

        /// <summary>Clock daemon</summary>
        Clock = 9,

        /// <summary>Security/authorization messages (private)</summary>
        Auth = 10,

        /// <summary>ftp daemon</summary>
        FTP = 11,

        /// <summary>ntp daemon</summary>
        NTP = 12,

        /// <summary>Audit logging</summary>
        LogAudit = 13,

        /// <summary>Alert logging</summary>
        LogAlert = 14,

        /// <summary>Cron daemon</summary>
        Cron = 15,

        /// <summary>Reserved for local use</summary>
        Local0 = 16,

        /// <summary>Reserved for local use</summary>
        Local1 = 17,

        /// <summary>Reserved for local use</summary>
        Local2 = 18,

        /// <summary>Reserved for local use</summary>
        Local3 = 19,

        /// <summary>Reserved for local use</summary>
        Local4 = 20,

        /// <summary>Reserved for local use</summary>
        Local5 = 21,

        /// <summary>Reserved for local use</summary>
        Local6 = 22,

        /// <summary>Reserved for local use</summary>
        Local7 = 23
    }
}
