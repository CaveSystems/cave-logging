using Cave.Data;
using System;

namespace Cave.Logging.Database;

/// <summary>
/// Provides a specialized table writer for log tables
/// </summary>
public sealed class LogTableWriter : IDisposable
{
    TableWriter<LogEntry>? writer;

    /// <summary>Creates a new log table writer instance</summary>
    /// <param name="database">The log database.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="tableFlags">The table flags.</param>
    public LogTableWriter(IDatabase database, string? tableName = null, TableFlags tableFlags = TableFlags.None)
        : this(database.GetTable<LogEntry>(tableName, tableFlags))
    {
    }

    /// <summary>
    /// Creates a new log table writer instance
    /// </summary>
    /// <param name="logTable">Table to write to</param>
    public LogTableWriter(ITable<LogEntry> logTable) => writer = new TableWriter<LogEntry>(logTable);

    /// <summary>
    /// Writes a log entry to the writer
    /// </summary>
    /// <param name="logEntry"></param>
    public void Write(LogEntry logEntry) => writer?.Insert([logEntry]);

    /// <summary>
    /// Disposes the writer
    /// </summary>
    public void Dispose()
    {
        if (writer != null)
        {
            writer.Close();
            writer = null;
        }
    }

    /// <summary>
    /// Obtains the number of items queued for writing
    /// </summary>
    public int QueueCount => writer?.QueueCount ?? -1;

    /// <summary>
    /// Obtains the number of items written
    /// </summary>
    public long WrittenCount => writer?.WrittenCount ?? -1;

    /// <summary>The table this instance writes to</summary>
    public ITable<LogEntry> Table => writer?.Table ?? throw new ObjectDisposedException(nameof(LogTableWriter));

    /// <summary>
    /// Returns a string representation of the writer
    /// </summary>
    /// <returns></returns>
    public override string ToString() => writer?.ToString() ?? "Writer disposed.";
}
