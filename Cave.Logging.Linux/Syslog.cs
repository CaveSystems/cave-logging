using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Cave.Logging;

/// <summary>Provides access to the *nix logging deamon.</summary>
public static class Syslog
{
    #region Private Fields

    static readonly object SyncRoot = new();
    static IntPtr processNamePtr;

    #endregion Private Fields

    #region Public Methods

    /// <summary>Closes the connection to the logging deamon.</summary>
    public static void Close()
    {
        lock (SyncRoot)
        {
            if (processNamePtr != IntPtr.Zero)
            {
                libc.SafeNativeMethods.closelog();
                if (processNamePtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(processNamePtr);
                }

                processNamePtr = IntPtr.Zero;
            }
        }
    }

    /// <summary>Starts logging to the logging deamon.</summary>
    public static void Init() => Init(SyslogOption.Pid | SyslogOption.NoDelay, SyslogFacility.Local1);

    /// <summary>Starts logging to the logging deamon.</summary>
    /// <param name="option">The syslog option.</param>
    /// <param name="facility">The syslog facility.</param>
    public static void Init(SyslogOption option, SyslogFacility facility)
    {
        if (processNamePtr != IntPtr.Zero)
        {
            return;
        }

        var processName = Process.GetCurrentProcess().ProcessName;
        processNamePtr = Marshal.StringToHGlobalAnsi(processName);
        libc.SafeNativeMethods.openlog(processNamePtr, new IntPtr((int)option), new IntPtr((int)facility));
    }

    /// <summary>Logs a message.</summary>
    /// <param name="severity">The syslog severity.</param>
    /// <param name="facility">The syslog facility.</param>
    /// <param name="msg">The message tring to log.</param>
    public static void Write(SyslogSeverity severity, SyslogFacility facility, string msg)
    {
        lock (SyncRoot)
        {
            if (processNamePtr == IntPtr.Zero)
            {
                return;
            }

            var priority = (((int)facility) << 3) | ((int)severity);
            libc.SafeNativeMethods.syslog(priority, msg);
        }
    }

    #endregion Public Methods
}
