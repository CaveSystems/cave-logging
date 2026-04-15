using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cave;
using Cave.Collections;
using Cave.Logging;
using NUnit.Framework;

namespace Tests;

[TestFixture]
class LoggerTest
{
    #region Public Methods

    [Test]
    public void LoggerFlushTest()
    {
        Logger.LogToDebug = Logger.LogToTrace = false;
        for (int i = 0; i < 10; i++)
        {
            var col = new LogCollector();
            col.Mode = LogReceiverMode.Continuous;
            col.LateMessageThreshold = -1;
            col.LateMessageMilliseconds = -1;
            col.MaximumItemCount = -1;
            col.Start();
            var logger = new Logger();
            Parallel.For(0, 100000, n => logger.Info($"Test {i}.{n}"));
            Logger.Flush();
            var count = col.ItemCount;
            var items = col.ToArray().Select(l => int.Parse(l.Content.ToString().AfterFirst('.'))).ToList();
            var missing = new Counter(0, 10000).Except(items).ToList();
            Assert.AreEqual(100000, count);
            Assert.AreEqual(100000, items.Count);
        }
    }


    class CounterReceiver : LogReceiver
    {
        long count;

        public long Received => Interlocked.Read(ref count);

        public override void Write(LogMessage message) => Interlocked.Increment(ref count);
    }

    [Test]
    public void RunThreadsFor10Seconds()
    {
        for (ulong u = ulong.MaxValue; u > 0; u = (u >> 1))
        {
            try
            {
                Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)(-1);
                break;
            }
            catch { }
        }

        var counterReceiver1 = new CounterReceiver();
        var counterReceiver2 = new CounterReceiver() { Level = LogLevel.Verbose };

        int cpuCount = Environment.ProcessorCount;
        Thread[] threads = new Thread[cpuCount];

        ManualResetEvent allReady = new ManualResetEvent(false);
        ManualResetEvent startSignal = new ManualResetEvent(false);
        ManualResetEvent allFinished = new ManualResetEvent(false);

        int readyCount = 0;
        int finishedCount = 0;
        object lockObj = new object();

        DateTime end = default;

        // Performance Counter
        long globalMessageCounter = 0;

        for (int i = 0; i < cpuCount; i++)
        {
            int threadId = i;

            threads[i] = new Thread((object name) =>
            {
                Logger log = new Logger();

                // Thread meldet: bereit
                lock (lockObj)
                {
                    readyCount++;
                    if (readyCount == cpuCount)
                    {
                        end = DateTime.UtcNow.AddSeconds(10);
                        allReady.Set();
                    }
                }

                // Warten auf Startsignal
                startSignal.WaitOne();

                long localCount = 0;

                // 10 Sekunden Schleife
                while (DateTime.UtcNow < end)
                {
                    for (var lvl = 0; lvl <= (int)LogLevel.Verbose; lvl++)
                    {
                        log.Send((LogLevel)lvl, $"Thread {name} Message {localCount}!");
                        localCount++;
                    }
                }

                // Lokale Zählung in globalen Counter einfließen lassen
                Interlocked.Add(ref globalMessageCounter, localCount);

                // Thread meldet: fertig
                lock (lockObj)
                {
                    finishedCount++;
                    if (finishedCount == cpuCount)
                        allFinished.Set();
                }
            });

            threads[i].Priority = ThreadPriority.Highest;
            threads[i].Start(threadId);
        }

        // wait until all threads are ready
        allReady.WaitOne();

        //start receiver
        counterReceiver1.Start();
        counterReceiver2.Start();

        var watch = StopWatch.StartNew();

        // send start signal
        startSignal.Set();

        // wait until all threads are finished
        allFinished.WaitOne();

        var duration = watch.Elapsed;

        Logger.Flush();

        var endTime = watch.Elapsed;

        // print performance results
        double perSecond = globalMessageCounter / duration.TotalSeconds;

        Console.WriteLine("=====================================");
        Console.WriteLine(" Performance Counter");
        Console.WriteLine("=====================================");
        Console.WriteLine("CPUs:                  " + cpuCount);
        Console.WriteLine("Total Messages:        " + globalMessageCounter);
        Console.WriteLine("Messages / Second:     " + (long)perSecond);
        Console.WriteLine("=====================================");
        Console.WriteLine("Logger.WriteCount:     " + Logger.WriteCount);
        Console.WriteLine("Logger.ReadCount:      " + Logger.WriteCount);
        Console.WriteLine("=====================================");
        Console.WriteLine("InfoReceiver.Count:    " + counterReceiver1.Received);
        Console.WriteLine("VerboseReceiver.Count: " + counterReceiver2.Received);
        Console.WriteLine("=====================================");
        Console.WriteLine("Flush competed after: " + endTime.FormatTime());
        Assert.Greater(globalMessageCounter, 0, "No messages were generated.");
    }

    #endregion Public Methods
}
