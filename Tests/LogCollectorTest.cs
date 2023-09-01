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
class LogCollectorTest
{
    #region Public Methods

    [Test]
    public void LogCollectorTest1()
    {
        var logger = new Logger("Test1");
        var col = new LogCollector();
        int removed = 0;
        col.MessagesRemoved += (s, e) => removed++;
        Assert.AreEqual(100, col.MaximumItemCount);
        Assert.AreEqual(LogLevel.Information, col.Level);
        for (var i = 0; i < 200; i++)
        {
            logger.Verbose($"Verbose Message <cyan>{i}");
            logger.Info($"Message <cyan>{i}");
        }
        Logger.Flush();
        Assert.AreEqual(100, col.ItemCount);
        for (var i = 100; i < 200; i++)
        {
            Assert.IsTrue(col.TryGet(out var msg));
            Assert.AreEqual(LogLevel.Information, msg.Level);
            Assert.AreEqual($"Message <cyan>{i}", msg.Content.ToString());
            CollectionAssert.AreEqual(LogText.Parse($"Message <cyan>{i}"), LogText.Parse(msg.Content.ToString()));
        }
        Assert.AreEqual(100, removed);
        Assert.IsFalse(col.TryGet(out _));
        Logger.Close();
        Assert.IsTrue(col.Closed);
    }

    [Test]
    public void LogCollectorTest2()
    {
        var logger = new Logger("Test2");
        var col = new LogCollector
        {
            MaximumItemCount = 200
        };
        col.MessagesRemoved += (s, e) => Assert.Fail();
        Assert.AreEqual(LogLevel.Information, col.Level);
        for (var i = 0; i < 200; i++)
        {
            logger.Verbose($"Verbose Message <cyan>{i}");
            logger.Info($"Message <cyan>{i}");
        }
        Logger.Flush();
        Assert.AreEqual(200, col.ItemCount);
        for (var i = 0; i < 200; i++)
        {
            Assert.IsTrue(col.TryGet(out var msg));
            Assert.AreEqual(LogLevel.Information, msg.Level);
            Assert.AreEqual("LogCollectorTest.cs", Path.GetFileName(msg.SourceFile));
            Assert.AreEqual(GetType(), msg.SenderType);
            Assert.AreEqual(logger.SenderName, msg.SenderName);
            var logText = LogText.Parse(msg.Content.ToString());
            Assert.AreEqual($"Message {i}", logText.GetPlainText());
            CollectionAssert.AreEqual(LogText.Parse($"Message <cyan>{i}"), LogText.Parse(msg.Content.ToString()));
        }
        Assert.IsFalse(col.TryGet(out _));
        Logger.Close();
        Assert.IsTrue(col.Closed);
    }

    [Test]
    public void LogCollectorTest3()
    {
        var logger = new Logger("Test3");
        var col = new LogCollector
        {
            MaximumItemCount = 300
        };
        col.MessagesRemoved += (s, e) => Assert.Fail();
        Assert.AreEqual(LogLevel.Information, col.Level);
        for (var i = 0; i < 200; i++)
        {
            logger.Verbose($"Verbose Message <cyan>{i}");
            logger.Debug($"Debug Message <cyan>{i}");
            logger.Info($"Message <cyan>{i}");
        }
        Logger.Flush();
        Assert.AreEqual(200, col.ItemCount);
        for (var i = 0; i < 200; i++)
        {
            Assert.IsTrue(col.TryGet(out var msg));
            Assert.AreEqual(LogLevel.Information, msg.Level);
            Assert.AreEqual($"Message <cyan>{i}", msg.Content.ToString());
        }
        Assert.IsFalse(col.TryGet(out _));
        Logger.Close();
        Assert.IsTrue(col.Closed);
    }

    #endregion Public Methods
}
