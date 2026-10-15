using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PandocGUIWrapper.Services;
using PandocGUIWrapper.ViewModels;

namespace PandocGUIWrapper.Tests
{
    /// <summary>
    /// Tests for <see cref="MainViewModel"/>.
    /// </summary>
    [TestClass]
    public class MainViewModelTests
    {
        #region Fields

        private FakePandocService _pandoc;
        private FakeDialogService _dialogs;
        private ReferenceDocumentStore _store;

        #endregion

        #region Public Methods

        [TestInitialize]
        public void Setup()
        {
            _pandoc = new FakePandocService();
            _dialogs = new FakeDialogService();
            _store = new ReferenceDocumentStore(new FakeSettingsService());
        }

        [TestMethod]
        public async Task Initialize_WithoutPandoc_ShowsErrorAndDisablesConvert()
        {
            var vm = Create(null);

            await vm.InitializeAsync();

            Assert.AreEqual("pandoc not found on PATH", vm.Convert.Status);
            Assert.AreEqual(DialogKind.Error, _dialogs.Messages.Single().Kind);
            Assert.IsFalse(vm.Convert.ConvertCommand.CanExecute(null));
            Assert.AreEqual(0, vm.ReferenceDocuments.Items.Count);
        }

        [TestMethod]
        public async Task Initialize_LoadsBothTabs()
        {
            var vm = Create(_pandoc);

            await vm.InitializeAsync();

            Assert.AreEqual("Ready", vm.Convert.Status);
            Assert.AreEqual("docx", vm.Convert.SelectedOutputFormat.Format);
            CollectionAssert.AreEqual(new[] { "docx", "odt" }, vm.ReferenceDocuments.Items.Select(i => i.Format).ToArray());
        }

        [TestMethod]
        public async Task Initialize_WhenFormatsFail_ShowsErrorAndDisablesConvert()
        {
            _pandoc.FormatsException = new InvalidOperationException("boom");
            var vm = Create(_pandoc);

            await vm.InitializeAsync();

            StringAssert.Contains(_dialogs.Messages.Single().Message, "boom");
            Assert.AreEqual("Could not read the pandoc formats", vm.Convert.Status);
            Assert.IsFalse(vm.Convert.ConvertCommand.CanExecute(null));
        }

        #endregion

        #region Private Methods

        private MainViewModel Create(IPandocService pandoc)
        {
            return new MainViewModel(
                new ConvertViewModel(pandoc, _dialogs, _store),
                new ReferenceDocumentsViewModel(_store, _dialogs),
                pandoc,
                _dialogs);
        }

        #endregion
    }
}
