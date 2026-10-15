using System.Diagnostics;
using System.IO;
using System.Windows;

namespace PandocGUIWrapper.Services
{
    /// <summary>
    /// The WPF implementation of <see cref="IDialogService"/>.
    /// </summary>
    public sealed class DialogService : IDialogService
    {

        #region Public Methods

        /// <inheritdoc />
        public string ShowOpenFileDialog(string title, string filter, string initialDirectory)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = title,
                Filter = filter,
                CheckFileExists = true,
            };

            if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
            {
                dialog.InitialDirectory = initialDirectory;
            }

            bool? result = Owner != null ? dialog.ShowDialog(Owner) : dialog.ShowDialog();
            return result == true ? dialog.FileName : null;
        }

        /// <inheritdoc />
        public string ShowFolderDialog(string description, string initialPath)
        {
            // WPF on .NET Framework has no folder picker, so the WinForms dialog is used.
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog { Description = description, ShowNewFolderButton = true })
            {
                if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
                {
                    dialog.SelectedPath = initialPath;
                }

                return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
            }
        }

        /// <inheritdoc />
        public void ShowMessage(string message, string title, DialogKind kind)
        {
            Show(message, title, MessageBoxButton.OK, kind);
        }

        /// <inheritdoc />
        public bool Confirm(string message, string title, DialogKind kind)
        {
            return Show(message, title, MessageBoxButton.YesNo, kind) == MessageBoxResult.Yes;
        }

        /// <inheritdoc />
        public void ShowInExplorer(string filePath)
        {
            Process.Start("explorer.exe", "/select,\"" + filePath + "\"");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the window that owns the dialogs, or <c>null</c> before the main window exists.
        /// </summary>
        private static Window Owner => Application.Current?.MainWindow;

        /// <summary>
        /// Shows a message box with the icon for <paramref name="kind"/>.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="title">The window title.</param>
        /// <param name="buttons">The buttons to show.</param>
        /// <param name="kind">The icon to show.</param>
        /// <returns>The button the user selected.</returns>
        private static MessageBoxResult Show(string message, string title, MessageBoxButton buttons, DialogKind kind)
        {
            MessageBoxImage image;
            switch (kind)
            {
                case DialogKind.Error:
                    image = MessageBoxImage.Error;
                    break;
                case DialogKind.Warning:
                    image = MessageBoxImage.Warning;
                    break;
                default:
                    image = MessageBoxImage.Information;
                    break;
            }

            return Owner != null
                ? MessageBox.Show(Owner, message, title, buttons, image)
                : MessageBox.Show(message, title, buttons, image);
        }

        #endregion
    }
}
