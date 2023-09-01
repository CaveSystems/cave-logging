namespace Cave.Logging;

/// <summary>
/// Provides a log receiver implementation with <see cref="ILogMessageFormatter"/> and <see cref="ILogWriter"/>.
/// </summary>
public abstract class LogReceiver : LogReceiverBase
{
    /// <summary>
    /// Provides formatting for log messages.
    /// </summary>
    public ILogMessageFormatter MessageFormatter { get; set; } = new LogMessageFormatter();

    /// <summary>
    /// Provides writing to the backend.
    /// </summary>
    public ILogWriter Writer { get; set; } = LogWriterBase.Empty;

    /// <summary>
    /// Function to write a message to the receiver.
    /// This is called automatically within the 
    /// </summary>
    /// <param name="message"></param>
    public override void Write(LogMessage message)
    {
        //message filtered ?
        if (message.Level > Level) return;

        var items = MessageFormatter.FormatMessage(message);
        var color = LogColor.Default;
        var style = LogStyle.Unchanged;
        Writer.Reset();
        foreach (var item in items)
        {
            if (item.Style == LogStyle.Reset)
            {
                Writer.Reset();
                color = LogColor.Default;
                style = LogStyle.Unchanged;
            }
            else
            {
                style = item.Style;
                Writer.ChangeStyle(style);

                if (item.Color != LogColor.Default)
                {
                    color = item.Color;
                    Writer.ChangeColor(color);
                }
            }
            if (Equals(item, LogText.NewLine))
            {
                Writer.NewLine();
            }
            else
            {
                Writer.Write(item.Text);
            }
        }
        Writer.Reset();
    }
}
