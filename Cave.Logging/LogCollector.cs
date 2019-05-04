using System.Collections.Generic;

namespace Cave.Logging
{
    /// <summary>
    /// Automatically collects the latest and keeps a specified number of <see cref="LogMessage"/> items from the logging system.
    /// </summary>
    public class LogCollector : LogReceiverBase
    {
        Queue<LogMessage> items = new Queue<LogMessage>();
        volatile int maximumItemCount = 100;

        void CleanItems()
        {
            while (items.Count > maximumItemCount)
            {
                items.Dequeue();
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

        /// <summary>
        /// Retrieves all present <see cref="LogMessage"/> items and clears the collector.
        /// </summary>
        /// <returns></returns>
        public LogMessage[] Retrieve()
        {
            lock (items)
            {
                LogMessage[] result = items.ToArray();
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
        /// Returns LogCollector[ItemCount,Level].
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "LogCollector[" + ItemCount + "," + Level + "]";
        }

        #region ILogReceiver Member

        /// <summary>Provides the callback function used to transmit the logging notifications.</summary>
        /// <param name="msg">The message.</param>
        public override void Write(LogMessage msg)
        {
            lock (items)
            {
                items.Enqueue(msg);
                CleanItems();
            }
        }

        /// <summary>
        /// Gets the string "LogCollector".
        /// </summary>
        public override string LogSourceName => "LogCollector";

        #endregion
    }
}
