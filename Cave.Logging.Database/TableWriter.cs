
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cave.Data;

namespace Cave.Logging.Database;

/// <summary>
/// Provides an asynchronous table writer.
/// </summary>
public class TableWriter : IDisposable
{
    /// <summary>logs verbose messages</summary>
    public bool LogVerboseMessages { get; set; }

    bool flushing;
    bool disposed;
    Task? flushTask;
    bool exit;
    long written;
    long? lastFlush;
    TimeSpan maxSeenDelay = new TimeSpan(TimeSpan.TicksPerMinute);
    readonly List<Transaction> transactionList = new();

    /// <summary>The table this instance writes to</summary>
    public ITable Table { get; private set; }

    /// <summary>Gets or sets the transaction flags.</summary>
    /// <value>The transaction flags.</value>
    public TransactionFlags TransactionFlags { get; set; }

    /// <summary>
    /// Obtains the RowLayout of the table
    /// </summary>
    public RowLayout Layout => Table.Layout;

    /// <summary>
    /// Gets / sets the cache flush treshold. This is the number of datasets the CachedTable will store before triggering a threshold violation and causing it to be flushed to the database.
    /// Set this to -1 to disable the treshold
    /// </summary>
    public int CacheFlushTreshold { get; set; } = 1000;

    /// <summary>
    /// Gets / sets the minimum cache flush wait time. This is the time in milliseconds the Writer will wait before starting any flush to the database. 
    /// Set this to TimeSpan.Zero to disable the minimum wait time (the background thread will no longer sleep whenever anythis can be written)
    /// </summary>
    public int CacheFlushMinWaitTime { get; set; } = 1000;

    /// <summary>
    /// Gets / sets the maximum cache flush wait time. This is the time in milliseconds the Writer will wait before starting a forced flush to the database. 
    /// Set this to TimeSpan.Zero to disable the maximum wait time (the background thread will wait until a treshold violation occurs and the CacheFlushWaitTime is exceeded)
    /// </summary>
    public int CacheFlushMaxWaitTime { get; set; } = 60000;

    /// <summary>
    /// Obtains the (local) date time of the last flush
    /// </summary>
    public DateTime LastFlush => new DateTime(lastFlush??0);

    void UncatchedFlush()
    {
        if (disposed) throw new ObjectDisposedException(nameof(TableWriter));
        IList<Transaction> transactions;
        lock (transactionList)
        {
            transactions = transactionList.ToArray();
        }

        long written = Table.Commit(transactions, TransactionFlags);

        if (written > 0)
        {
            Interlocked.Add(ref this.written, written);

            var delay = TimeSpan.Zero;
            var t = transactions.First();
            if (t != null)
            {
                delay = DateTime.UtcNow - t.Created;
                if (delay > maxSeenDelay)
                {
                    maxSeenDelay = delay;
                    Trace.TraceWarning($"Delay <red>{maxSeenDelay}<default>!");
                }
            }
            if (LogVerboseMessages)
            {
                Trace.TraceWarning($"Flushed {written} datasets to <green>{Table.Name}<default> (current delay <magenta>{delay}<default>)");
            }
        }
    }


    /// <summary>
    /// Flushes the data to the database and catches any errors
    /// </summary>
    bool CatchedFlush()
    {
        try
        {
            UncatchedFlush();
            lastFlush = DateTime.UtcNow.Ticks;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(string.Format("Error while flushing cache to table {0}\n{1}", Layout, ex));
            return false;
        }
    }

    void CheckError()
    {
        var task = flushTask;
        if (task != null && task.IsFaulted) throw task.Exception;
    }

    int TransactionCount() { lock (transactionList) return transactionList.Count; }

    int TransactionWait() { lock (transactionList) if (transactionList.Count == 0) Monitor.Wait(transactionList, 1000); return transactionList.Count; }

    /// <summary>
    /// Checks whether the <see cref="CacheFlushTreshold"/> is exceeded or not. If a threshold violation is detected, the table is flushed to the database.
    /// </summary>
    void Worker()
    {

#if DEBUG
        Thread.CurrentThread.Name = "TableWriter " + Table;
#endif
        long nextMessage = 0;
        var nextMaxWaitTimeExceeded = DateTime.UtcNow.AddMilliseconds(CacheFlushMaxWaitTime);

        while (!exit)
        {
            //wait until at least one transaction is available
            while (!exit && TransactionWait() == 0)
            {
                nextMaxWaitTimeExceeded = DateTime.UtcNow.AddMilliseconds(CacheFlushMaxWaitTime);
                if (LogVerboseMessages)
                {
                    if (DateTime.UtcNow.Ticks > nextMessage)
                    {
                        Trace.TraceInformation(ToString());
                        nextMessage = DateTime.UtcNow.Ticks + TimeSpan.TicksPerMinute;
                    }
                }
            }

            var count = TransactionCount();
            if (!flushing)
            {    //obey min wait time
                if (CacheFlushMinWaitTime > 0) Thread.Sleep(CacheFlushMinWaitTime);
                //obey treshold
                if (CacheFlushTreshold > 0)
                {
                    if (count < CacheFlushTreshold && DateTime.UtcNow < nextMaxWaitTimeExceeded) continue;
                }
            }
            CatchedFlush();
        }
    }

    /// <summary>
    /// Commits all changes to the database
    /// </summary>
    public void Flush()
    {
        CheckError();
        flushing = true;
        if (exit && flushTask?.IsCompleted == false)
        {
            lock (transactionList) { Monitor.Pulse(transactionList); }
            flushTask.Wait();
        }
        while (!exit && TransactionCount() > 0)
        {
            lock (transactionList) { Monitor.Pulse(transactionList); }
            Thread.Sleep(10);
        }
        flushing = false;
        while (TransactionCount() > 0) UncatchedFlush();
    }

    /// <summary>Closes this instance after flushing all data.</summary>
    /// <exception cref="ObjectDisposedException">TableWriter</exception>
    public void Close()
    {
        lock (transactionList)
        {
            if (exit) return;
            exit = true;
            Flush();
            Dispose();
        }
    }

    /// <summary>
    /// Creates a writer for the specified table.
    /// </summary>
    /// <param name="table">The underlying table</param>
    public TableWriter(ITable table)
    {
        LogVerboseMessages = Debugger.IsAttached;
        Table = table;
        lastFlush = DateTime.Now.Ticks;
        flushTask = Task.Factory.StartNew(() =>
        {
            try { Worker(); }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fatal error in table writer. This is unrecoverable!\n{ex}");
                Close();
            }
        }, TaskCreationOptions.LongRunning);
    }

    /// <summary>
    /// Obtains the number of items queued for writing
    /// </summary>
    public int QueueCount => TransactionCount();

    /// <summary>
    /// Obtains the number of items written
    /// </summary>
    public long WrittenCount => Interlocked.Read(ref written);

    /// <summary>Gets the error.</summary>
    /// <value>The error.</value>
    public Exception? Error { get { var task = flushTask; return task == null ? null : task.Exception; } }

    /// <summary>
    /// Writes all transaction of the given TransactionLog to the table
    /// </summary>
    /// <param name="transactions">The transactions to commit</param>
    public void Commit(IEnumerable<Transaction> transactions)
    {
        if (disposed) throw new ObjectDisposedException(nameof(TableWriter));
        if (transactions == null) throw new ArgumentNullException(nameof(transactions));
        CheckError();
        lock (transactionList)
        {
            transactionList.AddRange(transactions);
            Monitor.Pulse(transactionList);
        }
    }

    /// <summary>
    /// Writes a transaction
    /// </summary>
    /// <param name="transaction">Transaction to write</param>
    public void Write(IEnumerable<Transaction> transaction)
    {
        if (disposed) throw new ObjectDisposedException(nameof(TableWriter));
        CheckError();
        lock (transactionList)
        {
            transactionList.AddRange(transaction);
            Monitor.Pulse(transactionList);
        }
    }

    /// <summary>Inserts rows at the table using a background transaction</summary>
    /// <param name="rows">The rows.</param>
    public void Insert(IEnumerable<Row> rows) => Write(rows.Select(Transaction.Insert));

    /// <summary>Replaces rows at the table using a background transaction</summary>
    /// <param name="rows">The rows.</param>
    public void Replace(IEnumerable<Row> rows) => Write(rows.Select(Transaction.Replace));

    /// <summary>Updates rows at the table using a background transaction</summary>
    /// <param name="rows">The rows.</param>
    public void Update(IEnumerable<Row> rows) => Write(rows.Select(Transaction.Updated));

    /// <summary>Deletes the specified rows.</summary>
    /// <param name="rows">The rows.</param>
    public void Delete(IEnumerable<Row> rows) => Write(rows.Select(Transaction.Delete));

    /// <summary>
    /// Name Queue:0 Written:0
    /// </summary>
    /// <returns></returns>
    public override string ToString() => string.Format("TableWriter {0} Queue:{1} Written:{2}", Table, QueueCount, WrittenCount);

    #region IDisposable Support        

    /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            exit = true;
            disposed = true;
            if (disposing)
            {
                if (flushTask != null)
                {
                    exit = true;
                    lock (transactionList)
                    {
                        Monitor.Pulse(transactionList);
                    }
                    var t = flushTask;
                    flushTask = null;
                    t.Wait();
                    t.Dispose();
                }
            }
        }
    }

    /// <summary>Finalizes an instance of the <see cref="TableWriter"/> class.</summary>
    ~TableWriter()
    {
        Dispose(false);
    }

    /// <summary>
    /// Führt anwendungsspezifische Aufgaben durch, die mit der Freigabe, der Zurückgabe oder dem Zurücksetzen von nicht verwalteten Ressourcen zusammenhängen.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion
}


/// <summary>
/// Provides an asynchronous table writer.
/// </summary>
public class TableWriter<T> : TableWriter where T : struct
{
    /// <summary>
    /// Creates a writer for the specified table.
    /// </summary>
    /// <param name="table">The underlying table</param>
    public TableWriter(ITable<T> table)
        : base(table)
    {
        Table = table;
    }

    /// <summary>Inserts rows at the table using a background transaction</summary>
    /// <param name="items">The items.</param>
    public void Insert(IEnumerable<T> items) => Write(items.Select(Table.Layout.GetRow).Select(Transaction.Insert));

    /// <summary>Replaces rows at the table using a background transaction</summary>
    /// <param name="items">The items.</param>
    public void Replace(IEnumerable<T> items) => Write(items.Select(Table.Layout.GetRow).Select(Transaction.Replace));

    /// <summary>Updates rows at the table using a background transaction</summary>
    /// <param name="items">The items.</param>
    public void Update(IEnumerable<T> items) => Write(items.Select(Table.Layout.GetRow).Select(Transaction.Updated));

    /// <summary>Deletes rows at the table using a background transaction</summary>
    /// <param name="items">The items.</param>
    public void Delete(IEnumerable<T> items) => Write(items.Select(Table.Layout.GetRow).Select(Transaction.Delete));

    /// <summary>The table this instance writes to</summary>
    public new ITable<T> Table { get; private set; }
}

