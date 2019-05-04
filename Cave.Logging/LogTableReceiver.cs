using System;
using System.Threading;
using Cave.Console;
using Cave.Data;

namespace Cave.Logging
{
    /// <summary>
    /// Provides a log receiver for logging to database.
    /// </summary>
    public class LogTableReceiver : LogReceiver
    {
        RowLayout m_Layout;

        /// <summary>Gets the writer.</summary>
        /// <value>The writer.</value>
        public TableWriter<LogEntry> Writer { get; private set; }

        /// <summary>
        /// Gets/sets whether style and color information is written to the database or not.
        /// </summary>
        public bool WriteContentStyle;

        /// <summary>Connects to the specified database and table.</summary>
        /// <param name="database">The database to use.</param>
        /// <param name="tableFlags">The table flags.</param>
        /// <exception cref="ArgumentNullException">Database.</exception>
        public void Connect(IDatabase database, TableFlags tableFlags = TableFlags.None)
        {
            if (database == null)
            {
                throw new ArgumentNullException("Database");
            }

            Connect(database.GetTable<LogEntry>(tableFlags));
        }

        /// <summary>
        /// Connects to the specified table instance.
        /// </summary>
        /// <param name="table">Table to use.</param>
        public void Connect(ITable<LogEntry> table)
        {
            if (table == null)
            {
                throw new ArgumentNullException("Table");
            }

            m_Layout = table.Layout;
            LogEntry logEntry = new LogEntry()
            {
                DateTime = DateTime.Now,
                Level = LogLevel.Information,
                HostName = Logger.HostName,
                ProcessName = Logger.ProcessName,
                Source = "LogTableReceiver",
                Content = "Started logging to table"
            };
            Writer = new TableWriter<LogEntry>(table);
            Writer.Insert(logEntry);
            Writer.Flush();
            if (Writer.Error != null)
            {
                throw Writer.Error;
            }
        }

        /// <summary>
        /// Creates a new LogTableReceiver instance.
        /// </summary>
        public LogTableReceiver()
        {
            ExceptionMode = LogExceptionMode.Full;
        }

        /// <summary>Writes the specified log message.</summary>
        /// <param name="dateTime">The date time.</param>
        /// <param name="level">The level.</param>
        /// <param name="source">The source.</param>
        /// <param name="content">The content.</param>
        protected override void Write(DateTime dateTime, LogLevel level, string source, XT content)
        {
            if (Closed || (m_Layout == null))
            {
                return;
            }

            LogEntry logEntry = new LogEntry()
            {
                HostName = Logger.HostName,
                ProcessName = Logger.ProcessName,
                Source = source,
                DateTime = dateTime,
                Level = level,
                Content = WriteContentStyle ? content : content.Text,
            };
            Row row = Row.Create(m_Layout, logEntry);
            TableWriter writer = Writer;
            if (writer != null)
            {
                Writer.Write(Transaction.InsertNew(row));
                bool errorMode = false;
                while (writer == Writer && writer.QueueCount > 10000)
                {
                    try { writer.Flush(); }
                    catch (Exception ex)
                    {
                        if (!errorMode)
                        {
                            errorMode = true;
                            this.LogError(ex, "Error writing to log table!");
                        }
                        Thread.Sleep(1000);
                    }
                }
            }
        }

        /// <summary>
        /// LogTableReceiver.
        /// </summary>
        public override string LogSourceName => "LogTableReceiver";

        /// <summary>Closes the <see cref="LogReceiver" /> and disposes the tablewriter.</summary>
        /// <exception cref="ObjectDisposedException">LogTableReceiver.</exception>
        public override void Close()
        {
            TableWriter writer = Writer;
            Writer = null;
            if (writer == null)
            {
                return;
            }

            writer?.Close();
            base.Close();
        }
    }
}
