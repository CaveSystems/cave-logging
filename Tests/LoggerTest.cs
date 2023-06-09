using System.Data.SqlTypes;
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

    [Test]
    public void LogerFlushTest()
    {
        Logger.LogToDebug = true;
        for (int i = 0; i < 100; i++)
        {
            var col = new LogCollector();
            col.Mode = LogReceiverMode.Continuous;
            col.LateMessageThreshold = -1;
            col.LateMessageMilliseconds = -1;
            col.MaximumItemCount = -1;
            Parallel.For(0, 1000, n => new Logger().Info($"Test {n}"));
            Logger.Flush();
            var count = col.ItemCount;
            var items = col.ToArray().Select(l => int.Parse(l.Content.ToString().Substring(5))).ToList();
            var missing = new Counter(0, 1000).Except(items).ToList();
            Assert.AreEqual(1000, count);
            Assert.AreEqual(1000, items.Count);
        }
    }
}
