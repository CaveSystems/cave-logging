using System;
using System.Collections.Generic;
using System.Linq;

namespace Cave.Logging;

/// <summary>Automatically collects the latest and keeps a specified number of <see cref="LogMessage"/> items from the logging system.</summary>
public class LogCollector : LogReceiver
{
    #region Private Fields

    readonly Queue<LogMessage> items = new();

    volatile int maximumItemCount = 100;

    #endregion Private Fields

    #region Private Methods

    LinkedList<LogMessage>? CleanMaxItems()
    {
        LinkedList<LogMessage>? list = null;
        if (maximumItemCount > 0)
        {
            while (items.Count > maximumItemCount)
            {
                list ??= new();
                list.AddLast(items.Dequeue());
            }
        }
        return list;
    }

    #endregion Private Methods

    #region Protected Methods

    /// <summary>
    /// Calls the <see cref="MessageReceived"/> event and adds the message to the internal queue if <see cref="LogMessageEventArgs.Handled"/> is not set at the event.
    /// </summary>
    /// <param name="message">The log message</param>
    /// <param name="handled">Indicates whether the message was handled or not.</param>
    protected virtual void OnMessageReceived(LogMessage message, out bool handled)
    {
        var func = MessageReceived;
        if (func is not null)
        {
            var e = new LogMessageEventArgs(message);
            func?.Invoke(this, e);
            handled = e.Handled;
        }
        else
        {
            handled = false;
        }
    }

    /// <summary>Calls the <see cref="MessagesRemoved"/> event.</summary>
    /// <param name="messages">The messages</param>
    protected virtual void OnMessagesRemoved(IEnumerable<LogMessage> messages) => MessagesRemoved?.Invoke(this, new(messages));

    #endregion Protected Methods

    #region Public Events

    /// <summary>Event to be called on each incoming message before the message is added to the queue.</summary>
    public event EventHandler<LogMessageEventArgs>? MessageReceived;

    /// <summary>Event to be called on each message removed from the queue by <see cref="MaximumItemCount"/> filter.</summary>
    public event EventHandler<LogMessagesEventArgs>? MessagesRemoved;

    #endregion Public Events

    #region Public Properties

    /// <summary>Gets the count of items collected and not retrieved.</summary>
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

    /// <summary>Gets or sets the maximum item count of <see cref="LogMessage"/> items collected. Default = 100.</summary>
    /// <remarks>You can use a zero or negative value to collect unlimited items.</remarks>
    public int MaximumItemCount
    {
        get => maximumItemCount;
        set
        {
            IEnumerable<LogMessage>? cleaned = null;
            lock (items)
            {
                maximumItemCount = value;
                cleaned = CleanMaxItems();
            }
            if (cleaned is not null)
            {
                OnMessagesRemoved(cleaned);
            }
        }
    }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Starts a new instance of the <see cref="LogCollector"/> class.</summary>
    public static LogCollector StartNew() => Start(new LogCollector());

    /// <summary>Clears the list of <see cref="LogMessage"/> items.</summary>
    public void Clear()
    {
        lock (items)
        {
            items.Clear();
        }
    }

    /// <summary>Retrieves all present <see cref="LogMessage"/> items and clears the collector.</summary>
    /// <returns></returns>
    public LogMessage[] Retrieve()
    {
        LogMessage[] result;
        lock (items)
        {
            result = [.. items];
            items.Clear();
        }
        return result;
    }

    /// <summary>Provides a list of <see cref="LogMessage"/> items.</summary>
    public LogMessage[] ToArray()
    {
        lock (items)
        {
            return [.. items];
        }
    }

    /// <summary>Returns LogCollector[ItemCount,Level].</summary>
    /// <returns></returns>
    public override string ToString() => "LogCollector[" + ItemCount + "," + Level + "]";

    /// <summary>Retrieves a <see cref="LogMessage"/> items from the collector.</summary>
    /// <returns></returns>
    public bool TryGet(out LogMessage? msg)
    {
        lock (items)
        {
            if (items.Count == 0)
            {
                msg = null;
                return false;
            }

            msg = items.Dequeue();
        }
        return true;
    }

    /// <summary>Provides the callback function used to transmit the logging notifications.</summary>
    /// <param name="message">The message.</param>
    public override void Write(LogMessage message)
    {
        OnMessageReceived(message, out var handled);
        if (!handled)
        {
            IEnumerable<LogMessage>? cleaned = null;
            lock (items)
            {
                items.Enqueue(message);
                cleaned = CleanMaxItems();
            }
            if (cleaned is not null)
            {
                OnMessagesRemoved(cleaned);
            }
        }
    }

    #endregion Public Methods
}
