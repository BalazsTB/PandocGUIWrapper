using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PandocGUIWrapper.Models;
using PandocGUIWrapper.Services;

namespace PandocGUIWrapper.Tests
{
    /// <summary>
    /// Tests for <see cref="PandocService"/>. Tests that run pandoc are inconclusive when it is not on PATH.
    /// </summary>
    [TestClass]
    public class PandocServiceTests
    {
        #region Fields

        private string _tempDir;

        #endregion

        #region Public Methods

        [TestInitialize]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "PandocGuiTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
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
        public void BuildConvertArguments_WithFromFormat_IncludesFromFlag()
        {
            string args = PandocService.BuildConvertArguments(@"C:\in dir\a.md", @"C:\out\a.docx", "markdown", "docx", null);
            Assert.AreEqual("\"C:\\in dir\\a.md\" -f markdown -t docx -o \"C:\\out\\a.docx\"", args);
        }

        [TestMethod]
        public void BuildConvertArguments_WithoutFromFormat_OmitsFromFlag()
        {
            string args = PandocService.BuildConvertArguments(@"C:\a.xyz", @"C:\a.html", null, "html", null);
            Assert.AreEqual("\"C:\\a.xyz\" -t html -o \"C:\\a.html\"", args);
        }

        [TestMethod]
        public void FindPandoc_WhenOnPath_ReturnsExistingFile()
        {
            string path = RequirePandoc();
            Assert.IsTrue(File.Exists(path));
            StringAssert.EndsWith(path.ToLowerInvariant(), "pandoc.exe");
        }

        [TestMethod]
        public async Task GetFormatsAsync_ReturnsKnownFormats()
        {
            var service = new PandocService(RequirePandoc());

            var inputs = await service.GetInputFormatsAsync();
            var outputs = await service.GetOutputFormatsAsync();

            CollectionAssert.Contains(inputs.ToArrayList(), "markdown");
            CollectionAssert.Contains(outputs.ToArrayList(), "docx");
        }

        [TestMethod]
        public async Task ConvertAsync_MarkdownToHtml_CreatesFile()
        {
            var service = new PandocService(RequirePandoc());
            string input = Path.Combine(_tempDir, "sample.md");
            string output = Path.Combine(_tempDir, "sample.html");
            File.WriteAllText(input, "# Hello\n\nWorld");

            var result = await service.ConvertAsync(input, output, "markdown", "html", null);

            Assert.IsTrue(result.Success, result.ErrorText);
            Assert.AreEqual(0, result.ExitCode);
            StringAssert.Contains(File.ReadAllText(output), "<h1");
        }

        [TestMethod]
        public async Task ConvertAsync_MissingInput_ReturnsFailureWithError()
        {
            var service = new PandocService(RequirePandoc());

            var result = await service.ConvertAsync(Path.Combine(_tempDir, "missing.md"), Path.Combine(_tempDir, "out.html"), "markdown", "html", null);

            Assert.IsFalse(result.Success);
            Assert.AreNotEqual(0, result.ExitCode);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorText));
        }

        [TestMethod]
        public async Task ConvertAsync_BadPandocPath_ReturnsFailureInsteadOfThrowing()
        {
            var service = new PandocService(Path.Combine(_tempDir, "nope.exe"));

            var result = await service.ConvertAsync("a.md", "b.html", null, "html", null);

            Assert.IsFalse(result.Success);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorText));
        }

        [TestMethod]
        public void BuildConvertArguments_WithReferenceDocument_AddsQuotedOption()
        {
            string args = PandocService.BuildConvertArguments(@"C:\a.md", @"C:\a.docx", "markdown", "docx", @"C:\my styles\ref.docx");
            Assert.AreEqual("\"C:\\a.md\" -f markdown -t docx --reference-doc \"C:\\my styles\\ref.docx\" -o \"C:\\a.docx\"", args);
        }

        [TestMethod]
        public async Task ConvertAsync_WithReferenceDocument_CreatesDocx()
        {
            string pandoc = RequirePandoc();
            string referenceDoc = Path.Combine(_tempDir, "my ref.docx");
            var dump = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(pandoc, "-o \"" + referenceDoc + "\" --print-default-data-file reference.docx")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            dump.WaitForExit();
            Assert.IsTrue(File.Exists(referenceDoc), "The default reference.docx could not be created.");

            var service = new PandocService(pandoc);
            string input = Path.Combine(_tempDir, "sample.md");
            string output = Path.Combine(_tempDir, "sample.docx");
            File.WriteAllText(input, "# Hello");

            var result = await service.ConvertAsync(input, output, "markdown", "docx", referenceDoc);

            Assert.IsTrue(result.Success, result.ErrorText);
            Assert.IsTrue(new FileInfo(output).Length > 0);
        }

        [TestMethod]
        public async Task ConvertAsync_MissingReferenceDocument_ReturnsFailure()
        {
            var service = new PandocService(RequirePandoc());
            string input = Path.Combine(_tempDir, "sample.md");
            File.WriteAllText(input, "# Hello");

            var result = await service.ConvertAsync(input, Path.Combine(_tempDir, "sample.docx"), "markdown", "docx", Path.Combine(_tempDir, "nope.docx"));

            Assert.IsFalse(result.Success);
        }

        #endregion

        #region Private Methods

        private static string RequirePandoc()
        {
            string path = PandocService.FindPandoc();
            if (path == null)
            {
                Assert.Inconclusive("pandoc.exe is not on PATH.");
            }

            return path;
        }

        #endregion
    }

    /// <summary>
    /// Helpers for <see cref="CollectionAssert"/>.
    /// </summary>
    internal static class ListExtensions
    {
        #region Public Methods

        public static System.Collections.ArrayList ToArrayList(this System.Collections.Generic.IEnumerable<string> items)
        {
            return new System.Collections.ArrayList(System.Linq.Enumerable.ToArray(items));
        }

        #endregion
    }
}
