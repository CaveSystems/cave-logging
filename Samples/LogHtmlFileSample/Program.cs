using Cave.Logging;

namespace LogHtmlFileSample;

class Program
{
    #region Private Fields

    static int MessagesSent = 0;
    static int SendersReady = 0;
    static ManualResetEventSlim StartEvent = new(false);

    #endregion Private Fields

    #region Private Methods

    static void Main(string[] args)
    {
        Console.WriteLine("Prepare logging...");
        var file1 = LogHtmlFile.StartLogFile("testlog.info.html");
        var file2 = LogHtmlFile.StartLogFile("testlog.verbose.html");
        file2.Level = LogLevel.Verbose;

        //prepare 3 logger instances for the test
        var sender1 = new Logger("Sender1");
        var sender2 = new Logger("Sender2");
        var sender3 = new Logger("Sender3");

        //create 3 tasks waiting for start signal to send messages
        Console.WriteLine("Start threads...");
        var task1 = Task.Factory.StartNew(() => SendMessages(sender1));
        var task2 = Task.Factory.StartNew(() => SendMessages(sender2));
        var task3 = Task.Factory.StartNew(() => SendMessages(sender3));
        //wait until all threads are ready
        while (SendersReady < 3) Thread.Sleep(1);

        Console.WriteLine("Start threads...");
        //set start event for all threads
        StartEvent.Set();

        Console.WriteLine("Waiting for threads...");
        //wait until all threads are complete
        Task.WaitAll(task1, task2, task3);

        Console.WriteLine("Waiting for logger...");
        //wait for all loggers
        Logger.Flush();

        Console.WriteLine("Done.");
        //close logging system
        Logger.Close();
    }

    static void SendMessages(Logger logger)
    {
        var logLevelCount = Enum.GetValues<LogLevel>().Length;
        //set ready signal and log message
        Interlocked.Increment(ref SendersReady);
        logger.Notice($"SendMessages to Logger {logger.SenderName} waiting for start signal...");

        //wait for start signal then log message
        StartEvent.Wait();
        logger.Info($"<green>Begin SendMessages to Logger {logger.SenderName}");

        //send 10000 messages
        for (var i = 0; i < 10000; i++)
        {
            var level = (LogLevel)(i % logLevelCount);
            logger.Send(level, $"<{LogColor.Cyan}>Message<{LogColor.Default}> {i} is an even message: {i % 2 == 0} or an odd message: {i % 2 == 1}");
        }

        logger.Info($"<red>End SendMessages to Logger {logger.SenderName}");
        Interlocked.Add(ref MessagesSent, 3 + 10000);
    }

    #endregion Private Methods
}
