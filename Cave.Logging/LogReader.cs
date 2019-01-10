using Cave.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Cave.Logging
{
    /// <summary>
    /// Provides a log source to be scanned for log entries
    /// </summary>
    public class LogReader : ILogSource
    {
        /// <summary>Gets the name of the log source.</summary>
        /// <value>The name of the log source.</value>
        public string LogSourceName => "LogReader";

        long m_LastID = 0;
        bool m_OldLayout = false;
        int m_OldLayoutDateTimeField;
        int m_OldLayoutLevelField;
        int m_OldLayoutSourceField;
        int m_OldLayoutContentField;
        int m_OldLayoutHostnameField;
        int m_OldLayoutProcessnameField;

        /// <summary>
        /// Connection string for the storage
        /// </summary>
        public ConnectionString ConnectionString { get; private set; }

        /// <summary>
        /// Obtains the <see cref="ITable"/> instance of the LogSource. 
        /// </summary>
        public ITable Table { get; private set; }

        /// <summary>
        /// Filters log entries and returns only entries containing the specified string
        /// </summary>
        public string FilterHostName { get; set; }

        /// <summary>
        /// Filters log entries and returns only entries containing the specified string
        /// </summary>
        public string FilterProcessName { get; set; }

        /// <summary>
        /// Filters log entries and returns only entries containing the specified string
        /// </summary>
        public string FilterSource { get; set; }

        /// <summary>Gets the layout.</summary>
        /// <value>The layout.</value>
        public RowLayout Layout { get; } = RowLayout.CreateTyped(typeof(LogEntry));

        /// <summary>Connects to the table and checks the layout against the current and old logging layout</summary>
        /// <param name="table">The table.</param>
        /// <param name="throwEx">Throw exception of errors</param>
        /// <exception cref="InvalidDataException"></exception>
        bool ConnectTable(ITable table, bool throwEx)
        {
            //check if it is using the current layout
            if (table.Layout.Equals(RowLayout.CreateTyped(typeof(LogEntry))))
            {
                return true;
            }

            m_OldLayout = true;
            //reset field indices
            m_OldLayoutDateTimeField = -1;
            m_OldLayoutLevelField = -1;
            m_OldLayoutSourceField = -1;
            m_OldLayoutContentField = -1;
            m_OldLayoutHostnameField = -1;
            m_OldLayoutProcessnameField = -1;
            //check id field name
            if (table.Layout.IDFieldIndex < 0)
            {
                string l_Error = "ID field does not match expected format!";
                this.LogVerbose(l_Error);
                if (throwEx)
                {
                    throw new InvalidDataException(l_Error);
                }

                return false;
            }
            //find hostname field
            for (int i = 0; i < table.Layout.FieldCount; i++)
            {
                FieldProperties field = table.Layout.GetProperties(i);
                switch (field.Name.ToUpperInvariant())
                {
                    case "PROCESS":
                    case "PROCESSNAME":
                        m_OldLayoutProcessnameField = i;
                        break;
                    case "HOST":
                    case "HOSTNAME":
                    case "MACHINE":
                    case "MACHINENAME":
                        m_OldLayoutHostnameField = i;
                        break;
                    case "DATETIME":
                    case "DATE":
                    case "CREATED":
                        m_OldLayoutDateTimeField = i;
                        break;
                    case "LEVEL":
                    case "LOGLEVEL":
                        m_OldLayoutLevelField = i;
                        break;
                    case "SOURCE":
                        m_OldLayoutSourceField = i;
                        break;
                    case "MESSAGE":
                    case "CONTENT":
                    case "TEXT":
                    case "MSG":
                        m_OldLayoutContentField = i;
                        break;
                }
            }
            if (m_OldLayoutDateTimeField < 0)
            {
                string error = string.Format("Field {0} cannot be found!", "DateTime");
                this.LogVerbose(error);
                if (throwEx)
                {
                    throw new InvalidDataException(error);
                }

                return false;
            }
            if (m_OldLayoutLevelField < 0)
            {
                string error = string.Format("Field {0} cannot be found!", "Level");
                this.LogVerbose(error);
                if (throwEx)
                {
                    throw new InvalidDataException(error);
                }

                return false;
            }
            if (m_OldLayoutContentField < 0)
            {
                string error = string.Format("Field {0} cannot be found!", "Content");
                this.LogVerbose(error);
                if (throwEx)
                {
                    throw new InvalidDataException(error);
                }

                return false;
            }
            return true;
        }

        /// <summary>Connects to the table and checks the layout against the current and old logging layout</summary>
        /// <param name="db">The database.</param>
        /// <param name="name">Name of the table</param>
        /// <param name="throwEx">Throw exception of errors</param>
        /// <returns>Returns the ITable instance or null on error.</returns>
        /// <exception cref="InvalidDataException"></exception>
        ITable ConnectTable(IDatabase db, string name, bool throwEx)
        {
            ITable table = db.GetTable(name);
            if (ConnectTable(table, throwEx))
            {
                return table;
            }

            return null;
        }

        LogEntry ConvertOldLayoutRow(Row row)
        {
            LogEntry logEntry = new LogEntry
            {
                //get id
                ID = Layout.GetID(row)
            };
            #region decode datetime
            //get row data
            object dateTimeObject = row.GetValue(m_OldLayoutDateTimeField);
            if (dateTimeObject is DateTime)
            {
                //native value
                logEntry.DateTime = (DateTime)dateTimeObject;
            }
            else if (dateTimeObject is long)
            {
                //log value, check if bigint datetime
                long value = (long)dateTimeObject;
                if (!DateTime.TryParseExact(value.ToString(), CaveSystemData.BigIntDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out logEntry.DateTime))
                {
                    //tick value
                    logEntry.DateTime = new DateTime(value);
                }
            }
            else
            {
                throw new InvalidDataException("Invalid or unknown datetime value format!");
            }
            #endregion
            logEntry.Level = (LogLevel)Convert.ToInt64(row.GetValue(m_OldLayoutLevelField));
            if (m_OldLayoutSourceField > -1)
            {
                logEntry.Source = row.GetValue(m_OldLayoutSourceField) as string;
            }

            logEntry.Content = row.GetValue(m_OldLayoutContentField) as string;
            if (m_OldLayoutHostnameField > -1)
            {
                logEntry.HostName = row.GetValue(m_OldLayoutHostnameField) as string;
            }

            if (m_OldLayoutProcessnameField > -1)
            {
                logEntry.ProcessName = row.GetValue(m_OldLayoutProcessnameField) as string;
            }

            return logEntry;
        }

        /// <summary>
        /// Loads datasets with an old logging layout
        /// </summary>
        /// <param name="search"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        LogEntry[] OldLayoutGet(Search search, ResultOption option)
        {
            List<Row> rows = Table.GetRows(search, option);
            if (rows.Count == 0)
            {
                return new LogEntry[0];
            }

            LogEntry[] results = new LogEntry[rows.Count];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = ConvertOldLayoutRow(rows[i]);
            }
            if (results.Length > 0)
            {
                m_LastID = Math.Max(m_LastID, results[results.Length - 1].ID);
            }
            return results;
        }

        /// <summary>
        /// Loads datasets from the source
        /// </summary>
        /// <param name="search">Search to use</param>
        /// <param name="option">Option to use</param>
        /// <returns></returns>
        LogEntry[] Get(Search search, ResultOption option)
        {
            List<Row> rows = Table.GetRows(search, option);
            LogEntry[] results = new LogEntry[rows.Count];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = rows[i].GetStruct<LogEntry>(Table.Layout);
            }
            if (results.Length > 0)
            {
                m_LastID = Math.Max(m_LastID, results[results.Length - 1].ID);
            }
            return results;
        }

        Search MakeFilters()
        {
            Search result = Search.None;
            if (FilterHostName != null)
            {
                if (m_OldLayout)
                {
                    if (m_OldLayoutHostnameField > -1)
                    {
                        string fieldName = Table.Layout.GetName(m_OldLayoutHostnameField);
                        result &= Search.FieldLike(fieldName, FilterHostName);
                    }
                }
                else
                {
                    result &= Search.FieldLike("HostName", FilterHostName);
                }
            }
            if (FilterProcessName != null)
            {
                if (m_OldLayout)
                {
                    if (m_OldLayoutProcessnameField > -1)
                    {
                        string fieldName = Table.Layout.GetName(m_OldLayoutProcessnameField);
                        result &= Search.FieldLike(fieldName, FilterProcessName);
                    }
                }
                else
                {
                    result &= Search.FieldLike("ProcessName", FilterProcessName);
                }
            }
            if (FilterSource != null)
            {
                if (m_OldLayout)
                {
                    if (m_OldLayoutSourceField > -1)
                    {
                        string fieldName = Table.Layout.GetName(m_OldLayoutSourceField);
                        result &= Search.FieldLike(fieldName, FilterSource);
                    }
                }
                else
                {
                    result &= Search.FieldLike("Source", FilterSource);
                }
            }
            return result;
        }

        /// <summary>Initializes a new instance of the <see cref="LogReader"/> class.</summary>
        /// <param name="table">The table.</param>
        /// <exception cref="InvalidDataException"></exception>
        public LogReader(ITable table)
        {
            if (!ConnectTable(table, true))
            {
                throw new InvalidDataException(string.Format("Table {0} is not a valid log table!", table));
            }

            Table = table;
        }

        /// <summary>
        /// Groups datasets of same source, host, process and level.
        /// This function does not apply filters.
        /// </summary>
        /// <returns>Returns the last dataset of each source, host, process and level combination.</returns>
        public LogEntry[] GetGrouped()
        {
            if (m_OldLayout)
            {
                ResultOption option = ResultOption.SortDescending(Table.Layout.IDField.Name);
                if (m_OldLayoutSourceField > -1)
                {
                    option += ResultOption.Group(Table.Layout.GetName(m_OldLayoutSourceField));
                }

                if (m_OldLayoutLevelField > -1)
                {
                    option += ResultOption.Group(Table.Layout.GetName(m_OldLayoutLevelField));
                }

                if (m_OldLayoutProcessnameField > -1)
                {
                    option += ResultOption.Group(Table.Layout.GetName(m_OldLayoutProcessnameField));
                }

                if (m_OldLayoutHostnameField > -1)
                {
                    option += ResultOption.Group(Table.Layout.GetName(m_OldLayoutHostnameField));
                }

                return OldLayoutGet(Search.None, option);
            }
            return Get(Search.None, ResultOption.Group("Level") + ResultOption.Group("ProcessName") + ResultOption.Group("HostName") + ResultOption.Group("Source"));
        }

        /// <summary>
        /// Obtains the log entry with the specified id
        /// </summary>
        /// <param name="id">id of the entry</param>
        /// <returns></returns>
        public LogEntry Get(long id)
        {
            Row row = Table.GetRow(id);
            if (m_OldLayout)
            {
                return ConvertOldLayoutRow(row);
            }
            else
            {
                return Get(Search.FieldEquals(nameof(LogEntry.ID), id), ResultOption.None)[0];
            }
        }

        /// <summary>
        /// Obtains a list of ids present at the log table
        /// </summary>
        /// <returns></returns>
        public List<long> GetIDs()
        {
            ResultOption option = ResultOption.SortAscending(Table.Layout.IDField.Name);
            return Table.FindRows(MakeFilters(), option);
        }

        /// <summary>
        /// Obtains a number of items previous to the specified id.
        /// </summary>
        /// <param name="id">ID of the lastest entry</param>
        /// <param name="backLogCount">Number of items to get</param>
        /// <returns></returns>
        public LogEntry[] GetHistory(long id, int backLogCount)
        {
            Search search = Search.FieldLike(Table.Layout.IDField.Name, id) & MakeFilters();
            ResultOption option = ResultOption.SortDescending(Table.Layout.IDField.Name) + ResultOption.Limit(backLogCount);
            LogEntry[] l_Entries;
            if (m_OldLayout)
            {
                l_Entries = OldLayoutGet(search, option);
            }
            else
            {
                l_Entries = Get(search, option);
            }
            Array.Reverse(l_Entries);
            return l_Entries;
        }

        /// <summary>
        /// Obtains items from the log table
        /// </summary>
        /// <param name="end">Last items date time</param>
        /// <param name="backLogCount">Number of items to get</param>
        /// <returns></returns>
        public LogEntry[] GetHistory(DateTime end, int backLogCount)
        {
            Search search = Search.FieldSmaller(nameof(LogEntry.DateTime), end) | Search.FieldEquals(nameof(LogEntry.DateTime), end);
            search &= MakeFilters();
            ResultOption option = ResultOption.SortDescending(Table.Layout.IDField.Name) + ResultOption.Limit(backLogCount);
            LogEntry[] l_Entries;
            if (m_OldLayout)
            {
                l_Entries = OldLayoutGet(search, option);
            }
            else
            {
                l_Entries = Get(search, option);
            }
            Array.Reverse(l_Entries);
            return l_Entries;
        }

        /// <summary>
        /// Obtains datasets in the specified range. This function obeys filters.
        /// </summary>
        /// <param name="start">Start date time</param>
        /// <param name="end">End date time</param>
        /// <returns></returns>
        public LogEntry[] GetHistory(DateTime start, DateTime end)
        {
            Search search = (Search.FieldGreater(nameof(LogEntry.DateTime), start) & Search.FieldSmaller(nameof(LogEntry.DateTime), end)) | Search.FieldEquals(nameof(LogEntry.DateTime), start) | Search.FieldEquals(nameof(LogEntry.DateTime), end);
            search &= MakeFilters();
            if (m_OldLayout)
            {
                return OldLayoutGet(search, ResultOption.SortAscending(Table.Layout.IDField.Name));
            }

            return Get(search, ResultOption.SortAscending(Table.Layout.IDField.Name));
        }

        /// <summary>
        /// Retrieves the next datasets from the table. This function obeys filters.
        /// </summary>
        /// <param name="level">Minimum level to retrieve</param>
        /// <returns></returns>
        public LogEntry[] GetNext(LogLevel level)
        {
            Search search = Search.FieldGreater(Table.Layout.IDField.Name, m_LastID) & Search.FieldSmaller("Level", level + 1);
            search &= MakeFilters();
            if (m_OldLayout)
            {
                return OldLayoutGet(search, ResultOption.SortAscending(Table.Layout.IDField.Name));
            }

            return Get(search, ResultOption.SortAscending(Table.Layout.IDField.Name));
        }

        /// <summary>
        /// Retrieves the next datasets from the source. This function obeys filters.
        /// </summary>
        /// <returns></returns>
        public LogEntry[] GetNext()
        {
            Search search = Search.FieldGreater(Table.Layout.IDField.Name, m_LastID);
            search &= MakeFilters();
            if (m_OldLayout)
            {
                return OldLayoutGet(search, ResultOption.SortAscending(Table.Layout.IDField.Name));
            }

            return Get(search, ResultOption.SortAscending(Table.Layout.IDField.Name));
        }

        /// <summary>
        /// Moves the reader to the end of the table
        /// </summary>
        public void MoveToEnd()
        {
            m_LastID = Table.FindRow(Search.None, ResultOption.SortDescending(Table.Layout.IDField.Name) + ResultOption.Limit(1));
        }

        /// <summary>
        /// Moves the reader to the start of the table
        /// </summary>
        public void MoveToStart()
        {
            m_LastID = -1;
        }

        /// <summary>
        /// Obtains the number of items present at the table (without any filters appied)
        /// </summary>
        public long RowCount => Table.RowCount;

        /// <summary>
        /// Obtains the number items present at the table after filtering
        /// </summary>
        public long FilteredRowCount => Table.Count(MakeFilters(), ResultOption.None);

        /// <summary>
        /// Gets the log entry at the specified index. This function does not apply filters.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public LogEntry GetAtIndex(int index)
        {
            if (m_OldLayout)
            {
                ConvertOldLayoutRow(Table.GetRowAt(index));
            }
            return Table.GetRowAt(index).GetStruct<LogEntry>(Layout);
        }

        /// <summary>
        /// Obtains the table name
        /// </summary>
        public string Name => Table.Name;

        /// <summary>
        /// returns the connection string of the source
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ConnectionString.ToString(ConnectionStringPart.Protocol | ConnectionStringPart.Server | ConnectionStringPart.Path);
        }
    }
}
