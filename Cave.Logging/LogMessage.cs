using Cave.Console;
using System;

namespace Cave.Logging
{
    /// <summary>
    /// Provides an immutable log message
    /// </summary>
    public class LogMessage
    {
        /// <summary>Initializes a new instance of the <see cref="LogMessage"/> class.</summary>
        public LogMessage() { }

        /// <summary>Initializes a new instance of the <see cref="LogMessage" /> class.</summary>
        /// <param name="source">The source.</param>
        /// <param name="dateTime">The date time.</param>
        /// <param name="level">The level.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="content">The content.</param>
        /// <param name="arguments">The arguments.</param>
        public LogMessage(string source, DateTime dateTime, LogLevel level, Exception exception, XT content, object[] arguments)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            DateTime = dateTime;
            Level = level;
            Source = source;
            m_Content = content;
            Exception = exception;
            m_Arguments = arguments;
        }

        /// <summary>The date time</summary>
        public DateTime DateTime { get; }

        /// <summary>The level</summary>
        public LogLevel Level { get; }

        /// <summary>The source</summary>
        public string Source { get; }

        /// <summary>The content</summary>
        XT m_Content;

        /// <summary>The arguments</summary>
        object[] m_Arguments;

        /// <summary>The exception</summary>
        public Exception Exception { get; }

        XT m_CompleteContent = null;

        /// <summary>Gets the content including arguments.</summary>
        /// <value>The content.</value>
        public XT Content
        {
            get
            {
                lock (this)
                {
                    if (m_CompleteContent == null)
                    {

                        if (m_Arguments == null || m_Arguments.Length == 0)
                        {
                            m_CompleteContent = m_Content;
                        }
                        else
                        {
                            m_CompleteContent = XT.Format(m_Content.Data, m_Arguments);
                        }
                    }
                    return m_CompleteContent;
                }
            }
        }
    }
}
