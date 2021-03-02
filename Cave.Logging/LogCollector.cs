using System;
using System.Collections.Generic;

namespace Cave.Logging
{
    /// <summary>
    /// Automatically collects the latest and keeps a specified number of <see cref="LogMessage"/> items from the logging system.
    /// </summary>
    public class LogCollector : LogReceiver
    {
        #region Fields

        readonly Queue<LogMessage> items = new();
        volatile int maximumItemCount = 100;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Gets the count of items collected and not retrieved.
        /// </summary>
        public int ItemCount
        {
            get
            {
                lock (items)
                {
                    return items.Count;
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum item count of <see cref="LogMessage"/> items collected.
        /// </summary>
        public int MaximumItemCount
        {
            get => maximumItemCount;
            set
            {
                lock (items)
                {
                    maximumItemCount = value;
                    CleanItems();
                }
            }
        }

        #endregion Properties

        #region Overrides

        /// <summary>
        /// Returns LogCollector[ItemCount,Level].
        /// </summary>
        /// <returns></returns>
        public override string ToString() => "LogCollector[" + ItemCount + "," + Level + "]";

        #endregion Overrides

        #region Members

        void CleanItems()
        {
            while (items.Count > maximumItemCount)
            {
                items.Dequeue();
            }
        }

        /// <summary>
        /// Clears the list of <see cref="LogMessage"/> items.
        /// </summary>
        public void Clear()
        {
            lock (items)
            {
                items.Clear();
            }
        }

        /// <summary>
        /// Retrieves all present <see cref="LogMessage"/> items and clears the collector.
        /// </summary>
        /// <returns></returns>
        public LogMessage[] Retrieve()
        {
            lock (items)
            {
                var result = items.ToArray();
                items.Clear();
                return result;
            }
        }

        /// <summary>
        /// Provides a list of <see cref="LogMessage"/> items.
        /// </summary>
        public LogMessage[] ToArray()
        {
            lock (items)
            {
                return items.ToArray();
            }
        }

        /// <summary>
        /// Retrieves a <see cref="LogMessage"/> items from the collector.
        /// </summary>
        /// <returns></returns>
        public bool TryGet(out LogMessage msg)
        {
            lock (items)
            {
                if (items.Count == 0)
                {
                    msg = new LogMessage();
                    return false;
                }

                msg = items.Dequeue();
                return true;
            }
        }

        #endregion Members

        #region ILogReceiver Member

        /// <inheritdoc/>
        protected override void Write(DateTime dateTime, LogLevel level, string source, XT content) => Write(new LogMessage(source, dateTime, level, null, content, null));

        /// <summary>
        /// Provides the callback function used to transmit the logging notifications.
        /// </summary>
        /// <param name="msg">The message.</param>
        public override void Write(LogMessage msg)
        {
            lock (items)
            {
                items.Enqueue(msg);
                CleanItems();
            }
        }

        #endregion ILogReceiver Member
    }
}
