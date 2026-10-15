using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PandocGUIWrapper.Models;
using PandocGUIWrapper.Services;

namespace PandocGUIWrapper.ViewModels
{
    /// <summary>
    /// The ViewModel for the Convert tab: input file, output name, format, folder, and reference document.
    /// </summary>
    public sealed class ConvertViewModel : ObservableObject
    {
        #region Constants

        /// <summary>The output format selected by default.</summary>
        private const string DefaultOutputFormat = "docx";

        /// <summary>The text shown when no reference document is used.</summary>
        private const string NoReferenceDocumentText = "None (pandoc default)";

        /// <summary>The title of all message boxes.</summary>
        private const string DialogTitle = "Pandoc GUI";

        #endregion

        #region Fields

        /// <summary>The service that runs pandoc.</summary>
        private readonly IPandocService _pandoc;

        /// <summary>The service that shows dialogs.</summary>
        private readonly IDialogService _dialogs;

        /// <summary>The store that holds the reference document for each format.</summary>
        private readonly IReferenceDocumentStore _referenceDocuments;

        /// <summary>Backing field for <see cref="InputPath"/>.</summary>
        private string _inputPath;

        /// <summary>Backing field for <see cref="OutputName"/>.</summary>
        private string _outputName = string.Empty;

        /// <summary>Backing field for <see cref="OutputFolder"/>.</summary>
        private string _outputFolder = string.Empty;

        /// <summary>Backing field for <see cref="SelectedOutputFormat"/>.</summary>
        private FormatOption _selectedOutputFormat;

        /// <summary>Backing field for <see cref="IsBusy"/>.</summary>
        private bool _isBusy;

        /// <summary>Backing field for <see cref="Status"/>.</summary>
        private string _status = "Starting…";

        /// <summary>Whether <see cref="Load"/> has run with pandoc's format lists.</summary>
        private bool _isReady;

        /// <summary>Whether the user typed an output name; if so, a new input file does not replace it.</summary>
        private bool _nameEdited;

        /// <summary>Whether the user chose an output folder; if so, a new input file does not replace it.</summary>
        private bool _folderEdited;

        /// <summary>The input formats pandoc can read.</summary>
        private IReadOnlyList<string> _inputFormats = new string[0];

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvertViewModel"/> class.
        /// </summary>
        /// <param name="pandoc">The service that runs pandoc. May be <c>null</c> when pandoc was not found.</param>
        /// <param name="dialogs">The service that shows dialogs.</param>
        /// <param name="referenceDocuments">The store that holds the reference documents.</param>
        public ConvertViewModel(IPandocService pandoc, IDialogService dialogs, IReferenceDocumentStore referenceDocuments)
        {
            _pandoc = pandoc;
            _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
            _referenceDocuments = referenceDocuments ?? throw new ArgumentNullException(nameof(referenceDocuments));
            _referenceDocuments.Changed += OnReferenceDocumentChanged;

            BrowseInputCommand = new RelayCommand(BrowseInput);
            BrowseOutputFolderCommand = new RelayCommand(BrowseOutputFolder);
            ConvertCommand = new AsyncRelayCommand(ConvertAsync, () => CanConvert);
        }

        #endregion

        #region Public Properties

        /// <summary>Gets the output formats shown in the format list.</summary>
        public ObservableCollection<FormatOption> OutputFormats { get; } = new ObservableCollection<FormatOption>();

        /// <summary>Gets or sets the full path of the file to convert.</summary>
        public string InputPath
        {
            get => _inputPath;
            set => SetProperty(ref _inputPath, value);
        }

        /// <summary>Gets or sets the output file name without the extension.</summary>
        public string OutputName
        {
            get => _outputName;
            set
            {
                if (SetProperty(ref _outputName, value ?? string.Empty))
                {
                    // A blank value lets the next input file fill in the default again.
                    _nameEdited = !string.IsNullOrWhiteSpace(value);
                }
            }
        }

        /// <summary>Gets or sets the folder where the output file is written.</summary>
        public string OutputFolder
        {
            get => _outputFolder;
            set
            {
                if (SetProperty(ref _outputFolder, value ?? string.Empty))
                {
                    // A blank value lets the next input file fill in the default again.
                    _folderEdited = !string.IsNullOrWhiteSpace(value);
                }
            }
        }

        /// <summary>Gets or sets the selected output format.</summary>
        public FormatOption SelectedOutputFormat
        {
            get => _selectedOutputFormat;
            set
            {
                if (SetProperty(ref _selectedOutputFormat, value))
                {
                    OnPropertyChanged(nameof(OutputExtension));
                    RaiseReferenceDocumentChanged();
                }
            }
        }

        /// <summary>Gets the extension of the output file, including the dot.</summary>
        public string OutputExtension =>
            _selectedOutputFormat == null ? string.Empty : FormatCatalog.GetExtension(_selectedOutputFormat.Format);

        /// <summary>Gets a value indicating whether the selected format uses a reference document.</summary>
        public bool IsReferenceDocumentSupported =>
            _selectedOutputFormat != null && FormatCatalog.SupportsReferenceDocument(_selectedOutputFormat.Format);

        /// <summary>Gets the configured reference document for the selected format, or <c>null</c>.</summary>
        public string ReferenceDocumentPath =>
            IsReferenceDocumentSupported ? _referenceDocuments.GetPath(_selectedOutputFormat.Format) : null;

        /// <summary>Gets the reference document file name, or a "none" text.</summary>
        public string ReferenceDocumentText
        {
            get
            {
                string path = ReferenceDocumentPath;
                return string.IsNullOrEmpty(path) ? NoReferenceDocumentText : Path.GetFileName(path);
            }
        }

        /// <summary>Gets a value indicating whether a conversion is running.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsIdle));
                }
            }
        }

        /// <summary>Gets a value indicating whether no conversion is running. Used to enable the inputs.</summary>
        public bool IsIdle => !_isBusy;

        /// <summary>Gets the text shown in the status bar.</summary>
        public string Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        /// <summary>Gets a value indicating whether all inputs are valid and a conversion can start.</summary>
        public bool CanConvert =>
            _isReady
            && !IsBusy
            && File.Exists(InputPath)
            && !string.IsNullOrWhiteSpace(OutputName)
            && SelectedOutputFormat != null
            && Directory.Exists(OutputFolder);

        /// <summary>Gets the command that opens the input-file dialog.</summary>
        public IRelayCommand BrowseInputCommand { get; }

        /// <summary>Gets the command that opens the output-folder dialog.</summary>
        public IRelayCommand BrowseOutputFolderCommand { get; }

        /// <summary>Gets the command that runs the conversion.</summary>
        public IAsyncRelayCommand ConvertCommand { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Fills the format list from pandoc's formats and makes the tab ready to use.
        /// </summary>
        /// <param name="inputFormats">The input formats pandoc can read.</param>
        /// <param name="outputFormats">The output formats pandoc can write.</param>
        public void Load(IReadOnlyList<string> inputFormats, IReadOnlyList<string> outputFormats)
        {
            _inputFormats = inputFormats ?? new string[0];

            OutputFormats.Clear();
            foreach (FormatOption option in FormatCatalog.CreateOptions(outputFormats ?? new string[0]))
            {
                OutputFormats.Add(option);
            }

            SelectedOutputFormat = OutputFormats.FirstOrDefault(o => o.Format == DefaultOutputFormat) ?? OutputFormats.FirstOrDefault();
            _isReady = true;
            Status = "Ready";
        }

        /// <summary>
        /// Shows a status text, for example when pandoc could not be started.
        /// </summary>
        /// <param name="text">The text to show.</param>
        public void SetStatus(string text)
        {
            Status = text;
        }

        /// <summary>
        /// Checks the inputs, asks the needed questions, and runs pandoc.
        /// </summary>
        /// <returns>A task that completes when the conversion is done.</returns>
        public async Task ConvertAsync()
        {
            if (!CanConvert)
            {
                return;
            }

            if (OutputName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                _dialogs.ShowMessage("The file name contains characters that are not allowed.", DialogTitle, DialogKind.Warning);
                return;
            }

            string outputPath = Path.Combine(OutputFolder, OutputName.Trim() + OutputExtension);
            if (string.Equals(Path.GetFullPath(outputPath), Path.GetFullPath(InputPath), StringComparison.OrdinalIgnoreCase))
            {
                _dialogs.ShowMessage("The output file cannot be the same as the input file.", DialogTitle, DialogKind.Warning);
                return;
            }

            if (File.Exists(outputPath)
                && !_dialogs.Confirm(Path.GetFileName(outputPath) + " already exists. Replace it?", DialogTitle, DialogKind.Warning))
            {
                return;
            }

            // A configured reference document that no longer exists is skipped only if the user agrees.
            string referenceDocument = ReferenceDocumentPath;
            if (!string.IsNullOrEmpty(referenceDocument) && !File.Exists(referenceDocument))
            {
                string question = "The reference document was not found:\n" + referenceDocument + "\n\nConvert without it?";
                if (!_dialogs.Confirm(question, DialogTitle, DialogKind.Warning))
                {
                    return;
                }

                referenceDocument = null;
            }

            string fromFormat = FormatCatalog.GuessInputFormat(InputPath);
            IsBusy = true;
            Status = "Converting…";
            ConversionResult result;
            try
            {
                result = await _pandoc.ConvertAsync(InputPath, outputPath, fromFormat, SelectedOutputFormat.Format, referenceDocument);
            }
            finally
            {
                IsBusy = false;
            }

            if (result.Success)
            {
                Status = "Converted: " + outputPath;
                if (_dialogs.Confirm("Converted to " + Path.GetFileName(outputPath) + ". Open folder?", DialogTitle, DialogKind.Information))
                {
                    _dialogs.ShowInExplorer(outputPath);
                }
            }
            else
            {
                Status = "Failed";
                string message = string.IsNullOrWhiteSpace(result.ErrorText)
                    ? "pandoc exited with code " + result.ExitCode + "."
                    : result.ErrorText;
                _dialogs.ShowMessage(message, DialogTitle, DialogKind.Error);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Asks for the input file and fills the default name and folder.
        /// </summary>
        private void BrowseInput()
        {
            string initialDirectory = File.Exists(InputPath) ? Path.GetDirectoryName(InputPath) : null;
            string path = _dialogs.ShowOpenFileDialog("Select a file to convert", FormatCatalog.BuildOpenFilter(_inputFormats), initialDirectory);
            if (path == null)
            {
                return;
            }

            InputPath = path;

            // Defaults are written through the backing fields so they do not count as user edits.
            if (!_nameEdited)
            {
                SetProperty(ref _outputName, Path.GetFileNameWithoutExtension(path), nameof(OutputName));
            }

            if (!_folderEdited)
            {
                SetProperty(ref _outputFolder, Path.GetDirectoryName(path), nameof(OutputFolder));
            }
        }

        /// <summary>
        /// Asks for the output folder.
        /// </summary>
        private void BrowseOutputFolder()
        {
            string folder = _dialogs.ShowFolderDialog("Select the folder for the new file", OutputFolder);
            if (folder != null)
            {
                OutputFolder = folder;
            }
        }

        /// <summary>
        /// Raises change notifications for all reference-document properties.
        /// </summary>
        private void RaiseReferenceDocumentChanged()
        {
            OnPropertyChanged(nameof(IsReferenceDocumentSupported));
            OnPropertyChanged(nameof(ReferenceDocumentPath));
            OnPropertyChanged(nameof(ReferenceDocumentText));
        }

        #endregion

        #region Overrides

        /// <inheritdoc />
        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // Any change can make the Convert button available or unavailable.
            if (e.PropertyName != nameof(CanConvert))
            {
                OnPropertyChanged(nameof(CanConvert));
                ConvertCommand?.NotifyCanExecuteChanged();
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Updates the shown reference document when it changes on the Reference documents tab.
        /// </summary>
        /// <param name="sender">The store.</param>
        /// <param name="e">The format that changed.</param>
        private void OnReferenceDocumentChanged(object sender, ReferenceDocumentChangedEventArgs e)
        {
            if (_selectedOutputFormat != null && string.Equals(e.Format, _selectedOutputFormat.Format, StringComparison.OrdinalIgnoreCase))
            {
                RaiseReferenceDocumentChanged();
            }
        }

        #endregion
    }
}
