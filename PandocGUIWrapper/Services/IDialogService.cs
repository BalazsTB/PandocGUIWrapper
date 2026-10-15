namespace PandocGUIWrapper.Services
{
    /// <summary>
    /// The icon and tone of a message box.
    /// </summary>
    public enum DialogKind
    {
        /// <summary>An information message.</summary>
        Information,

        /// <summary>A warning.</summary>
        Warning,

        /// <summary>An error.</summary>
        Error,
    }

    /// <summary>
    /// Shows dialogs and message boxes. ViewModels use this interface so they do not depend on WPF.
    /// </summary>
    public interface IDialogService
    {
        #region Public Methods

        /// <summary>
        /// Shows an Open-file dialog.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="filter">The file-type filter, for example <c>"Word (*.docx)|*.docx"</c>.</param>
        /// <param name="initialDirectory">The folder to open first, or <c>null</c>.</param>
        /// <returns>The selected file path, or <c>null</c> when the user cancels.</returns>
        string ShowOpenFileDialog(string title, string filter, string initialDirectory);

        /// <summary>
        /// Shows a folder picker.
        /// </summary>
        /// <param name="description">The text shown in the dialog.</param>
        /// <param name="initialPath">The folder to select first, or <c>null</c>.</param>
        /// <returns>The selected folder path, or <c>null</c> when the user cancels.</returns>
        string ShowFolderDialog(string description, string initialPath);

        /// <summary>
        /// Shows a message with an OK button.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="title">The window title.</param>
        /// <param name="kind">The icon to show.</param>
        void ShowMessage(string message, string title, DialogKind kind);

        /// <summary>
        /// Asks a Yes/No question.
        /// </summary>
        /// <param name="message">The question.</param>
        /// <param name="title">The window title.</param>
        /// <param name="kind">The icon to show.</param>
        /// <returns><c>true</c> when the user selects Yes.</returns>
        bool Confirm(string message, string title, DialogKind kind);

        /// <summary>
        /// Opens File Explorer with a file selected.
        /// </summary>
        /// <param name="filePath">The file to select.</param>
        void ShowInExplorer(string filePath);

        #endregion
    }
}
