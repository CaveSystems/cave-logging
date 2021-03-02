using System;

namespace Cave.Logging
{
    /// <summary>
    /// Provides an immutable log message.
    /// </summary>
    public class LogMessage
    {
        #region Private Fields

        /// <summary>
        /// Gets the arguments.
        /// </summary>
        readonly object[] arguments;

        /// <summary>
        /// Gets the content.
        /// </summary>
        readonly XT content;

        XT completeContent;

        #endregion Private Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LogMessage"/> class.
        /// </summary>
        public LogMessage() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogMessage"/> class.
        /// </summary>
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
            this.content = content;
            Exception = exception;
            this.arguments = arguments;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets the content including arguments.
        /// </summary>
        /// <value>The content.</value>
        public XT Content
        {
            get
            {
                lock (this)
                {
                    if (completeContent == null)
                    {
                        if ((arguments == null) || (arguments.Length == 0))
                        {
                            completeContent = content;
                        }
                        else
                        {
                            completeContent = XT.Format(content.Data, arguments);
                        }
                    }

                    return completeContent;
                }
            }
        }

        /// <summary>
        /// Gets the date time.
        /// </summary>
        public DateTime DateTime { get; }

        /// <summary>
        /// Gets the exception.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the level.
        /// </summary>
        public LogLevel Level { get; }

        /// <summary>
        /// Gets the source.
        /// </summary>
        public string Source { get; }

        #endregion Properties
    }
}
