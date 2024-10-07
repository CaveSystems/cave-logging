using System;
using System.Collections.Generic;
using Cave.IO;

namespace Cave.Logging;

sealed class LogFileWriter(DataWriter writer) : LogWriter, IDisposable
{
    #region Private Fields

    DataWriter? writer = writer;

    #endregion Private Fields

    #region Public Methods

    public override void Close()
    {
        base.Close();
        writer?.Close();
        writer = null;
    }

    public void Dispose() => Close();

    public override void Flush() => writer?.Flush();

    public override void Write(LogMessage message, IEnumerable<ILogText> items)
    {
        foreach (var item in items)
        {
            if (item.Equals(LogText.NewLine))
            {
                writer?.WriteLine(item.Text);
            }
            else
            {
                writer?.Write(item.Text);
            }
        }
    }

    #endregion Public Methods
}
