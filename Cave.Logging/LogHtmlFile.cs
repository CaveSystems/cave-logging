using System;
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
        /// Gets/sets the used file extension for the html logs.
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

        StreamWriter m_Writer;

        /// <summary>Writes the specified log message.</summary>
        /// <param name="dateTime">The date time.</param>
        /// <param name="level">The level.</param>
        /// <param name="source">The source.</param>
        /// <param name="content">The content.</param>
        protected override void Write(DateTime dateTime, LogLevel level, string source, XT content)
        {
            //start row
            m_Writer.Write("<tr>");
            //datetime
            m_Writer.Write("<td>" + dateTime.ToString(StringExtensions.DisplayDateTimeWithTimeZoneFormat) + "</td>");
            //loglevel (background colored)
            {
                XTColor color = Level.GetLogLevelColor();
                if (color == XTColor.Default) { m_Writer.Write("<td>"); }
                else { m_Writer.Write("<td class=\"" + color + "\">"); }
                m_Writer.Write(Level + "</td>");
            }
            //source
            m_Writer.Write("<td>" + source + "</td>");
            //colored content
            {
                m_Writer.Write("<td>");
                XTColor color = XTColor.Default;
                foreach (XTItem item in content.Items)
                {
                    if (item.Color != color)
                    {
                        if (color != XTColor.Default)
                        {
                            m_Writer.Write("</span>");
                        }

                        color = item.Color;
                        if (color != XTColor.Default)
                        {
                            m_Writer.Write("<span style=\"color:" + color + "\">");
                        }
                    }
                    if (item.Text.Contains("\n"))
                    {
                        m_Writer.Write("<br/>");
                    }
                    else
                    {
                        m_Writer.Write(item.Text);
                    }
                }
                if (color != XTColor.Default)
                {
                    m_Writer.Write("</span>");
                }
                m_Writer.Write("</td>");
            }
            //end row
            m_Writer.WriteLine("</tr>");
            m_Writer.Flush();
        }

        /// <summary>Initializes a new instance of the <see cref="LogHtmlFile"/> class.</summary>
        /// <param name="fileName">Name of the file.</param>
        public LogHtmlFile(string fileName) : base(fileName)
        {
            this.LogDebug("Prepare logging to file <cyan>{0}", fileName);
            m_Writer = File.CreateText(fileName);
            m_Writer.WriteLine("<html><head>");
            m_Writer.WriteLine(res.LogHtmsortTable);
            m_Writer.WriteLine(res.LogHtmstyle);
            m_Writer.WriteLine("</head>");
            m_Writer.WriteLine("<body style=\"font-family:monospace\"><table class=\"sortable\">");
            m_Writer.WriteLine("<tr><th>DateTime</th><th>Level</th><th>Source</th><th>Content</th></tr>");
            m_Writer.Flush();
        }

        /// <summary>Closes the <see cref="LogReceiver" />.</summary>
        public override void Close()
        {
            lock (this)
            {
                if (m_Writer != null)
                {
                    m_Writer.WriteLine("</table></body></html>");
                    m_Writer.Close();
                    m_Writer = null;
                }
            }
        }

        /// <summary>Obtains the name of the log.</summary>
        public override string LogSourceName => "LogHtmlFile <" + FileName + ">";
    }
}
