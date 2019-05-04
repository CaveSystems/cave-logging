using System;
using System.IO;
using System.Text;
using Cave.Console;

namespace Cave.Logging
{
    /// <summary>
    /// Use this class to write messages directly to a utf-8 logfile.
    /// </summary>
    public sealed class LogFile : LogFileBase
    {
        #region default log files
        /// <summary>
        /// Gets/sets the used file extension for the logs.
        /// </summary>
        public static string FileExtension = ".log";

        /// <summary>
        /// Returns a <see cref="LogFile"/> instance for the local machine.
        /// </summary>
        public static LogFile CreateLocalMachineLogFile(LogFileFlags flags, string additionalPath = null)
        {
            return new LogFile(GetLocalMachineLogFileName(flags, additionalPath) + FileExtension);
        }

        /// <summary>
        /// Returns a <see cref="LogFile"/> instance for the local user.
        /// </summary>
        public static LogFile CreateLocalUserLogFile(LogFileFlags flags, string additionalPath = null)
        {
            return new LogFile(GetLocalUserLogFileName(flags, additionalPath) + FileExtension);
        }

        /// <summary>
        /// Returns a <see cref="LogFile"/> instance for the current (roaming) user.
        /// </summary>
        public static LogFile CreateUserLogFile(LogFileFlags flags, string additionalPath = null)
        {
            return new LogFile(GetUserLogFileName(flags, additionalPath) + FileExtension);
        }

        /// <summary>
        /// Returns a <see cref="LogFile"/> instance for the current running program in the programs startup directory.
        /// This should only be used for administration processes.
        /// Attention do nut use this for service processes!.
        /// </summary>
        /// <returns></returns>
        public static LogFile CreateProgramLogFile(LogFileFlags flags, string additionalPath = null)
        {
            return new LogFile(GetProgramLogFileName(flags, additionalPath) + FileExtension);
        }
        #endregion

        #region private implementation

        StreamWriter m_Writer = null;
        int m_Counter = 0;
        #endregion

        #region constructors
        void m_Init(string fileName)
        {
            if (m_Writer != null)
            {
                throw new InvalidOperationException(string.Format("LogFile already opened!"));
            }

            string fullFilePath = Path.GetFullPath(fileName);

            this.LogDebug("Prepare logging to file <cyan>{0}", fullFilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullFilePath));
            Stream stream = File.Open(fullFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.End);
            }

            m_Writer = new StreamWriter(stream, Encoding.UTF8);
        }

        /// <summary>
        /// Opens the specified logfile.
        /// </summary>
        /// <param name="fileName"></param>
        public LogFile(string fileName)
            : base(fileName)
        {
            m_Init(fileName);
        }

        /// <summary>
        /// Opens the specified logfile.
        /// </summary>
        /// <param name="level">The LogLevel to use initially.</param>
        /// <param name="fileName"></param>
        public LogFile(LogLevel level, string fileName)
            : base(fileName)
        {
            m_Init(fileName);
            Level = level;
        }
        #endregion

        /// <summary>Closes the <see cref="LogReceiver" />.</summary>
        public override void Close()
        {
            lock (this)
            {
                if (m_Writer != null)
                {
                    m_Writer.Close();
                    m_Writer = null;
                }
            }
            base.Close();
        }

        /// <summary>Writes the specified log message.</summary>
        /// <param name="dateTime">The date time.</param>
        /// <param name="level">The level.</param>
        /// <param name="source">The source.</param>
        /// <param name="content">The content.</param>
        protected override void Write(DateTime dateTime, LogLevel level, string source, XT content)
        {
            string text =
                dateTime.ToString(StringExtensions.InterOpDateTimeFormat) + " " +
                level + " " +
                source + ": " +
                content.Text;
            lock (this)
            {
                if (m_Writer == null)
                {
                    return;
                }

                m_Writer.WriteLine(text);
                m_Writer.Flush();
                m_Counter++;
            }
        }

        /// <summary>
        /// Obtains the number of notifications logged.
        /// </summary>
        public int Counter => m_Counter;

        /// <summary>
        /// Obtains the string "LogFile".
        /// </summary>
        public override string LogSourceName => $"LogFile <{Path.GetFileName(FileName)}>";
    }
}
