﻿using System;
using System.IO;
using Cave.Console;
using res = Cave.Logging.Properties.Resources;

namespace Cave.Logging
{
    /// <summary>
    /// Use this class to write messages directly to a html logfile.
    /// </summary>
    /// <seealso cref="Cave.Logging.LogFileBase" />
    public class LogHtmlFile : LogFileBase
    {
        #region default log files

        /// <summary>
        /// Gets or sets the used file extension for the html logs.
        /// </summary>
        public static string FileExtension = ".html";

        /// <summary>
        /// Returns a <see cref="LogHtmlFile"/> instance for the local machine.
        /// </summary>
        public static LogHtmlFile CreateLocalMachineLogFile(LogFileFlags flags, string additionalPath = null)
        {
            return new LogHtmlFile(GetLocalMachineLogFileName(flags, additionalPath) + FileExtension);
        }

        /// <summary>
        /// Returns a <see cref="LogHtmlFile"/> instance for the local user.
        /// </summary>
        public static LogHtmlFile CreateLocalUserLogFile(LogFileFlags flags, string additionalPath = null)
        {
            return new LogHtmlFile(GetLocalUserLogFileName(flags, additionalPath) + FileExtension);
        }

        /// <summary>
        /// Returns a <see cref="LogHtmlFile"/> instance for the current (roaming) user.
        /// </summary>
        public static LogHtmlFile CreateUserLogFile(LogFileFlags flags, string additionalPath = null)
        {
            return new LogHtmlFile(GetUserLogFileName(flags, additionalPath) + FileExtension);
        }

        /// <summary>
        /// Returns a <see cref="LogHtmlFile"/> instance for the current running program in the programs startup directory.
        /// This should only be used for administration processes.
        /// Attention do nut use this for service processes!.
        /// </summary>
        /// <returns></returns>
        public static LogHtmlFile CreateProgramLogFile(LogFileFlags flags, string additionalPath = null)
        {
            return new LogHtmlFile(GetProgramLogFileName(flags, additionalPath) + FileExtension);
        }
        #endregion

        StreamWriter writer;

        /// <summary>Writes the specified log message.</summary>
        /// <param name="dateTime">The date time.</param>
        /// <param name="level">The level.</param>
        /// <param name="source">The source.</param>
        /// <param name="content">The content.</param>
        protected override void Write(DateTime dateTime, LogLevel level, string source, XT content)
        {
            // start row
            writer.Write("<tr>");

            // datetime
            writer.Write("<td>" + dateTime.ToString(StringExtensions.DisplayDateTimeWithTimeZoneFormat) + "</td>");

            // loglevel (background colored)
            {
                XTColor color = Level.GetLogLevelColor();
                if (color == XTColor.Default) { writer.Write("<td>"); }
                else { writer.Write("<td class=\"" + color + "\">"); }
                writer.Write(Level + "</td>");
            }

            // source
            writer.Write("<td>" + source + "</td>");

            // colored content
            {
                writer.Write("<td>");
                XTColor color = XTColor.Default;
                foreach (XTItem item in content.Items)
                {
                    if (item.Color != color)
                    {
                        if (color != XTColor.Default)
                        {
                            writer.Write("</span>");
                        }

                        color = item.Color;
                        if (color != XTColor.Default)
                        {
                            writer.Write("<span style=\"color:" + color + "\">");
                        }
                    }
                    if (item.Text.Contains("\n"))
                    {
                        writer.Write("<br/>");
                    }
                    else
                    {
                        writer.Write(item.Text);
                    }
                }
                if (color != XTColor.Default)
                {
                    writer.Write("</span>");
                }
                writer.Write("</td>");
            }

            // end row
            writer.WriteLine("</tr>");
            writer.Flush();
        }

        /// <summary>Initializes a new instance of the <see cref="LogHtmlFile"/> class.</summary>
        /// <param name="fileName">Name of the file.</param>
        public LogHtmlFile(string fileName)
            : base(fileName)
        {
            this.LogDebug("Prepare logging to file <cyan>{0}", fileName);
            writer = File.CreateText(fileName);
            writer.WriteLine("<html><head>");
            writer.WriteLine(res.LogHtmsortTable);
            writer.WriteLine(res.LogHtmstyle);
            writer.WriteLine("</head>");
            writer.WriteLine("<body style=\"font-family:monospace\"><table class=\"sortable\">");
            writer.WriteLine("<tr><th>DateTime</th><th>Level</th><th>Source</th><th>Content</th></tr>");
            writer.Flush();
        }

        /// <summary>Closes the <see cref="LogReceiver" />.</summary>
        public override void Close()
        {
            lock (this)
            {
                if (writer != null)
                {
                    writer.WriteLine("</table></body></html>");
                    writer.Close();
                    writer = null;
                }
            }
        }

        /// <summary>Obtains the name of the log.</summary>
        public override string LogSourceName => "LogHtmlFile <" + FileName + ">";
    }
}
