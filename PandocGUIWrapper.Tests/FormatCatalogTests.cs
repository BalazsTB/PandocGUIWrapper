using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PandocGUIWrapper.Models;
using PandocGUIWrapper.Services;

namespace PandocGUIWrapper.Tests
{
    /// <summary>
    /// Tests for <see cref="FormatCatalog"/>.
    /// </summary>
    [TestClass]
    public class FormatCatalogTests
    {
        #region Public Methods

        [TestMethod]
        [DataRow("docx", ".docx")]
        [DataRow("markdown", ".md")]
        [DataRow("gfm", ".md")]
        [DataRow("latex", ".tex")]
        [DataRow("html5", ".html")]
        [DataRow("plain", ".txt")]
        [DataRow("DOCX", ".docx")]
        public void GetExtension_MappedFormat_ReturnsMappedExtension(string format, string expected)
        {
            Assert.AreEqual(expected, FormatCatalog.GetExtension(format));
        }

        [TestMethod]
        public void GetExtension_UnmappedFormat_ReturnsDotFormat()
        {
            Assert.AreEqual(".foo", FormatCatalog.GetExtension("foo"));
        }

        [TestMethod]
        public void GetDisplayName_MappedFormat_ReturnsFriendlyNameWithExtension()
        {
            Assert.AreEqual("Word (.docx)", FormatCatalog.GetDisplayName("docx"));
        }

        [TestMethod]
        public void GetDisplayName_UnmappedFormat_ReturnsRawName()
        {
            Assert.AreEqual("foo", FormatCatalog.GetDisplayName("foo"));
        }

        [TestMethod]
        [DataRow(@"C:\docs\readme.MD", "markdown")]
        [DataRow(@"C:\docs\report.docx", "docx")]
        [DataRow(@"C:\docs\page.htm", "html")]
        [DataRow(@"C:\docs\paper.tex", "latex")]
        public void GuessInputFormat_KnownExtension_ReturnsFormat(string path, string expected)
        {
            Assert.AreEqual(expected, FormatCatalog.GuessInputFormat(path));
        }

        [TestMethod]
        [DataRow(@"C:\docs\file.xyz")]
        [DataRow(@"C:\docs\noextension")]
        [DataRow("")]
        [DataRow(null)]
        public void GuessInputFormat_UnknownExtension_ReturnsNull(string path)
        {
            Assert.IsNull(FormatCatalog.GuessInputFormat(path));
        }

        [TestMethod]
        public void BuildOpenFilter_WithFormats_ContainsSupportedGroupsAndAllFiles()
        {
            string filter = FormatCatalog.BuildOpenFilter(new[] { "markdown", "docx" });

            StringAssert.StartsWith(filter, "All supported files|");
            StringAssert.Contains(filter, "Word (*.docx)|*.docx");
            StringAssert.Contains(filter, "*.md");
            StringAssert.EndsWith(filter, "All files (*.*)|*.*");
            Assert.IsFalse(filter.Contains("*.odt"), "Formats that pandoc does not list must not appear.");
            Assert.AreEqual(0, filter.Split('|').Length % 2, "A filter must contain pairs of description and pattern.");
        }

        [TestMethod]
        public void BuildOpenFilter_NoFormats_ReturnsAllFilesOnly()
        {
            Assert.AreEqual("All files (*.*)|*.*", FormatCatalog.BuildOpenFilter(new string[0]));
            Assert.AreEqual("All files (*.*)|*.*", FormatCatalog.BuildOpenFilter(null));
        }

        [TestMethod]
        public void CreateOptions_SortsByDisplayNameAndKeepsUnmapped()
        {
            var options = FormatCatalog.CreateOptions(new[] { "docx", "markdown", "foo" });

            CollectionAssert.AreEqual(
                new[] { "foo", "Pandoc Markdown (.md)", "Word (.docx)" },
                options.Select(o => o.DisplayName).ToArray());
            Assert.AreEqual("docx", options.Single(o => o.DisplayName == "Word (.docx)").Format);
            Assert.AreEqual("Word (.docx)", options.Last().ToString());
        }

        [TestMethod]
        [DataRow("docx", true)]
        [DataRow("ODT", true)]
        [DataRow("pptx", true)]
        [DataRow("html", false)]
        [DataRow("latex", false)]
        [DataRow(null, false)]
        public void SupportsReferenceDocument_OnlyForOfficeFormats(string format, bool expected)
        {
            Assert.AreEqual(expected, FormatCatalog.SupportsReferenceDocument(format));
        }

        [TestMethod]
        public void ReferenceDocumentFormats_AreDocxOdtPptxInOrder()
        {
            CollectionAssert.AreEqual(new[] { "docx", "odt", "pptx" }, FormatCatalog.ReferenceDocumentFormats.ToArray());
        }

        [TestMethod]
        public void BuildSingleFormatFilter_ReturnsOnePair()
        {
            Assert.AreEqual("Word (*.docx)|*.docx", FormatCatalog.BuildSingleFormatFilter("docx"));
        }

        [TestMethod]
        public void BuildSingleFormatFilter_BlankFormat_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(() => FormatCatalog.BuildSingleFormatFilter(" "));
        }

        #endregion
    }
}
