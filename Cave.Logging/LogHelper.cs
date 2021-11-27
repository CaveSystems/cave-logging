// this class defines always DEBUG and TRACE

#define TRACE
#define DEBUG

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Cave.Logging
{
    /// <summary>internal log helper class used to write to trace and debug even if the lib is in release mode.</summary>
    [ExcludeFromCodeCoverage]
    static class LogHelper
    {
        #region Static

        /// <summary>Writes a line to the <see cref="Debug"/> output.</summary>
        /// <param name="msg"></param>
        public static void DebugLine(string msg) => Debug.WriteLine(msg, "Logging");

        /// <summary>Writes a line to the <see cref="Trace"/> output.</summary>
        /// <param name="msg"></param>
        public static void TraceLine(string msg) => Trace.WriteLine(msg, "Logging");

        #endregion Static
    }
}
