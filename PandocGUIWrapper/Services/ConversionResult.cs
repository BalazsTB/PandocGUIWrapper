namespace PandocGUIWrapper.Services
{
    /// <summary>
    /// The outcome of one pandoc conversion.
    /// </summary>
    public sealed class ConversionResult
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ConversionResult"/> class.
        /// </summary>
        /// <param name="isSuccess">Whether pandoc finished with exit code 0.</param>
        /// <param name="exitCode">The pandoc exit code, or -1 when pandoc could not start.</param>
        /// <param name="errorText">The text pandoc wrote to standard error.</param>
        public ConversionResult(bool isSuccess, int exitCode, string errorText)
        {
            Success = isSuccess;
            ExitCode = exitCode;
            ErrorText = errorText;
        }

        #endregion

        #region Public Properties

        /// <summary>Gets a value indicating whether the conversion succeeded.</summary>
        public bool Success { get; }

        /// <summary>Gets the pandoc exit code, or -1 when pandoc could not start.</summary>
        public int ExitCode { get; }

        /// <summary>Gets the text pandoc wrote to standard error.</summary>
        public string ErrorText { get; }

        #endregion
    }
}
