namespace PandocGUIWrapper.Models
{
    /// <summary>
    /// Describes one pandoc format: the name shown to the user and the default file extension.
    /// </summary>
    internal sealed class FormatInfo
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FormatInfo"/> class.
        /// </summary>
        /// <param name="friendlyName">The name shown to the user, for example "Word".</param>
        /// <param name="extension">The default file extension, including the dot.</param>
        public FormatInfo(string friendlyName, string extension)
        {
            FriendlyName = friendlyName;
            Extension = extension;
        }

        #endregion

        #region Public Properties

        /// <summary>Gets the name shown to the user.</summary>
        public string FriendlyName { get; }

        /// <summary>Gets the default file extension, including the dot.</summary>
        public string Extension { get; }

        #endregion
    }
}
