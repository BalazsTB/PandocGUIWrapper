using System.Windows;
using PandocGUIWrapper.Services;
using PandocGUIWrapper.ViewModels;

namespace PandocGUIWrapper
{
    /// <summary>
    /// The application. <see cref="OnStartup"/> is the composition root that creates the services and ViewModels.
    /// </summary>
    public partial class App : Application
    {
        #region Overrides

        /// <summary>
        /// Creates the services and ViewModels, shows the main window, and loads pandoc's formats.
        /// </summary>
        /// <param name="e">The startup arguments.</param>
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Services
            string pandocPath = PandocService.FindPandoc();
            IPandocService pandoc = pandocPath == null ? null : new PandocService(pandocPath);
            IDialogService dialogs = new DialogService();
            IReferenceDocumentStore referenceDocuments = new ReferenceDocumentStore(new JsonSettingsService(JsonSettingsService.DefaultFilePath));

            // ViewModels
            var viewModel = new MainViewModel(
                new ConvertViewModel(pandoc, dialogs, referenceDocuments),
                new ReferenceDocumentsViewModel(referenceDocuments, dialogs),
                pandoc,
                dialogs);

            var window = new MainWindow { DataContext = viewModel };
            MainWindow = window;
            window.Show();

            await viewModel.InitializeAsync();
        }

        #endregion
    }
}