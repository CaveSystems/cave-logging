using Cave.Console;

namespace Cave.Logging
{
    /// <summary>
    /// Provides an interface for log output
    /// </summary>
    public interface ILogTarget
    {
        /// <summary>
        /// Invert the color (use color as background highlighter)
        /// </summary>
        bool Inverted { get; set; }

        /// <summary>
        /// Gets/sets the current text color
        /// </summary>
        XTColor TextColor { set; get; }

        /// <summary>
        /// Gets/sets the current text color
        /// </summary>
        XTStyle TextStyle { set; get; }

        /// <summary>
        /// Writes a string to the console (no formatting)
        /// </summary>
        /// <param name="text">The plain string to write</param>
        /// <returns>Returns the number of newlines printed</returns>
        int WriteString(string text);

        /// <summary>
        /// Writes a LogText to the console (with formatting)
        /// </summary>
        /// <param name="text">The <see cref="XT"/> instance to write</param>
        /// <returns>Returns the number of newlines printed</returns>
        int Write(XT text);

        /// <summary>
        /// Writes a LogText to the console (with formatting)
        /// </summary>
        /// <param name="item">The <see cref="XTItem"/> instance to write</param>
        /// <returns>Returns the number of newlines printed</returns>
        int Write(XTItem item);

        /// <summary>
        /// Starts a new line at the console
        /// </summary>
        void NewLine();

        /// <summary>
        /// Resets the color to default value
        /// </summary>
        void ResetColor();

        /// <summary>
        /// Gets/sets the console title
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Clears the console
        /// </summary>
        void Clear();
    }
}
