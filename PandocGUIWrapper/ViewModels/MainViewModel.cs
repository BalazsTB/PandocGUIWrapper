using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PandocGUIWrapper.Services;

namespace PandocGUIWrapper.ViewModels
{
    /// <summary>
    /// The ViewModel for the main window. It holds one ViewModel for each tab and loads pandoc's formats.
    /// </summary>
    public sealed class MainViewModel : ObservableObject
    {
        #region Constants

        /// <summary>The title of all message boxes.</summary>
        private const string DialogTitle = "Pandoc GUI";

        #endregion

        #region Fields

        /// <summary>The service that runs pandoc, or <c>null</c> when pandoc was not found.</summary>
        private readonly IPandocService _pandoc;

        /// <summary>The service that shows dialogs.</summary>
        private readonly IDialogService _dialogs;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        /// <param name="convert">The ViewModel for the Convert tab.</param>
        /// <param name="referenceDocuments">The ViewModel for the Reference documents tab.</param>
        /// <param name="pandoc">The service that runs pandoc, or <c>null</c> when pandoc was not found.</param>
        /// <param name="dialogs">The service that shows dialogs.</param>
        public MainViewModel(ConvertViewModel convert, ReferenceDocumentsViewModel referenceDocuments, IPandocService pandoc, IDialogService dialogs)
        {
            Convert = convert ?? throw new ArgumentNullException(nameof(convert));
            ReferenceDocuments = referenceDocuments ?? throw new ArgumentNullException(nameof(referenceDocuments));
            _pandoc = pandoc;
            _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        }

        #endregion

        #region Public Properties

        /// <summary>Gets the ViewModel for the Convert tab.</summary>
        public ConvertViewModel Convert { get; }

        /// <summary>Gets the ViewModel for the Reference documents tab.</summary>
        public ReferenceDocumentsViewModel ReferenceDocuments { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Reads pandoc's format lists and loads both tabs. Shows an error when pandoc is missing or fails.
        /// </summary>
        /// <returns>A task that completes when loading is done.</returns>
        public async Task InitializeAsync()
        {
            if (_pandoc == null)
            {
                Convert.SetStatus("pandoc not found on PATH");
                _dialogs.ShowMessage(
                    "pandoc was not found on PATH.\nInstall it from https://pandoc.org/installing.html and start the app again.",
                    DialogTitle,
                    DialogKind.Error);
                return;
            }

            IReadOnlyList<string> inputFormats;
            IReadOnlyList<string> outputFormats;
            try
            {
                inputFormats = await _pandoc.GetInputFormatsAsync();
                outputFormats = await _pandoc.GetOutputFormatsAsync();
            }
            catch (Exception ex)
            {
                Convert.SetStatus("Could not read the pandoc formats");
                _dialogs.ShowMessage("Could not read the pandoc formats:\n" + ex.Message, DialogTitle, DialogKind.Error);
                return;
            }

            Convert.Load(inputFormats, outputFormats);
            ReferenceDocuments.Load(outputFormats);
        }

        #endregion
    }
}
