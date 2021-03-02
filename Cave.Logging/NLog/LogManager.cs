using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace NLog
{
    public class LogManager
    {
        [Obsolete("This method is still very slow because it uses a StackFrame. Use new Logger(string name) instead to speed this up to 0.1% time consumption!")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Logger GetCurrentClassLogger() => new(new StackFrame(1).GetMethod()?.DeclaringType?.Name);

        [Obsolete("Use new Logger(string name) instead!")]
        public static Logger GetLogger(string name) => new Logger(name);

        [Obsolete("Use new LoggingSystem.Flush() instead!")]
        public static void Flush() => Logger.Flush();

        [Obsolete("Use new LoggingSystem.Close() instead!")]
        public static void Shutdown() => Logger.Close();
    }
}
