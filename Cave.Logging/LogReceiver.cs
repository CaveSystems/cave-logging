namespace Cave.Logging;

/// <summary>Provides a log receiver implementation with <see cref="ILogMessageFormatter"/> and <see cref="ILogWriter"/>.</summary>
public abstract class LogReceiver : LogReceiverBase
{
    #region Public Properties

    /// <summary>Provides formatting for log messages.</summary>
    public ILogMessageFormatter MessageFormatter { get; set; } = new LogMessageFormatter();

    /// <summary>Provides writing to the backend.</summary>
    public ILogWriter Writer { get; set; } = LogWriter.Empty;

    #endregion Public Properties

    #region Public Methods

    /// <summary>Function to write a message to the receiver. This is called automatically within the</summary>
    /// <param name="message"></param>
    public override void Write(LogMessage message)
    {
        //message filtered ?
        if (message.Level > Level) return;
        var items = MessageFormatter.FormatMessage(message);
        items.ForEach(Writer.Write);
    }

    #endregion Public Methods
}
