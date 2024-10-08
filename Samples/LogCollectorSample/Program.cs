using Cave.Logging;

namespace LogCollectorSample;

class Program
{
    #region Private Fields

    static readonly ManualResetEventSlim StartEvent = new(false);
    static int messagesSent;
    static int sendersReady;

    #endregion Private Fields

    #region Private Methods

    static void Collector_MessageReceived(object? sender, LogMessageEventArgs e)
    {
        //you can filter messages you want to collect by checking e.Message
        //and define whether the collector should store the message
        //e.Handled = false or if the collector already did
        //its job with e.Handled = true

        //in this sample we filter by sender
        //all messages from FilteredSender shall be filtered
        e.Handled = e.Message.SenderName == "FilteredSender";
    }

    static void Main()
    {
        var collector1 = LogCollector.StartNew();
        //define custom message received filter
        collector1.MessageReceived += Collector_MessageReceived;
        //set loglevel of collector or warnings and above
        collector1.Level = LogLevel.Warning;
        //this collector shall use continous logging
        collector1.Mode = LogReceiverMode.Continuous;
        //keep all messages
        collector1.MaximumItemCount = int.MaxValue;
        //count collector start message
        messagesSent++;

        //no filter at second collector
        var collector2 = LogCollector.StartNew();
        collector2.Level = LogLevel.Verbose;
        collector2.MaximumItemCount = int.MaxValue;
        //count collector start message
        messagesSent++;

        //prepare 3 logger instances for the test
        var sender1 = new Logger("Sender1");
        var sender2 = new Logger("Sender2");
        var senderF = new Logger("SenderFiltered");
        //create 3 tasks waiting for start signal to send messages
        var task1 = Task.Factory.StartNew(() => SendMessages(sender1));
        var task2 = Task.Factory.StartNew(() => SendMessages(sender2));
        var task3 = Task.Factory.StartNew(() => SendMessages(senderF));
        //wait until all threads are ready
        while (sendersReady < 3) Thread.Sleep(1);
        //set start event for all threads
        StartEvent.Set();
        //wait until all threads are complete
        Task.WaitAll(task1, task2, task3);

        //wait for all loggers
        Logger.Flush();

        //look at collector
        if (collector2.ItemCount != messagesSent) throw new Exception("ItemCount does not match sent count!");
        var expected = collector2.ToArray().Where(i => i.Level <= LogLevel.Warning && i.SenderName != "FilteredSender").ToList();
        if (!expected.SequenceEqual(collector1.ToArray())) throw new Exception("Collected items do not match!");

        //close logging system
        Logger.Close();
    }

    static void SendMessages(Logger logger)
    {
        var logLevelCount = Enum.GetValues<LogLevel>().Length;
        //set ready signal and log message
        Interlocked.Increment(ref sendersReady);
        logger.Notice($"SendMessages to Logger {logger.SenderName} waiting for start signal...");

        //wait for start signal then log message
        StartEvent.Wait();
        logger.Info($"<green>Begin SendMessages to Logger {logger.SenderName}");

        //send 10000 messages
        for (var i = 0; i < 10000; i++)
        {
            var level = (LogLevel)(i % logLevelCount);
            logger.Send(level, $"<cyan>Message {i}<reset> is an even message: {i % 2 == 0} or an odd message: {i % 2 == 1}");
        }

        logger.Info($"<red>End SendMessages to Logger {logger.SenderName}");
        Interlocked.Add(ref messagesSent, 3 + 10000);
    }

    #endregion Private Methods
}
