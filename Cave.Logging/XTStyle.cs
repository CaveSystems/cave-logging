namespace Cave
{
    /// <summary>
    /// Provides available logging colors.
    /// </summary>
    public enum XTStyle : byte
    {
        /// <summary>
        /// Default style (=will reset color to system default)
        /// </summary>
        Default = 0,

        /// <summary>
        /// Bold font
        /// </summary>
        Bold = 1,

        /// <summary>
        /// Italic font
        /// </summary>
        Italic = 2,

        /// <summary>
        /// Underline
        /// </summary>
        Underline = 4,

        /// <summary>
        /// Strikeout
        /// </summary>
        Strikeout = 8
    }
}
