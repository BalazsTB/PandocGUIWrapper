using System.Collections.Generic;
using System.Threading.Tasks;

namespace PandocGUIWrapper.Services
{
    /// <summary>
    /// Runs pandoc.
    /// </summary>
    public interface IPandocService
    {
        #region Public Methods

        /// <summary>
        /// Gets the input formats that pandoc can read.
        /// </summary>
        /// <returns>The format names, one per pandoc output line.</returns>
        /// <exception cref="System.InvalidOperationException">pandoc returned an error.</exception>
        Task<IReadOnlyList<string>> GetInputFormatsAsync();

        /// <summary>
        /// Gets the output formats that pandoc can write.
        /// </summary>
        /// <returns>The format names, one per pandoc output line.</returns>
        /// <exception cref="System.InvalidOperationException">pandoc returned an error.</exception>
        Task<IReadOnlyList<string>> GetOutputFormatsAsync();

        /// <summary>
        /// Converts one file. This method does not throw; failures are returned in the result.
        /// </summary>
        /// <param name="inputPath">The file to convert.</param>
        /// <param name="outputPath">The file to write.</param>
        /// <param name="fromFormat">The input format, or <c>null</c> to let pandoc detect it.</param>
        /// <param name="toFormat">The output format.</param>
        /// <param name="referenceDocumentPath">The reference document for <c>--reference-doc</c>, or <c>null</c> for none.</param>
        /// <returns>The result of the conversion.</returns>
        Task<ConversionResult> ConvertAsync(string inputPath, string outputPath, string fromFormat, string toFormat, string referenceDocumentPath);

        #endregion
    }
}
