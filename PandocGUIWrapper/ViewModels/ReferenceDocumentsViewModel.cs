using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PandocGUIWrapper.Models;
using PandocGUIWrapper.Services;

namespace PandocGUIWrapper.ViewModels
{
    /// <summary>
    /// The ViewModel for the Reference documents tab.
    /// </summary>
    public sealed class ReferenceDocumentsViewModel : ObservableObject
    {
        #region Fields

        /// <summary>The store that holds the reference documents.</summary>
        private readonly IReferenceDocumentStore _store;

        /// <summary>The service that shows dialogs.</summary>
        private readonly IDialogService _dialogs;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDocumentsViewModel"/> class.
        /// </summary>
        /// <param name="store">The store that holds the reference documents.</param>
        /// <param name="dialogs">The service that shows dialogs.</param>
        public ReferenceDocumentsViewModel(IReferenceDocumentStore store, IDialogService dialogs)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        }

        #endregion

        #region Public Properties

        /// <summary>Gets one row for each output format that supports a reference document.</summary>
        public ObservableCollection<ReferenceDocumentItemViewModel> Items { get; } = new ObservableCollection<ReferenceDocumentItemViewModel>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates the rows for the reference-document formats that this pandoc can write.
        /// </summary>
        /// <param name="outputFormats">The output formats pandoc can write.</param>
        public void Load(IReadOnlyList<string> outputFormats)
        {
            var available = new HashSet<string>(outputFormats ?? new string[0], StringComparer.OrdinalIgnoreCase);

            Items.Clear();
            foreach (string format in FormatCatalog.ReferenceDocumentFormats.Where(available.Contains))
            {
                Items.Add(new ReferenceDocumentItemViewModel(format, _store, _dialogs));
            }
        }

        #endregion
    }
}
