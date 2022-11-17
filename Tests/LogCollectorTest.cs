using Cave;
using Cave.Logging;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    class LogCollectorTest
    {
        #region Public Methods

        [Test]
        public void LogCollectorTest1()
        {
            var logger = new Logger("Test1");
            var col = new LogCollector();
            Assert.AreEqual(100, col.MaximumItemCount);
            Assert.AreEqual(LogLevel.Information, col.Level);
            for (var i = 0; i < 200; i++)
            {
                logger.LogVerbose($"Verbose Message <cyan>{i}");
                logger.LogInfo($"Message <cyan>{i}");
            }
            Logger.Flush();
            Assert.AreEqual(100, col.ItemCount);
            for (var i = 100; i < 200; i++)
            {
                Assert.IsTrue(col.TryGet(out var msg));
                Assert.AreEqual(LogLevel.Information, msg.Level);
                Assert.AreEqual($"Message {i}", msg.Content.Text);
                Assert.AreEqual(new XT($"Message <cyan>{i}"), msg.Content);
            }
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
            Assert.AreEqual(LogLevel.Information, col.Level);
            for (var i = 0; i < 200; i++)
            {
                logger.LogVerbose($"Verbose Message <cyan>{i}");
                logger.LogInfo($"Message <cyan>{i}");
            }
            Logger.Flush();
            Assert.AreEqual(200, col.ItemCount);
            for (var i = 0; i < 200; i++)
            {
                Assert.IsTrue(col.TryGet(out var msg));
                Assert.AreEqual(LogLevel.Information, msg.Level);
                Assert.AreEqual($"Message {i}", msg.Content.Text);
                Assert.AreEqual(new XT($"Message <cyan>{i}"), msg.Content);
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
            Assert.AreEqual(LogLevel.Information, col.Level);
            for (var i = 0; i < 200; i++)
            {
                logger.LogVerbose($"Verbose Message <cyan>{i}");
                logger.LogInfo($"Message <cyan>{i}");
            }
            Logger.Flush();
            Assert.AreEqual(200, col.ItemCount);
            for (var i = 0; i < 200; i++)
            {
                Assert.IsTrue(col.TryGet(out var msg));
                Assert.AreEqual(LogLevel.Information, msg.Level);
                Assert.AreEqual($"Message {i}", msg.Content.Text);
                Assert.AreEqual(new XT($"Message <cyan>{i}"), msg.Content);
            }
            Assert.IsFalse(col.TryGet(out _));
            Logger.Close();
            Assert.IsTrue(col.Closed);
        }

        #endregion Public Methods
    }
}
