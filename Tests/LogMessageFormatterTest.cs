using Cave;
using System;
using System.Globalization;
using Cave.Logging;
using NUnit.Framework;

namespace Tests;

[TestFixture]
class LogMessageFormatterTest
{
    [Test]
    public void Default()
    {
        var intl = new LogMessageFormatter() { FormatProvider = CultureInfo.InvariantCulture };
        var de = new LogMessageFormatter() { FormatProvider = new CultureInfo("de-DE") };
        var dt = new DateTime(2023, 2, 23, 23, 2, 23, 200, DateTimeKind.Local);
        var msg = new LogMessage(dt, "LogMessageFormatterTest", GetType(), LogLevel.Critical, $"Critical test message number {2.5d}: This is even = {true}");
        var result1 = intl.FormatMessage(msg);
        Assert.AreEqual($"2023-02-23 23:02:23.200: Critical LogMessageFormatterTest> 'Critical test message number 2.5: This is even = True'{Environment.NewLine}", result1.GetPlainText());
        Assert.AreEqual("2023-02-23 23:02:23.200: Critical LogMessageFormatterTest> 'Critical test message number 2.5: This is even = True'<Reset>\n", result1.Join());
        var result2 = de.FormatMessage(msg);
        Assert.AreEqual($"2023-02-23 23:02:23.200: Critical LogMessageFormatterTest> 'Critical test message number 2,5: This is even = True'{Environment.NewLine}", result2.GetPlainText());
        Assert.AreEqual("2023-02-23 23:02:23.200: Critical LogMessageFormatterTest> 'Critical test message number 2,5: This is even = True'<Reset>\n", result2.Join());
    }

    [Test]
    public void DefaultColored()
    {
        var intl = new LogMessageFormatter
        {
            FormatProvider = CultureInfo.InvariantCulture,
            MessageFormat = LogMessageFormatter.DefaultColored
        };
        var de = new LogMessageFormatter
        {
            FormatProvider = new CultureInfo("de-DE"),
            MessageFormat = LogMessageFormatter.DefaultColored
        };
        var dt = new DateTime(2023, 2, 23, 23, 2, 23, 200, DateTimeKind.Local);
        var msg = new LogMessage(dt, "LogMessageFormatterTest", GetType(), LogLevel.Critical, $"Critical test message number {2.5d}: This is even = {true}");
        var result1 = intl.FormatMessage(msg);
        Assert.AreEqual($"2023-02-23 23:02:23.200: Critical LogMessageFormatterTest> 'Critical test message number 2.5: This is even = True'{Environment.NewLine}", result1.GetPlainText());
        Assert.AreEqual("<Red>2023-02-23 23:02:23.200: Critical LogMessageFormatterTest> 'Critical test message number 2.5: This is even = True'<Reset>\n", result1.Join());
        var result2 = de.FormatMessage(msg);
        Assert.AreEqual($"2023-02-23 23:02:23.200: Critical LogMessageFormatterTest> 'Critical test message number 2,5: This is even = True'{Environment.NewLine}", result2.GetPlainText());
        Assert.AreEqual("<Red>2023-02-23 23:02:23.200: Critical LogMessageFormatterTest> 'Critical test message number 2,5: This is even = True'<Reset>\n", result2.Join());
    }
}
