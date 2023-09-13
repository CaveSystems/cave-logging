using System;
using System.Collections.Generic;
using System.IO;
using Cave.IO;
using static System.Net.Mime.MediaTypeNames;

namespace Cave.Logging;

class LogFileWriter : LogWriter, IDisposable
{
    #region Private Fields

    DataWriter? writer;

    #endregion Private Fields

    #region Public Constructors

    public LogFileWriter(DataWriter writer)
    {
        this.writer = writer;
    }

    #endregion Public Constructors

    #region Public Methods

    public override void Close()
    {
        base.Close();
        writer?.Close();
        writer = null;
    }

    public void Dispose() => Close();

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
