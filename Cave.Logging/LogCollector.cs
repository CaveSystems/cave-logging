using System.Collections.Generic;

namespace Cave.Logging
{
    /// <summary>
    /// Automatically collects the latest and keeps a specified number of <see cref="LogMessage"/> items from the logging system.
    /// </summary>
    public class LogCollector : LogReceiverBase
    {
        Queue<LogMessage> m_Items = new Queue<LogMessage>();
        volatile int m_MaximumItemCount = 100;

        void m_Clean()
        {
            while (m_Items.Count > m_MaximumItemCount)
            {
                m_Items.Dequeue();
            }
        }

        /// <summary>
        /// Provides the maximum item count of <see cref="LogMessage"/> items collected.
        /// </summary>
        public int MaximumItemCount
        {
            get => m_MaximumItemCount;
            set { m_MaximumItemCount = value; m_Clean(); }
        }

        /// <summary>
        /// Clears the list of <see cref="LogMessage"/> items.
        /// </summary>
        public void Clear()
        {
            lock (m_Items)
            {
                m_Items.Clear();
            }
        }

        /// <summary>
        /// Retrieves a <see cref="LogMessage"/> items from the collector.
        /// </summary>
        /// <returns></returns>
        public bool TryGet(out LogMessage msg)
        {
            lock (m_Items)
            {
                if (m_Items.Count == 0)
                {
                    msg = new LogMessage();
                    return false;
                }
                msg = m_Items.Dequeue();
                return true;
            }
        }

        /// <summary>
        /// Retrieves all present <see cref="LogMessage"/> items and clears the collector.
        /// </summary>
        /// <returns></returns>
        public LogMessage[] Retrieve()
        {
            lock (m_Items)
            {
                LogMessage[] result = m_Items.ToArray();
                m_Items.Clear();
                return result;
            }
        }

        /// <summary>
        /// Provides a list of <see cref="LogMessage"/> items.
        /// </summary>
        public LogMessage[] ToArray()
        {
            lock (m_Items)
            {
                return m_Items.ToArray();
            }
        }

        /// <summary>
        /// Obtains the count of items collected and not retrieved.
        /// </summary>
        public int ItemCount
        {
            get
            {
                lock (m_Items)
                {
                    return m_Items.Count;
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
            lock (m_Items)
            {
                m_Items.Enqueue(msg);
                m_Clean();
            }
        }

        /// <summary>
        /// Obtains the string "LogCollector".
        /// </summary>
        public override string LogSourceName => "LogCollector";

        #endregion
    }
}
