using System;
using System.IO;

namespace Cave.Logging;

class LogFileWriter : LogWriterBase, IDisposable
{
    StreamWriter? writer;
    public LogFileWriter(Stream stream)
    {
        writer = new StreamWriter(stream);
    }
    public override void Close()
    {
        base.Close();
        writer?.Close();
        writer = null;
    }
    public override void ChangeColor(LogColor color) { }
    public override void ChangeStyle(LogStyle style) { }
    public override void NewLine() => writer?.WriteLine();
    public override void Reset() => writer?.Flush();
    public override void Write(string text) => writer?.WriteLine(text);
    public void Dispose() => Close();
}
