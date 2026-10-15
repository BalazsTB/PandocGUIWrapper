using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PandocGUIWrapper.Models;
using PandocGUIWrapper.Services;

namespace PandocGUIWrapper.ViewModels
{
    /// <summary>
    /// One row on the Reference documents tab: the reference document for one output format.
    /// </summary>
    public sealed class ReferenceDocumentItemViewModel : ObservableObject
    {
        #region Constants

        /// <summary>The title of all message boxes.</summary>
        private const string DialogTitle = "Pandoc GUI";

        #endregion

        #region Fields

        /// <summary>The store that holds the reference documents.</summary>
        private readonly IReferenceDocumentStore _store;

        /// <summary>The service that shows dialogs.</summary>
        private readonly IDialogService _dialogs;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDocumentItemViewModel"/> class.
        /// </summary>
        /// <param name="format">The output format, for example <c>docx</c>.</param>
        /// <param name="store">The store that holds the reference documents.</param>
        /// <param name="dialogs">The service that shows dialogs.</param>
        public ReferenceDocumentItemViewModel(string format, IReferenceDocumentStore store, IDialogService dialogs)
        {
            Format = format ?? throw new ArgumentNullException(nameof(format));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
            _store.Changed += OnStoreChanged;

            BrowseCommand = new RelayCommand(Browse);
            ClearCommand = new RelayCommand(Clear, () => HasPath);
        }

        #endregion

        #region Public Properties

        /// <summary>Gets the pandoc output format.</summary>
        public string Format { get; }

        /// <summary>Gets the text shown for the format, for example "Word (docx)".</summary>
        public string DisplayName => FormatCatalog.GetDisplayName(Format);

        /// <summary>Gets the configured reference document path, or <c>null</c>.</summary>
        public string Path => _store.GetPath(Format);

        /// <summary>Gets a value indicating whether a reference document is configured.</summary>
        public bool HasPath => !string.IsNullOrEmpty(Path);

        /// <summary>Gets the command that selects a reference document.</summary>
        public IRelayCommand BrowseCommand { get; }

        /// <summary>Gets the command that removes the reference document.</summary>
        public IRelayCommand ClearCommand { get; }

        #endregion

        #region Private Methods

        /// <summary>
        /// Asks for a reference document and saves it.
        /// </summary>
        private void Browse()
        {
            string initialDirectory = HasPath ? System.IO.Path.GetDirectoryName(Path) : null;
            string path = _dialogs.ShowOpenFileDialog(
                "Select a reference document for " + DisplayName,
                FormatCatalog.BuildSingleFormatFilter(Format),
                initialDirectory);
            if (path == null)
            {
                return;
            }

            Save(() => _store.SetPath(Format, path));
        }

        /// <summary>
        /// Removes the reference document and saves the change.
        /// </summary>
        private void Clear()
        {
            Save(() => _store.ClearPath(Format));
        }

        /// <summary>
        /// Runs a store change and shows a warning when the settings cannot be saved.
        /// </summary>
        /// <param name="change">The change to run.</param>
        private void Save(Action change)
        {
            try
            {
                change();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                _dialogs.ShowMessage("The settings could not be saved:\n" + ex.Message, DialogTitle, DialogKind.Warning);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Refreshes the row when its format changes in the store.
        /// </summary>
        /// <param name="sender">The store.</param>
        /// <param name="e">The format that changed.</param>
        private void OnStoreChanged(object sender, ReferenceDocumentChangedEventArgs e)
        {
            if (!string.Equals(e.Format, Format, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            OnPropertyChanged(nameof(Path));
            OnPropertyChanged(nameof(HasPath));
            ClearCommand.NotifyCanExecuteChanged();
        }

        #endregion
    }
}
