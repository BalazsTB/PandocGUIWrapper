namespace PandocGUIWrapper.Models
{
    /// <summary>
    /// One entry in the output-format list: the pandoc format name and the text shown for it.
    /// </summary>
    public sealed class FormatOption
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FormatOption"/> class.
        /// </summary>
        /// <param name="format">The pandoc format name, for example <c>docx</c>.</param>
        /// <param name="displayName">The text shown in the user interface.</param>
        public FormatOption(string format, string displayName)
        {
            Format = format;
            DisplayName = displayName;
        }

        #endregion

        #region Public Properties

        /// <summary>Gets the pandoc format name.</summary>
        public string Format { get; }

        /// <summary>Gets the text shown in the user interface.</summary>
        public string DisplayName { get; }

        #endregion

        #region Overrides

        /// <inheritdoc />
        public override string ToString() => DisplayName;

        #endregion
    }
}
