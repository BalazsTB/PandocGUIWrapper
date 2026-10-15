using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PandocGUIWrapper.Models;
using PandocGUIWrapper.Services;

namespace PandocGUIWrapper.Tests
{
    /// <summary>
    /// Tests for <see cref="JsonSettingsService"/>.
    /// </summary>
    [TestClass]
    public class JsonSettingsServiceTests
    {
        #region Fields

        private string _tempDir;
        private string _filePath;

        #endregion

        #region Public Methods

        [TestInitialize]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "PandocGuiSettingsTests_" + Guid.NewGuid().ToString("N"));
            _filePath = Path.Combine(_tempDir, "sub", "settings.json");
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
        public void Load_MissingFile_ReturnsEmptySettings()
        {
            var settings = new JsonSettingsService(_filePath).Load();

            Assert.AreEqual(0, settings.ReferenceDocuments.Count);
        }

        [TestMethod]
        public void SaveThenLoad_RoundTripsAndCreatesFolder()
        {
            var service = new JsonSettingsService(_filePath);
            var settings = new AppSettings();
            settings.ReferenceDocuments["docx"] = @"C:\styles\ref.docx";

            service.Save(settings);
            AppSettings loaded = service.Load();

            Assert.IsTrue(File.Exists(_filePath));
            Assert.AreEqual(@"C:\styles\ref.docx", loaded.ReferenceDocuments["DOCX"]);
        }

        [TestMethod]
        [DataRow("{ not json")]
        [DataRow("null")]
        [DataRow("")]
        [DataRow("{ \"ReferenceDocuments\": null }")]
        public void Load_CorruptOrEmptyFile_ReturnsEmptySettings(string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
            File.WriteAllText(_filePath, content);

            var settings = new JsonSettingsService(_filePath).Load();

            Assert.IsNotNull(settings.ReferenceDocuments);
            Assert.AreEqual(0, settings.ReferenceDocuments.Count);
        }

        [TestMethod]
        public void DefaultFilePath_IsInAppData()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            StringAssert.StartsWith(JsonSettingsService.DefaultFilePath, appData);
            StringAssert.EndsWith(JsonSettingsService.DefaultFilePath, @"PandocGUIWrapper\settings.json");
        }

        [TestMethod]
        public void Constructor_BlankPath_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new JsonSettingsService(" "));
        }

        #endregion
    }
}
