using System;
using System.Diagnostics;
using System.Threading;
using Cave.Data;

namespace Cave.Logging.Database;

/// <summary>
/// Provides a log receiver for logging to database
/// </summary>
public class LogTableReceiver : LogReceiver
{
    RowLayout? layout;

    /// <summary>Gets or sets the host name.</summary>
    public string HostName { get; set; }

    /// <summary>Gets the writer.</summary>
    /// <value>The writer.</value>
    public new TableWriter<LogEntry>? Writer { get; private set; }

    /// <summary>Connects to the specified database and table</summary>
    /// <param name="database">The database to use</param>
    /// <param name="tableName">The name of the table to use (optional)</param>
    /// <param name="tableFlags">The table flags (optional).</param>
    /// <exception cref="ArgumentNullException">Database</exception>
    public void Connect(IDatabase database, string? tableName = null, TableFlags tableFlags = TableFlags.None)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        Connect(database.GetTable<LogEntry>(tableName, tableFlags));
    }

    /// <summary>
    /// Connects to the specified table instance
    /// </summary>
    /// <param name="table">Table to use</param>
    public void Connect(ITable<LogEntry> table)
    {
        if (table == null) throw new ArgumentNullException(nameof(table));
        layout = table.Layout;
        var logEntry = new LogEntry()
        {
            DateTime = DateTime.Now,
            Level = LogLevel.Information,
            HostName = HostName,
            ProcessName = ProcessName,
            Source = "LogTableReceiver",
            Content = "Started logging to table"
        };
        Writer = new TableWriter<LogEntry>(table);
        Writer.Insert([logEntry]);
        Writer.Flush();
        if (Writer.Error != null) throw Writer.Error;
    }

    /// <summary>
    /// Creates a new LogTableReceiver instance
    /// </summary>
    public LogTableReceiver()
    {
        if (ProcessName == null)
        {
            try { ProcessName = AssemblyVersionInfo.Program.Product; }
            catch (Exception ex) { Log.Error(ex, $"Error loading process name into logger instance"); }
        }
        if (ProcessName == null)
        {
            try { ProcessName = Process.GetCurrentProcess().ProcessName; }
            catch (Exception ex) { Log.Error(ex, $"Error loading process name into logger instance"); }
        }
        ProcessName = "Unknown" + Environment.TickCount;
        HostName = Environment.MachineName;
    }

    /// <summary>
    /// Writes the specified log message to the database
    /// </summary>
    /// <param name="message">The log message to write.</param>
    public override void Write(LogMessage message)
    {
        if (Closed || (layout == null)) return;
        var logEntry = new LogEntry()
        {
            HostName = HostName,
            ProcessName = ProcessName,
            Source = message.SenderSource ?? string.Empty,
            DateTime = message.DateTime,
            Level = message.Level,
            Content = message.Content?.ToString() ?? string.Empty,
        };
        var writer = Writer;
        if (writer != null)
        {
            writer.Insert([logEntry]);
            var errorMode = false;
            while (writer == Writer && writer.QueueCount > 10000)
            {
                try { writer.Flush(); }
                catch (Exception ex)
                {
                    if (!errorMode)
                    {
                        errorMode = true;
                        Log.Error(ex, "Error writing to log table!");
                    }
                    Thread.Sleep(1000);
                }
            }
        }
    }

    /// <summary>
    /// The process name we log for
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>Closes the <see cref="LogReceiver" /> and disposes the tablewriter</summary>
    /// <exception cref="ObjectDisposedException">LogTableReceiver</exception>
    public override void Close()
    {
        Writer?.Close();
        Writer = null;
        base.Close();
    }
}
