using System;
using System.IO;

namespace Cave.Logging
{
    /// <summary>
    /// Provides a base class for file logging.
    /// </summary>
    /// <seealso cref="Cave.Logging.LogReceiver"/>
    public abstract class LogFileBase : LogReceiver
    {
        #region Constructors

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LogFileBase"/> class.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        protected LogFileBase(string fileName) => FileName = fileName;

        #endregion constructors

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <value>The name of the file.</value>
        public string FileName { get; }

        #endregion Properties

        #region Members

        /// <summary>
        /// Returns the current size of the logfile.
        /// </summary>
        /// <returns></returns>
        public long GetSize() => new FileInfo(FileName).Length;

        #endregion Members

        #region default log files

        static string GetFileName(LogFileFlags flags)
        {
            var fileName = AssemblyVersionInfo.Program.Product;
            if ((flags & LogFileFlags.UseDateTimeFileName) != 0)
            {
                fileName += " " + DateTime.Now.ToString(StringExtensions.FileNameDateTimeFormat);
            }

            return fileName;
        }

        static string GetFullPath(string basePath, string additionalPath, LogFileFlags flags)
        {
            var fileName = GetFileName(flags);
            var ver = AssemblyVersionInfo.Program;
            if ((flags & LogFileFlags.UseCompanyName) != 0)
            {
                fileName = Path.GetFullPath($"{basePath}/{ver.Company}/{ver.Product}/{additionalPath}/Logs/{fileName}");
            }
            else
            {
                fileName = Path.GetFullPath($"{basePath}/{ver.Product}/{additionalPath}/Logs/{fileName}");
            }

            return fileName;
        }

        /// <summary>
        /// Rotates the logfile and keeps a specified number of old logfiles.
        /// </summary>
        static void Rotate(string fileName, int keepOldFilesCount)
        {
            if (keepOldFilesCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(keepOldFilesCount));
            }

            var logger = new Logger("LogFile");
            logger.Info("RotationStart");
            var fullFilePath = fileName;
            var fileCount = 0;
            for (var i = keepOldFilesCount - 1; i >= 0; i--)
            {
                var oldFile = fullFilePath;
                if (i > 0)
                {
                    oldFile += "." + (i - 1);
                }

                if (File.Exists(oldFile))
                {
                    if (fileCount < i) { fileCount = i + 1; }

                    var newFile = fullFilePath + "." + i;
                    if (File.Exists(newFile))
                    {
                        File.Delete(newFile);
                    }

                    File.Move(oldFile, newFile);
                }
            }

            logger.Info("RotationComplete {0} files.", fileCount);
        }

        /// <summary>
        /// Returns the log file name for the local machine.
        /// </summary>
        /// <returns>Returns a file name with full path without extension.</returns>
        /// <remarks>This function applies rotation.</remarks>
        protected static string GetLocalMachineLogFileName(LogFileFlags flags, string additionalPath = null)
        {
            string filename = Platform.Type switch
            {
                PlatformType.Linux => FileSystem.Combine("/var", "log", additionalPath, GetFileName(flags)),
                _ => GetFullPath(FileSystem.LocalMachineConfiguration, additionalPath, flags)
            };

            if ((flags & LogFileFlags.UseRotation) != 0)
            {
                Rotate(filename, 10);
            }

            return filename;
        }

        /// <summary>
        /// Returns the log file name for the local user.
        /// </summary>
        /// <returns>Returns a file name with full path without extension.</returns>
        /// <remarks>This function applies rotation.</remarks>
        protected static string GetLocalUserLogFileName(LogFileFlags flags, string additionalPath = null)
        {
            string filename = Platform.Type switch
            {
                PlatformType.Linux => FileSystem.Combine("~", ".local", "log", additionalPath, GetFileName(flags)),
                _ => GetFullPath(FileSystem.LocalUserConfiguration, additionalPath, flags)
            };

            if ((flags & LogFileFlags.UseRotation) != 0)
            {
                Rotate(filename, 10);
            }

            return filename;
        }

        /// <summary>
        /// Returns the log file name for the current running program in the programs startup directory. This should only be used for administration processes.
        /// Attention do nut use this for service processes!.
        /// </summary>
        /// <returns>Returns a file name with full path without extension.</returns>
        /// <remarks>This function applies rotation.</remarks>
        protected static string GetProgramLogFileName(LogFileFlags flags, string additionalPath = null)
        {
            var fileName = GetFileName(flags);
            var path = FileSystem.ProgramDirectory;
            fileName = FileSystem.Combine(path, additionalPath, "logs", fileName);
            if ((flags & LogFileFlags.UseRotation) != 0)
            {
                Rotate(fileName, 10);
            }

            return fileName;
        }

        /// <summary>
        /// Returns the log file name for the current (roaming) user.
        /// </summary>
        /// <returns>Returns a file name with full path without extension.</returns>
        /// <remarks>This function applies rotation.</remarks>
        protected static string GetUserLogFileName(LogFileFlags flags, string additionalPath = null)
        {
            string filename = Platform.Type switch
            {
                PlatformType.Linux => FileSystem.Combine("~", "log", additionalPath, GetFileName(flags)),
                _ => GetFullPath(FileSystem.UserConfiguration, additionalPath, flags)
            };

            if ((flags & LogFileFlags.UseRotation) != 0)
            {
                Rotate(filename, 10);
            }

            return filename;
        }

        #endregion default log files
    }
}
