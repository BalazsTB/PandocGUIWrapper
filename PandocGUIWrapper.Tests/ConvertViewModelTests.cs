using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PandocGUIWrapper.Services;
using PandocGUIWrapper.ViewModels;

namespace PandocGUIWrapper.Tests
{
    /// <summary>
    /// Tests for <see cref="ConvertViewModel"/>.
    /// </summary>
    [TestClass]
    public class ConvertViewModelTests
    {
        #region Fields

        private string _tempDir;
        private string _inputPath;
        private FakePandocService _pandoc;
        private FakeDialogService _dialogs;
        private ReferenceDocumentStore _store;

        #endregion

        #region Public Methods

        [TestInitialize]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "PandocGuiVmTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _inputPath = Path.Combine(_tempDir, "notes.md");
            File.WriteAllText(_inputPath, "# Notes");
            _pandoc = new FakePandocService();
            _dialogs = new FakeDialogService();
            _store = new ReferenceDocumentStore(new FakeSettingsService());
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [TestMethod]
        public void BeforeLoad_ConvertIsDisabled()
        {
            var vm = new ConvertViewModel(_pandoc, _dialogs, _store);

            Assert.AreEqual("Starting…", vm.Status);
            Assert.IsFalse(vm.ConvertCommand.CanExecute(null));
        }

        [TestMethod]
        public void Load_FillsFormatsAndSelectsDocx()
        {
            var vm = CreateReadyViewModel();

            Assert.AreEqual("Ready", vm.Status);
            Assert.AreEqual(4, vm.OutputFormats.Count);
            Assert.AreEqual("docx", vm.SelectedOutputFormat.Format);
        }

        [TestMethod]
        public void SetStatus_ShowsText()
        {
            var vm = new ConvertViewModel(_pandoc, _dialogs, _store);

            vm.SetStatus("pandoc not found on PATH");

            Assert.AreEqual("pandoc not found on PATH", vm.Status);
        }

        [TestMethod]
        public void BrowseInput_FillsNameAndFolderDefaults()
        {
            var vm = CreateReadyViewModel();
            _dialogs.NextOpenFile = _inputPath;

            vm.BrowseInputCommand.Execute(null);

            Assert.AreEqual(_inputPath, vm.InputPath);
            Assert.AreEqual("notes", vm.OutputName);
            Assert.AreEqual(_tempDir, vm.OutputFolder);
            Assert.IsTrue(vm.ConvertCommand.CanExecute(null));
            StringAssert.Contains(_dialogs.LastOpenFilter, "*.md");
        }

        [TestMethod]
        public void BrowseInput_Cancelled_KeepsValues()
        {
            var vm = CreateReadyViewModel();
            _dialogs.NextOpenFile = null;

            vm.BrowseInputCommand.Execute(null);

            Assert.IsNull(vm.InputPath);
        }

        [TestMethod]
        public void BrowseInput_DoesNotOverwriteEditedNameOrFolder()
        {
            var vm = CreateReadyViewModel();
            string otherFolder = Directory.CreateDirectory(Path.Combine(_tempDir, "out")).FullName;
            vm.OutputName = "custom";
            vm.OutputFolder = otherFolder;
            _dialogs.NextOpenFile = _inputPath;

            vm.BrowseInputCommand.Execute(null);

            Assert.AreEqual("custom", vm.OutputName);
            Assert.AreEqual(otherFolder, vm.OutputFolder);
        }

        [TestMethod]
        public void BrowseInput_AfterNameCleared_FillsNameAgain()
        {
            var vm = CreateReadyViewModel();
            vm.OutputName = "custom";
            vm.OutputName = string.Empty;
            _dialogs.NextOpenFile = _inputPath;

            vm.BrowseInputCommand.Execute(null);

            Assert.AreEqual("notes", vm.OutputName);
        }

        [TestMethod]
        public void BrowseOutputFolder_SetsFolder()
        {
            var vm = CreateReadyViewModel();
            _dialogs.NextFolder = _tempDir;

            vm.BrowseOutputFolderCommand.Execute(null);

            Assert.AreEqual(_tempDir, vm.OutputFolder);
        }

        [TestMethod]
        public void ConvertCommand_DisabledWhenNameEmptyOrFolderMissing()
        {
            var vm = CreateViewModelWithInput();
            Assert.IsTrue(vm.ConvertCommand.CanExecute(null));

            vm.OutputName = " ";
            Assert.IsFalse(vm.ConvertCommand.CanExecute(null));

            vm.OutputName = "notes";
            vm.OutputFolder = Path.Combine(_tempDir, "does-not-exist");
            Assert.IsFalse(vm.ConvertCommand.CanExecute(null));
        }

        [TestMethod]
        public void ConvertCommand_RaisesCanExecuteChangedWhenInputsChange()
        {
            var vm = CreateReadyViewModel();
            int raised = 0;
            vm.ConvertCommand.CanExecuteChanged += (s, e) => raised++;

            vm.OutputName = "x";

            Assert.IsTrue(raised > 0);
        }

        [TestMethod]
        public async Task Convert_Success_CallsPandocAndOffersToOpenFolder()
        {
            var vm = CreateViewModelWithInput();
            _dialogs.ConfirmAnswer = true;

            await vm.ConvertAsync();

            string expectedOutput = Path.Combine(_tempDir, "notes.docx");
            Assert.AreEqual(_inputPath, _pandoc.LastInput);
            Assert.AreEqual(expectedOutput, _pandoc.LastOutput);
            Assert.AreEqual("markdown", _pandoc.LastFrom);
            Assert.AreEqual("docx", _pandoc.LastTo);
            Assert.IsNull(_pandoc.LastReferenceDocument);
            Assert.AreEqual("Converted: " + expectedOutput, vm.Status);
            StringAssert.Contains(_dialogs.Confirms.Single(), "Open folder?");
            Assert.AreEqual(expectedOutput, _dialogs.ShownInExplorer);
            Assert.IsFalse(vm.IsBusy);
            Assert.IsTrue(vm.IsIdle);
        }

        [TestMethod]
        public async Task Convert_Success_DeclineOpenFolder_DoesNotOpenExplorer()
        {
            var vm = CreateViewModelWithInput();
            _dialogs.ConfirmAnswer = false;

            await vm.ConvertAsync();

            Assert.IsNull(_dialogs.ShownInExplorer);
        }

        [TestMethod]
        public async Task Convert_ExistingOutputDeclined_DoesNotCallPandoc()
        {
            var vm = CreateViewModelWithInput();
            File.WriteAllText(Path.Combine(_tempDir, "notes.docx"), "old");
            _dialogs.ConfirmAnswer = false;

            await vm.ConvertAsync();

            StringAssert.Contains(_dialogs.Confirms.Single(), "already exists");
            Assert.AreEqual(0, _pandoc.ConvertCalls);
        }

        [TestMethod]
        public async Task Convert_ExistingOutputConfirmed_CallsPandoc()
        {
            var vm = CreateViewModelWithInput();
            File.WriteAllText(Path.Combine(_tempDir, "notes.docx"), "old");
            _dialogs.ConfirmAnswer = true;

            await vm.ConvertAsync();

            Assert.AreEqual(1, _pandoc.ConvertCalls);
        }

        [TestMethod]
        public async Task Convert_Failure_ShowsPandocError()
        {
            var vm = CreateViewModelWithInput();
            _pandoc.Result = new ConversionResult(false, 43, "pdflatex not found");

            await vm.ConvertAsync();

            Assert.AreEqual("Failed", vm.Status);
            var message = _dialogs.Messages.Single();
            Assert.AreEqual(DialogKind.Error, message.Kind);
            Assert.AreEqual("pdflatex not found", message.Message);
        }

        [TestMethod]
        public async Task Convert_FailureWithoutText_ShowsExitCode()
        {
            var vm = CreateViewModelWithInput();
            _pandoc.Result = new ConversionResult(false, 5, string.Empty);

            await vm.ConvertAsync();

            StringAssert.Contains(_dialogs.Messages.Single().Message, "code 5");
        }

        [TestMethod]
        public async Task Convert_InvalidName_WarnsAndDoesNotCallPandoc()
        {
            var vm = CreateViewModelWithInput();
            vm.OutputName = "a/b";

            await vm.ConvertAsync();

            Assert.AreEqual(DialogKind.Warning, _dialogs.Messages.Single().Kind);
            Assert.AreEqual(0, _pandoc.ConvertCalls);
        }

        [TestMethod]
        public async Task Convert_OutputSameAsInput_WarnsAndDoesNotCallPandoc()
        {
            var vm = CreateViewModelWithInput();
            vm.SelectedOutputFormat = vm.OutputFormats.Single(o => o.Format == "markdown");

            await vm.ConvertAsync();

            StringAssert.Contains(_dialogs.Messages.Single().Message, "same as the input");
            Assert.AreEqual(0, _pandoc.ConvertCalls);
        }

        [TestMethod]
        public void ReferenceDocument_SupportedFormatWithoutPath_ShowsNone()
        {
            var vm = CreateReadyViewModel();

            Assert.IsTrue(vm.IsReferenceDocumentSupported);
            Assert.IsNull(vm.ReferenceDocumentPath);
            Assert.AreEqual("None (pandoc default)", vm.ReferenceDocumentText);
        }

        [TestMethod]
        public void ReferenceDocument_UnsupportedFormat_IsHiddenAndIgnored()
        {
            _store.SetPath("html", @"C:\x\ref.html");
            var vm = CreateReadyViewModel();

            vm.SelectedOutputFormat = vm.OutputFormats.Single(o => o.Format == "html");

            Assert.IsFalse(vm.IsReferenceDocumentSupported);
            Assert.IsNull(vm.ReferenceDocumentPath);
        }

        [TestMethod]
        public void ReferenceDocument_FollowsSelectedFormat()
        {
            _store.SetPath("odt", @"C:\styles\ref.odt");
            var vm = CreateReadyViewModel();

            vm.SelectedOutputFormat = vm.OutputFormats.Single(o => o.Format == "odt");

            Assert.AreEqual(@"C:\styles\ref.odt", vm.ReferenceDocumentPath);
            Assert.AreEqual("ref.odt", vm.ReferenceDocumentText);
        }

        [TestMethod]
        public void ReferenceDocument_StoreChange_RaisesPropertyChanged()
        {
            var vm = CreateReadyViewModel();
            int raised = 0;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ConvertViewModel.ReferenceDocumentText))
                {
                    raised++;
                }
            };

            _store.SetPath("docx", @"C:\styles\ref.docx");

            Assert.AreEqual(1, raised);
            Assert.AreEqual("ref.docx", vm.ReferenceDocumentText);
        }

        [TestMethod]
        public void ReferenceDocument_OtherFormatChange_DoesNotRaise()
        {
            var vm = CreateReadyViewModel();
            int raised = 0;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ConvertViewModel.ReferenceDocumentText))
                {
                    raised++;
                }
            };

            _store.SetPath("odt", @"C:\styles\ref.odt");

            Assert.AreEqual(0, raised);
        }

        [TestMethod]
        public async Task Convert_WithExistingReferenceDocument_PassesIt()
        {
            string referencePath = Path.Combine(_tempDir, "ref.docx");
            File.WriteAllText(referencePath, "x");
            _store.SetPath("docx", referencePath);
            var vm = CreateViewModelWithInput();

            await vm.ConvertAsync();

            Assert.AreEqual(referencePath, _pandoc.LastReferenceDocument);
        }

        [TestMethod]
        public async Task Convert_MissingReferenceDocumentDeclined_DoesNotCallPandoc()
        {
            _store.SetPath("docx", Path.Combine(_tempDir, "gone.docx"));
            var vm = CreateViewModelWithInput();
            _dialogs.ConfirmAnswer = false;

            await vm.ConvertAsync();

            StringAssert.Contains(_dialogs.Confirms.Single(), "Convert without it?");
            Assert.AreEqual(0, _pandoc.ConvertCalls);
        }

        [TestMethod]
        public async Task Convert_MissingReferenceDocumentAccepted_ConvertsWithoutIt()
        {
            _store.SetPath("docx", Path.Combine(_tempDir, "gone.docx"));
            var vm = CreateViewModelWithInput();
            _dialogs.ConfirmAnswer = true;

            await vm.ConvertAsync();

            Assert.AreEqual(1, _pandoc.ConvertCalls);
            Assert.IsNull(_pandoc.LastReferenceDocument);
        }

        #endregion

        #region Private Methods

        private ConvertViewModel CreateReadyViewModel()
        {
            var vm = new ConvertViewModel(_pandoc, _dialogs, _store);
            vm.Load(new[] { "markdown", "docx" }, _pandoc.OutputFormats);
            return vm;
        }

        private ConvertViewModel CreateViewModelWithInput()
        {
            var vm = CreateReadyViewModel();
            _dialogs.NextOpenFile = _inputPath;
            vm.BrowseInputCommand.Execute(null);
            return vm;
        }

        #endregion
    }
}
