using Cave.Console;

namespace Cave.Logging
{
    class LogSystemConsole : ILogTarget
    {
        public bool Inverted { get => SystemConsole.Inverted; set => SystemConsole.Inverted = value; }
        public XTColor TextColor { get => SystemConsole.TextColor; set => SystemConsole.TextColor = value; }
        public XTStyle TextStyle { get => SystemConsole.TextStyle; set => SystemConsole.TextStyle = value; }
        public string Title { get => SystemConsole.Title; set => SystemConsole.Title = value; }
        public void Clear()
        {
            SystemConsole.Clear();
        }

        public void NewLine()
        {
            SystemConsole.NewLine();
        }

        public void ResetColor()
        {
            SystemConsole.ResetColor();
        }

        public int Write(XT text)
        {
            return SystemConsole.Write(text);
        }

        public int Write(XTItem item)
        {
            return SystemConsole.Write(item);
        }

        public int WriteString(string text)
        {
            return SystemConsole.WriteString(text);
        }
    }
}
