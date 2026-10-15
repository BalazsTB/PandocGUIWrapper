using System.Windows;

namespace PandocGUIWrapper
{
    /// <summary>
    /// The main window. It contains only the tab layout; all logic is in <see cref="ViewModels.MainViewModel"/>.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion
    }
}