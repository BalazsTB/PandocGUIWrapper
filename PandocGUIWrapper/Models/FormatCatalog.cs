using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PandocGUIWrapper.Models
{
    /// <summary>
    /// Static lookup of pandoc format names: friendly names, file extensions, Open-dialog filters,
    /// and which formats accept a <c>--reference-doc</c>.
    /// </summary>
    public static class FormatCatalog
    {
        #region Constants

        /// <summary>The filter entry that matches every file.</summary>
        private const string AllFilesFilter = "All files (*.*)|*.*";

        #endregion

        #region Fields

        /// <summary>Known pandoc formats mapped to a friendly name and default extension.</summary>
        private static readonly Dictionary<string, FormatInfo> _formats = new Dictionary<string, FormatInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["ansi"] = new FormatInfo("ANSI terminal text", ".txt"),
            ["asciidoc"] = new FormatInfo("AsciiDoc", ".adoc"),
            ["asciidoc_legacy"] = new FormatInfo("AsciiDoc (legacy)", ".adoc"),
            ["asciidoctor"] = new FormatInfo("Asciidoctor", ".adoc"),
            ["beamer"] = new FormatInfo("LaTeX Beamer slides", ".tex"),
            ["biblatex"] = new FormatInfo("BibLaTeX", ".bib"),
            ["bibtex"] = new FormatInfo("BibTeX", ".bib"),
            ["chunkedhtml"] = new FormatInfo("Chunked HTML (zip)", ".zip"),
            ["commonmark"] = new FormatInfo("CommonMark", ".md"),
            ["commonmark_x"] = new FormatInfo("CommonMark (extended)", ".md"),
            ["context"] = new FormatInfo("ConTeXt", ".tex"),
            ["csljson"] = new FormatInfo("CSL JSON", ".json"),
            ["csv"] = new FormatInfo("CSV", ".csv"),
            ["djot"] = new FormatInfo("Djot", ".dj"),
            ["docbook"] = new FormatInfo("DocBook", ".xml"),
            ["docbook4"] = new FormatInfo("DocBook 4", ".xml"),
            ["docbook5"] = new FormatInfo("DocBook 5", ".xml"),
            ["docx"] = new FormatInfo("Word", ".docx"),
            ["dokuwiki"] = new FormatInfo("DokuWiki", ".txt"),
            ["dzslides"] = new FormatInfo("DZSlides", ".html"),
            ["epub"] = new FormatInfo("EPUB", ".epub"),
            ["epub2"] = new FormatInfo("EPUB 2", ".epub"),
            ["epub3"] = new FormatInfo("EPUB 3", ".epub"),
            ["fb2"] = new FormatInfo("FictionBook 2", ".fb2"),
            ["gfm"] = new FormatInfo("GitHub Markdown", ".md"),
            ["haddock"] = new FormatInfo("Haddock", ".txt"),
            ["html"] = new FormatInfo("HTML", ".html"),
            ["html4"] = new FormatInfo("HTML 4", ".html"),
            ["html5"] = new FormatInfo("HTML 5", ".html"),
            ["icml"] = new FormatInfo("InDesign ICML", ".icml"),
            ["ipynb"] = new FormatInfo("Jupyter Notebook", ".ipynb"),
            ["jats"] = new FormatInfo("JATS XML", ".xml"),
            ["jats_archiving"] = new FormatInfo("JATS Archiving XML", ".xml"),
            ["jats_articleauthoring"] = new FormatInfo("JATS Article Authoring XML", ".xml"),
            ["jats_publishing"] = new FormatInfo("JATS Publishing XML", ".xml"),
            ["jira"] = new FormatInfo("Jira wiki", ".txt"),
            ["json"] = new FormatInfo("Pandoc JSON", ".json"),
            ["latex"] = new FormatInfo("LaTeX", ".tex"),
            ["man"] = new FormatInfo("Man page", ".1"),
            ["markdown"] = new FormatInfo("Pandoc Markdown", ".md"),
            ["markdown_mmd"] = new FormatInfo("MultiMarkdown", ".md"),
            ["markdown_phpextra"] = new FormatInfo("PHP Markdown Extra", ".md"),
            ["markdown_strict"] = new FormatInfo("Markdown (strict)", ".md"),
            ["markua"] = new FormatInfo("Markua", ".md"),
            ["mediawiki"] = new FormatInfo("MediaWiki", ".wiki"),
            ["ms"] = new FormatInfo("roff ms", ".ms"),
            ["muse"] = new FormatInfo("Muse", ".muse"),
            ["native"] = new FormatInfo("Pandoc native", ".native"),
            ["odt"] = new FormatInfo("OpenDocument Text", ".odt"),
            ["opendocument"] = new FormatInfo("OpenDocument XML", ".xml"),
            ["opml"] = new FormatInfo("OPML", ".opml"),
            ["org"] = new FormatInfo("Org mode", ".org"),
            ["pdf"] = new FormatInfo("PDF", ".pdf"),
            ["plain"] = new FormatInfo("Plain text", ".txt"),
            ["pptx"] = new FormatInfo("PowerPoint", ".pptx"),
            ["revealjs"] = new FormatInfo("reveal.js slides", ".html"),
            ["ris"] = new FormatInfo("RIS", ".ris"),
            ["rst"] = new FormatInfo("reStructuredText", ".rst"),
            ["rtf"] = new FormatInfo("Rich Text Format", ".rtf"),
            ["s5"] = new FormatInfo("S5 slides", ".html"),
            ["slideous"] = new FormatInfo("Slideous slides", ".html"),
            ["slidy"] = new FormatInfo("Slidy slides", ".html"),
            ["t2t"] = new FormatInfo("txt2tags", ".t2t"),
            ["tei"] = new FormatInfo("TEI XML", ".xml"),
            ["texinfo"] = new FormatInfo("Texinfo", ".texi"),
            ["textile"] = new FormatInfo("Textile", ".textile"),
            ["tsv"] = new FormatInfo("TSV", ".tsv"),
            ["typst"] = new FormatInfo("Typst", ".typ"),
            ["xlsx"] = new FormatInfo("Excel", ".xlsx"),
            ["xml"] = new FormatInfo("Pandoc XML", ".xml"),
            ["xwiki"] = new FormatInfo("XWiki", ".txt"),
            ["zimwiki"] = new FormatInfo("ZimWiki", ".txt"),
        };

        /// <summary>File extensions mapped to the pandoc input format used to read them.</summary>
        private static readonly Dictionary<string, string> _inputExtensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".md"] = "markdown",
            [".markdown"] = "markdown",
            [".txt"] = "markdown",
            [".docx"] = "docx",
            [".odt"] = "odt",
            [".html"] = "html",
            [".htm"] = "html",
            [".tex"] = "latex",
            [".latex"] = "latex",
            [".rst"] = "rst",
            [".epub"] = "epub",
            [".org"] = "org",
            [".ipynb"] = "ipynb",
            [".rtf"] = "rtf",
            [".csv"] = "csv",
            [".tsv"] = "tsv",
            [".json"] = "json",
            [".textile"] = "textile",
            [".typ"] = "typst",
            [".wiki"] = "mediawiki",
            [".adoc"] = "asciidoc",
            [".asciidoc"] = "asciidoc",
            [".pptx"] = "pptx",
            [".xlsx"] = "xlsx",
            [".fb2"] = "fb2",
            [".opml"] = "opml",
            [".bib"] = "bibtex",
            [".ris"] = "ris",
            [".dj"] = "djot",
            [".muse"] = "muse",
            [".t2t"] = "t2t",
            [".dbk"] = "docbook",
            [".native"] = "native",
        };

        /// <summary>Output formats that pandoc styles from a reference document (<c>--reference-doc</c>).</summary>
        private static readonly string[] _referenceDocumentFormats = { "docx", "odt", "pptx" };

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the output formats that accept a reference document, in display order.
        /// </summary>
        public static IReadOnlyList<string> ReferenceDocumentFormats => _referenceDocumentFormats;

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the default file extension for a pandoc format.
        /// </summary>
        /// <param name="format">The pandoc format name, for example <c>docx</c>.</param>
        /// <returns>The mapped extension, or <c>"." + format</c> for an unmapped format.</returns>
        public static string GetExtension(string format)
        {
            FormatInfo info;
            return _formats.TryGetValue(format, out info) ? info.Extension : "." + format;
        }

        /// <summary>
        /// Gets the text shown for a format in the user interface.
        /// </summary>
        /// <param name="format">The pandoc format name.</param>
        /// <returns><c>"Friendly name (.ext)"</c>, or the raw format name for an unmapped format.</returns>
        public static string GetDisplayName(string format)
        {
            FormatInfo info;
            return _formats.TryGetValue(format, out info) ? info.FriendlyName + " (" + info.Extension + ")" : format;
        }

        /// <summary>
        /// Guesses the pandoc input format from a file's extension.
        /// </summary>
        /// <param name="path">The input file path.</param>
        /// <returns>The pandoc input format, or <c>null</c> to let pandoc detect it.</returns>
        public static string GuessInputFormat(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string extension = Path.GetExtension(path);
            string format;
            return !string.IsNullOrEmpty(extension) && _inputExtensions.TryGetValue(extension, out format) ? format : null;
        }

        /// <summary>
        /// Builds the Open-file dialog filter for the input formats that pandoc can read.
        /// </summary>
        /// <param name="inputFormats">The input formats reported by pandoc.</param>
        /// <returns>A filter that starts with "All supported files" and ends with "All files (*.*)".</returns>
        public static string BuildOpenFilter(IEnumerable<string> inputFormats)
        {
            var available = new HashSet<string>(inputFormats ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            var groups = _inputExtensions
                .Where(pair => available.Contains(pair.Value))
                .GroupBy(pair => pair.Value, pair => "*" + pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => new { Name = GetFriendlyName(group.Key), Patterns = string.Join(";", group) })
                .OrderBy(group => group.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var parts = new List<string>();
            if (groups.Count > 0)
            {
                parts.Add("All supported files|" + string.Join(";", groups.Select(group => group.Patterns)));
                parts.AddRange(groups.Select(group => group.Name + " (" + group.Patterns + ")|" + group.Patterns));
            }

            parts.Add(AllFilesFilter);
            return string.Join("|", parts);
        }

        /// <summary>
        /// Builds an Open-file dialog filter that matches only one format's extension.
        /// </summary>
        /// <param name="format">The pandoc format name, for example <c>docx</c>.</param>
        /// <returns>For example <c>"Word (*.docx)|*.docx"</c>.</returns>
        public static string BuildSingleFormatFilter(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentException("A format is required.", nameof(format));
            }

            string pattern = "*" + GetExtension(format);
            return GetFriendlyName(format) + " (" + pattern + ")|" + pattern;
        }

        /// <summary>
        /// Checks whether pandoc accepts a reference document for an output format.
        /// </summary>
        /// <param name="format">The pandoc output format name. The check ignores case.</param>
        /// <returns><c>true</c> for docx, odt, and pptx; otherwise <c>false</c>.</returns>
        public static bool SupportsReferenceDocument(string format)
        {
            return format != null && _referenceDocumentFormats.Contains(format, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates the options shown in the output-format list.
        /// </summary>
        /// <param name="formats">The output formats reported by pandoc.</param>
        /// <returns>The options, sorted by display name.</returns>
        public static List<FormatOption> CreateOptions(IEnumerable<string> formats)
        {
            return formats
                .Select(format => new FormatOption(format, GetDisplayName(format)))
                .OrderBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the friendly name of a format without its extension.
        /// </summary>
        /// <param name="format">The pandoc format name.</param>
        /// <returns>The friendly name, or the raw format name for an unmapped format.</returns>
        private static string GetFriendlyName(string format)
        {
            FormatInfo info;
            return _formats.TryGetValue(format, out info) ? info.FriendlyName : format;
        }

        #endregion
    }
}
