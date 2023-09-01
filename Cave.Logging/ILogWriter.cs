namespace Cave.Logging;

/// <summary>
/// Provides an interface for writing log data. The writer keeps the current state for style and color.
/// </summary>
public interface ILogWriter
{
    /// <summary>
    /// Change the state to the specified color.
    /// </summary>
    /// <param name="color">New color.</param>
    void ChangeColor(LogColor color);

    /// <summary>
    /// Change the state to the specified style.
    /// </summary>
    /// <param name="style">New style</param>
    void ChangeStyle(LogStyle style);

    /// <summary>
    /// Print a newline.
    /// </summary>
    void NewLine();

    /// <summary>
    /// Reset the writer state (style, color, in line)
    /// </summary>
    void Reset();

    /// <summary>
    /// Writes a text
    /// </summary>
    /// <param name="text"></param>
    void Write(string text);

    /// <summary>
    /// Closes the writer.
    /// </summary>
    void Close();

    /// <summary>
    /// Gets a value indicating
    /// </summary>
    bool IsClosed { get; }
}
