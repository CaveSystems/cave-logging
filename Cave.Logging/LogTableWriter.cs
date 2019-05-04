using System;
using Cave.Data;

namespace Cave.Logging
{
    /// <summary>
    /// Provides a specialized table writer for log tables.
    /// </summary>
    public sealed class LogTableWriter : IDisposable
    {
        TableWriter<LogEntry> m_Writer;

        /// <summary>Creates a new log table writer instance.</summary>
        /// <param name="logDatabase">The log database.</param>
        /// <param name="tableFlags">The table flags.</param>
        public LogTableWriter(IDatabase logDatabase, TableFlags tableFlags = TableFlags.None)
            : this(logDatabase.GetTable<LogEntry>(tableFlags))
        {
        }

        /// <summary>
        /// Creates a new log table writer instance.
        /// </summary>
        /// <param name="logTable">Table to write to.</param>
        public LogTableWriter(ITable<LogEntry> logTable)
        {
            m_Writer = new TableWriter<LogEntry>(logTable)
            {
                TransactionFlags = TransactionFlags.AllowRequeue
            };
        }

        /// <summary>
        /// Writes a log entry to the writer.
        /// </summary>
        /// <param name="logEntry"></param>
        public void Write(LogEntry logEntry)
        {
            m_Writer.Insert(logEntry);
        }

        /// <summary>
        /// Disposes the writer.
        /// </summary>
        public void Dispose()
        {
            if (m_Writer != null)
            {
                m_Writer.Close();
                m_Writer = null;
            }
        }

        /// <summary>
        /// Obtains the number of items queued for writing.
        /// </summary>
        public int QueueCount => m_Writer.QueueCount;

        /// <summary>
        /// Obtains the number of items written.
        /// </summary>
        public long WrittenCount => m_Writer.WrittenCount;

        /// <summary>The table this instance writes to.</summary>
        public ITable<LogEntry> Table => m_Writer.Table;

        /// <summary>The transaction log.</summary>
        public TransactionLog<LogEntry> TransactionLog => m_Writer.TransactionLog;

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return m_Writer.ToString();
        }
    }
}
