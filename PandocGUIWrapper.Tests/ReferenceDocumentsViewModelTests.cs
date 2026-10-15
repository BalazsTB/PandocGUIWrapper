using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PandocGUIWrapper.Services;
using PandocGUIWrapper.ViewModels;

namespace PandocGUIWrapper.Tests
{
    /// <summary>
    /// Tests for <see cref="ReferenceDocumentsViewModel"/> and <see cref="ReferenceDocumentItemViewModel"/>.
    /// </summary>
    [TestClass]
    public class ReferenceDocumentsViewModelTests
    {
        #region Fields

        private FakeDialogService _dialogs;
        private FakeSettingsService _settings;
        private ReferenceDocumentStore _store;

        #endregion

        #region Public Methods

        [TestInitialize]
        public void Setup()
        {
            _dialogs = new FakeDialogService();
            _settings = new FakeSettingsService();
            _store = new ReferenceDocumentStore(_settings);
        }

        [TestMethod]
        public void Load_AddsOnlySupportedFormatsThatPandocLists_InFixedOrder()
        {
            var vm = new ReferenceDocumentsViewModel(_store, _dialogs);

            vm.Load(new[] { "html", "pptx", "markdown", "docx" });

            CollectionAssert.AreEqual(new[] { "docx", "pptx" }, vm.Items.Select(i => i.Format).ToArray());
        }

        [TestMethod]
        public void Load_Twice_ReplacesItems()
        {
            var vm = new ReferenceDocumentsViewModel(_store, _dialogs);

            vm.Load(new[] { "docx", "odt", "pptx" });
            vm.Load(new[] { "odt" });

            Assert.AreEqual("odt", vm.Items.Single().Format);
        }

        [TestMethod]
        public void Item_WithoutPath_CannotClear()
        {
            var item = new ReferenceDocumentItemViewModel("docx", _store, _dialogs);

            Assert.IsNull(item.Path);
            Assert.IsFalse(item.HasPath);
            Assert.IsFalse(item.ClearCommand.CanExecute(null));
            Assert.AreEqual("Word (.docx)", item.DisplayName);
        }

        [TestMethod]
        public void Browse_SavesSelectedFileAndUsesSingleFormatFilter()
        {
            var item = new ReferenceDocumentItemViewModel("docx", _store, _dialogs);
            _dialogs.NextOpenFile = @"C:\styles\ref.docx";

            item.BrowseCommand.Execute(null);

            Assert.AreEqual(@"C:\styles\ref.docx", item.Path);
            Assert.IsTrue(item.HasPath);
            Assert.IsTrue(item.ClearCommand.CanExecute(null));
            Assert.AreEqual(@"C:\styles\ref.docx", _store.GetPath("docx"));
            Assert.AreEqual(1, _settings.SaveCalls);
            Assert.AreEqual("Word (*.docx)|*.docx", _dialogs.LastOpenFilter);
        }

        [TestMethod]
        public void Browse_Cancelled_DoesNotSave()
        {
            var item = new ReferenceDocumentItemViewModel("docx", _store, _dialogs);
            _dialogs.NextOpenFile = null;

            item.BrowseCommand.Execute(null);

            Assert.IsNull(item.Path);
            Assert.AreEqual(0, _settings.SaveCalls);
        }

        [TestMethod]
        public void Clear_RemovesPath()
        {
            _store.SetPath("odt", @"C:\styles\ref.odt");
            var item = new ReferenceDocumentItemViewModel("odt", _store, _dialogs);
            int canExecuteChanged = 0;
            item.ClearCommand.CanExecuteChanged += (s, e) => canExecuteChanged++;

            item.ClearCommand.Execute(null);

            Assert.IsNull(item.Path);
            Assert.IsNull(_store.GetPath("odt"));
            Assert.IsTrue(canExecuteChanged > 0);
        }

        [TestMethod]
        public void Item_RaisesPathChangedOnlyForItsFormat()
        {
            var item = new ReferenceDocumentItemViewModel("docx", _store, _dialogs);
            int raised = 0;
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ReferenceDocumentItemViewModel.Path))
                {
                    raised++;
                }
            };

            _store.SetPath("pptx", @"C:\a.pptx");
            _store.SetPath("DOCX", @"C:\a.docx");

            Assert.AreEqual(1, raised);
        }

        [TestMethod]
        public void Browse_SaveFails_ShowsWarning()
        {
            var item = new ReferenceDocumentItemViewModel("docx", _store, _dialogs);
            _settings.SaveException = new IOException("disk full");
            _dialogs.NextOpenFile = @"C:\styles\ref.docx";

            item.BrowseCommand.Execute(null);

            var message = _dialogs.Messages.Single();
            Assert.AreEqual(DialogKind.Warning, message.Kind);
            StringAssert.Contains(message.Message, "disk full");
        }

        #endregion
    }
}
