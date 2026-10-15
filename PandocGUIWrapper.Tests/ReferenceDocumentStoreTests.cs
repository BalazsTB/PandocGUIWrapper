using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PandocGUIWrapper.Models;
using PandocGUIWrapper.Services;

namespace PandocGUIWrapper.Tests
{
    /// <summary>
    /// Tests for <see cref="ReferenceDocumentStore"/>.
    /// </summary>
    [TestClass]
    public class ReferenceDocumentStoreTests
    {
        #region Public Methods

        [TestMethod]
        public void GetPath_ReturnsLoadedValue_IgnoringCase()
        {
            var settings = new FakeSettingsService();
            settings.Loaded.ReferenceDocuments["docx"] = @"C:\ref.docx";
            var store = new ReferenceDocumentStore(settings);

            Assert.AreEqual(@"C:\ref.docx", store.GetPath("DOCX"));
            Assert.IsNull(store.GetPath("odt"));
            Assert.IsNull(store.GetPath(null));
        }

        [TestMethod]
        public void Constructor_NullSettings_UsesEmptySettings()
        {
            var settings = new FakeSettingsService { Loaded = null };

            var store = new ReferenceDocumentStore(settings);

            Assert.IsNull(store.GetPath("docx"));
        }

        [TestMethod]
        public void SetPath_SavesAndRaisesChanged()
        {
            var settings = new FakeSettingsService();
            var store = new ReferenceDocumentStore(settings);
            var changed = new List<string>();
            store.Changed += (s, e) => changed.Add(e.Format);

            store.SetPath("odt", @"C:\ref.odt");

            Assert.AreEqual(1, settings.SaveCalls);
            Assert.AreEqual(@"C:\ref.odt", settings.Saved.ReferenceDocuments["odt"]);
            CollectionAssert.AreEqual(new[] { "odt" }, changed);
        }

        [TestMethod]
        public void ClearPath_RemovesSavesAndRaisesChanged()
        {
            var settings = new FakeSettingsService();
            settings.Loaded.ReferenceDocuments["pptx"] = @"C:\ref.pptx";
            var store = new ReferenceDocumentStore(settings);
            int changed = 0;
            store.Changed += (s, e) => changed++;

            store.ClearPath("pptx");

            Assert.IsNull(store.GetPath("pptx"));
            Assert.AreEqual(1, settings.SaveCalls);
            Assert.AreEqual(1, changed);
        }

        [TestMethod]
        public void ClearPath_NothingSet_DoesNotSave()
        {
            var settings = new FakeSettingsService();
            var store = new ReferenceDocumentStore(settings);

            store.ClearPath("docx");

            Assert.AreEqual(0, settings.SaveCalls);
        }

        [TestMethod]
        public void SetPath_BlankArguments_Throw()
        {
            var store = new ReferenceDocumentStore(new FakeSettingsService());

            Assert.ThrowsExactly<ArgumentException>(() => store.SetPath(" ", @"C:\a.docx"));
            Assert.ThrowsExactly<ArgumentException>(() => store.SetPath("docx", ""));
            Assert.ThrowsExactly<ArgumentException>(() => store.ClearPath(null));
        }

        [TestMethod]
        public void SetPath_SaveFails_ThrowsButStillRaisesChanged()
        {
            var settings = new FakeSettingsService { SaveException = new IOException("locked") };
            var store = new ReferenceDocumentStore(settings);
            int changed = 0;
            store.Changed += (s, e) => changed++;

            Assert.ThrowsExactly<IOException>(() => store.SetPath("docx", @"C:\a.docx"));
            Assert.AreEqual(1, changed);
        }

        [TestMethod]
        public void AppSettings_NullMap_BecomesEmptyCaseInsensitiveMap()
        {
            var settings = new AppSettings { ReferenceDocuments = null };
            settings.ReferenceDocuments["DOCX"] = "x";

            Assert.AreEqual("x", settings.ReferenceDocuments["docx"]);
        }

        #endregion
    }
}
