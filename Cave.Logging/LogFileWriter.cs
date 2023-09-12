using System;
using System.IO;

namespace Cave.Logging;

class LogFileWriter : LogWriter, IDisposable
{
    #region Private Fields

    StreamWriter? writer;

    #endregion Private Fields

    #region Public Constructors

    public LogFileWriter(Stream stream)
    {
        writer = new StreamWriter(stream);
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

    public override void Write(ILogText text)
    {
        if (text.Equals(LogText.NewLine))
        {
            writer?.WriteLine(text.Text);
        }
        else
        {
            writer?.Write(text.Text);
        }
    }

    #endregion Public Methods
}
