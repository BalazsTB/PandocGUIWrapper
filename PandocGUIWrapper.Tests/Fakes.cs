using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PandocGUIWrapper.Models;
using PandocGUIWrapper.Services;

namespace PandocGUIWrapper.Tests
{
    /// <summary>
    /// A fake <see cref="IPandocService"/> that records the last conversion and returns a set result.
    /// </summary>
    internal sealed class FakePandocService : IPandocService
    {
        #region Public Properties

        /// <summary>Gets or sets the exception thrown by the format methods, or <c>null</c>.</summary>
        public Exception FormatsException { get; set; }

        /// <summary>Gets or sets the output formats returned by <see cref="GetOutputFormatsAsync"/>.</summary>
        public string[] OutputFormats { get; set; } = { "markdown", "docx", "html", "odt" };

        /// <summary>Gets or sets the result returned by <see cref="ConvertAsync"/>.</summary>
        public ConversionResult Result { get; set; } = new ConversionResult(true, 0, string.Empty);

        /// <summary>Gets the number of calls to <see cref="ConvertAsync"/>.</summary>
        public int ConvertCalls { get; private set; }

        /// <summary>Gets the input path of the last conversion.</summary>
        public string LastInput { get; private set; }

        /// <summary>Gets the output path of the last conversion.</summary>
        public string LastOutput { get; private set; }

        /// <summary>Gets the input format of the last conversion.</summary>
        public string LastFrom { get; private set; }

        /// <summary>Gets the output format of the last conversion.</summary>
        public string LastTo { get; private set; }

        /// <summary>Gets the reference document of the last conversion.</summary>
        public string LastReferenceDocument { get; private set; }

        #endregion

        #region Interface Implementations

        /// <inheritdoc />
        public Task<IReadOnlyList<string>> GetInputFormatsAsync() => Formats(new[] { "markdown", "docx" });

        /// <inheritdoc />
        public Task<IReadOnlyList<string>> GetOutputFormatsAsync() => Formats(OutputFormats);

        /// <inheritdoc />
        public Task<ConversionResult> ConvertAsync(string inputPath, string outputPath, string fromFormat, string toFormat, string referenceDocumentPath)
        {
            ConvertCalls++;
            LastInput = inputPath;
            LastOutput = outputPath;
            LastFrom = fromFormat;
            LastTo = toFormat;
            LastReferenceDocument = referenceDocumentPath;
            return Task.FromResult(Result);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns the formats, or throws <see cref="FormatsException"/> when it is set.
        /// </summary>
        /// <param name="formats">The formats to return.</param>
        /// <returns>The formats.</returns>
        private Task<IReadOnlyList<string>> Formats(string[] formats)
        {
            if (FormatsException != null)
            {
                throw FormatsException;
            }

            return Task.FromResult<IReadOnlyList<string>>(formats);
        }

        #endregion
    }

    /// <summary>
    /// A fake <see cref="IDialogService"/> that returns set answers and records what was shown.
    /// </summary>
    internal sealed class FakeDialogService : IDialogService
    {
        #region Public Properties

        /// <summary>Gets or sets the path returned by the next Open-file dialog.</summary>
        public string NextOpenFile { get; set; }

        /// <summary>Gets or sets the path returned by the next folder dialog.</summary>
        public string NextFolder { get; set; }

        /// <summary>Gets or sets the answer to every confirmation.</summary>
        public bool ConfirmAnswer { get; set; }

        /// <summary>Gets the filter of the last Open-file dialog.</summary>
        public string LastOpenFilter { get; private set; }

        /// <summary>Gets the file passed to <see cref="ShowInExplorer"/>.</summary>
        public string ShownInExplorer { get; private set; }

        /// <summary>Gets the messages shown.</summary>
        public List<(string Message, DialogKind Kind)> Messages { get; } = new List<(string Message, DialogKind Kind)>();

        /// <summary>Gets the confirmation questions asked.</summary>
        public List<string> Confirms { get; } = new List<string>();

        #endregion

        #region Interface Implementations

        /// <inheritdoc />
        public string ShowOpenFileDialog(string title, string filter, string initialDirectory)
        {
            LastOpenFilter = filter;
            return NextOpenFile;
        }

        /// <inheritdoc />
        public string ShowFolderDialog(string description, string initialPath) => NextFolder;

        /// <inheritdoc />
        public void ShowMessage(string message, string title, DialogKind kind) => Messages.Add((message, kind));

        /// <inheritdoc />
        public bool Confirm(string message, string title, DialogKind kind)
        {
            Confirms.Add(message);
            return ConfirmAnswer;
        }

        /// <inheritdoc />
        public void ShowInExplorer(string filePath) => ShownInExplorer = filePath;

        #endregion
    }

    /// <summary>
    /// A fake <see cref="ISettingsService"/> that keeps the settings in memory.
    /// </summary>
    internal sealed class FakeSettingsService : ISettingsService
    {
        #region Public Properties

        /// <summary>Gets or sets the settings returned by <see cref="Load"/>.</summary>
        public AppSettings Loaded { get; set; } = new AppSettings();

        /// <summary>Gets the number of calls to <see cref="Save"/>.</summary>
        public int SaveCalls { get; private set; }

        /// <summary>Gets the settings passed to the last <see cref="Save"/>.</summary>
        public AppSettings Saved { get; private set; }

        /// <summary>Gets or sets the exception thrown by <see cref="Save"/>, or <c>null</c>.</summary>
        public Exception SaveException { get; set; }

        #endregion

        #region Interface Implementations

        /// <inheritdoc />
        public AppSettings Load() => Loaded;

        /// <inheritdoc />
        public void Save(AppSettings settings)
        {
            SaveCalls++;
            if (SaveException != null)
            {
                throw SaveException;
            }

            Saved = settings;
        }

        #endregion
    }
}
