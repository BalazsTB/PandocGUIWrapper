# Pandoc GUI Wrapper Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A WPF window where the user picks an input file, output name, output format, and output folder, and then converts the file with pandoc.

**Architecture:** The app uses MVVM.
- `Models/FormatCatalog` maps pandoc formats to names and extensions.
- `Services/PandocService` (`IPandocService`) runs pandoc.
- `Services/DialogService` (`IDialogService`) wraps the WPF and WinForms dialogs.
- `ViewModels/MainViewModel` holds the UI state and behaviour, and exposes commands.
- `MainWindow.xaml` contains bindings only. `App.OnStartup` creates the objects and connects them.
- A separate SDK-style MSTest project links the non-WPF source files directly. This lets it test them without building the WPF project.

> **As built:** `FormatCatalog.cs` is in `PandocGUIWrapper/Models/` (namespace `PandocGUIWrapper.Models`). `PandocService.cs` is in `PandocGUIWrapper/Services/` (namespace `PandocGUIWrapper.Services`), and it implements `IPandocService`. Tasks 1 and 2 show the original single-namespace code. The source files are the authoritative version.

**Tech Stack:** C# 7.3 and WPF on .NET Framework 4.8 (legacy csproj). The tests use MSTest on net48 (SDK-style). pandoc 3.x must be on `PATH`.

**Spec:** `docs/superpowers/specs/2026-10-14-pandoc-gui-wrapper-design.md`

## Global Constraints

- Target framework: .NET Framework 4.8. The app project is a legacy, non-SDK csproj, so its default language version is C# 7.3. Shared source files MUST compile under C# 7.3. Do not use records, `new()` target-typed expressions, switch expressions, `is not`, or nullable reference annotations.
- pandoc is found on `PATH` only. Do not bundle pandoc, and do not add a setting for its path.
- Architecture: MVVM. The View contains no logic, and the ViewModel has no WPF types.
- Scope is four fields and a Convert button. There is no reference-doc field and no field for extra arguments.
- If the output file exists, the user must confirm the overwrite.
- On success, show a `MessageBox` with the text "Converted to <output>. Open folder?" and the buttons Yes and No. Yes runs `explorer.exe /select,"<output>"`.
- The folder is not a git repository, so the plan has no commit steps.

## Tools and commands

- MSBuild: `& "C:\Program Files\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\MSBuild.exe"`
- Run all commands from the repository root: `C:\Users\bbodi\source\repos\Sandbox\PandocGUIWrapper`

## File structure

| File | Responsibility |
|---|---|
| `PandocGUIWrapper/Models/FormatCatalog.cs` | Maps formats to names and extensions, builds the Open-dialog filter, guesses the input format, and creates `FormatOption` items. |
| `PandocGUIWrapper/Services/IPandocService.cs`, `PandocService.cs` | Find pandoc, list the formats, and run the conversion (`ConversionResult`). |
| `PandocGUIWrapper/Services/IDialogService.cs`, `DialogService.cs` | Dialogs and message boxes, and opening a file in Explorer. |
| `PandocGUIWrapper/ViewModels/ObservableObject.cs`, `RelayCommand.cs` | MVVM base types. |
| `PandocGUIWrapper/ViewModels/MainViewModel.cs` | All UI state and behaviour. |
| `PandocGUIWrapper/MainWindow.xaml` (+ `.cs`) | View with bindings only. |
| `PandocGUIWrapper/App.xaml` (+ `.cs`) | Composition root. |
| `PandocGUIWrapper/PandocGUIWrapper.csproj` | Adds the new files and the `System.Windows.Forms` reference. |
| `PandocGUIWrapper.Tests/*` | MSTest project. It links the non-WPF sources and holds the `FormatCatalog`, `PandocService`, and `MainViewModel` tests. |
| `PandocGUIWrapper.slnx` | Adds the test project. |
---

### Task 1: Test project and `FormatCatalog`

**Files:**
- Create: `PandocGUIWrapper.Tests/PandocGUIWrapper.Tests.csproj`
- Create: `PandocGUIWrapper.Tests/FormatCatalogTests.cs`
- Create: `PandocGUIWrapper/FormatCatalog.cs`
- Modify: `PandocGUIWrapper/PandocGUIWrapper.csproj` (add a `Compile` item)
- Modify: `PandocGUIWrapper.slnx`

**Interfaces:**
- Produces (namespace `PandocGUIWrapper`):
  - `public sealed class FormatOption { FormatOption(string format, string displayName); string Format { get; } string DisplayName { get; } override string ToString() => DisplayName; }`
  - `public static class FormatCatalog`
    - `static string GetExtension(string format)`: returns the mapped extension. For a format that is not mapped, it returns `"." + format`.
    - `static string GetDisplayName(string format)`: returns `"Friendly (.ext)"`. For a format that is not mapped, it returns `format`.
    - `static string BuildOpenFilter(IEnumerable<string> inputFormats)`
    - `static string GuessInputFormat(string path)`: returns `null` when the extension is unknown.
    - `static List<FormatOption> CreateOptions(IEnumerable<string> formats)`: returns the options sorted by `DisplayName`, ignoring case.

- [x] **Step 1: Create the test project**

`PandocGUIWrapper.Tests/PandocGUIWrapper.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <LangVersion>7.3</LangVersion>
    <IsPackable>false</IsPackable>
    <RootNamespace>PandocGUIWrapper.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="..\PandocGUIWrapper\FormatCatalog.cs" Link="Linked\FormatCatalog.cs" />
  </ItemGroup>

</Project>
```

Then add the packages. Let them resolve to the latest versions:

```powershell
dotnet add PandocGUIWrapper.Tests package Microsoft.NET.Test.Sdk
dotnet add PandocGUIWrapper.Tests package MSTest.TestAdapter
dotnet add PandocGUIWrapper.Tests package MSTest.TestFramework
dotnet add PandocGUIWrapper.Tests package Microsoft.NETFramework.ReferenceAssemblies
```

Replace `PandocGUIWrapper.slnx` with:

```xml
<Solution>
  <Project Path="PandocGUIWrapper/PandocGUIWrapper.csproj" />
  <Project Path="PandocGUIWrapper.Tests/PandocGUIWrapper.Tests.csproj" />
</Solution>
```

- [x] **Step 2: Write the failing tests**

`PandocGUIWrapper.Tests/FormatCatalogTests.cs`:

```csharp
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PandocGUIWrapper.Tests
{
    [TestClass]
    public class FormatCatalogTests
    {
        [DataTestMethod]
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

        [DataTestMethod]
        [DataRow(@"C:\docs\readme.MD", "markdown")]
        [DataRow(@"C:\docs\report.docx", "docx")]
        [DataRow(@"C:\docs\page.htm", "html")]
        [DataRow(@"C:\docs\paper.tex", "latex")]
        public void GuessInputFormat_KnownExtension_ReturnsFormat(string path, string expected)
        {
            Assert.AreEqual(expected, FormatCatalog.GuessInputFormat(path));
        }

        [DataTestMethod]
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
    }
}
```

- [x] **Step 3: Run the tests to make sure they fail**

Run: `dotnet test PandocGUIWrapper.Tests`
Expected: the build FAILS because `..\PandocGUIWrapper\FormatCatalog.cs` is not found or `FormatCatalog` does not exist.

- [x] **Step 4: Implement `FormatCatalog`**

`PandocGUIWrapper/FormatCatalog.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PandocGUIWrapper
{
    public sealed class FormatOption
    {
        public FormatOption(string format, string displayName)
        {
            Format = format;
            DisplayName = displayName;
        }

        public string Format { get; }

        public string DisplayName { get; }

        public override string ToString() => DisplayName;
    }

    public static class FormatCatalog
    {
        private sealed class FormatInfo
        {
            public FormatInfo(string friendlyName, string extension)
            {
                FriendlyName = friendlyName;
                Extension = extension;
            }

            public string FriendlyName { get; }

            public string Extension { get; }
        }

        private static readonly Dictionary<string, FormatInfo> Formats = new Dictionary<string, FormatInfo>(StringComparer.OrdinalIgnoreCase)
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

        private static readonly Dictionary<string, string> InputExtensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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

        private const string AllFilesFilter = "All files (*.*)|*.*";

        public static string GetExtension(string format)
        {
            FormatInfo info;
            return Formats.TryGetValue(format, out info) ? info.Extension : "." + format;
        }

        public static string GetDisplayName(string format)
        {
            FormatInfo info;
            return Formats.TryGetValue(format, out info) ? info.FriendlyName + " (" + info.Extension + ")" : format;
        }

        public static string GuessInputFormat(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string extension = Path.GetExtension(path);
            string format;
            return !string.IsNullOrEmpty(extension) && InputExtensions.TryGetValue(extension, out format) ? format : null;
        }

        public static string BuildOpenFilter(IEnumerable<string> inputFormats)
        {
            var available = new HashSet<string>(inputFormats ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            var groups = InputExtensions
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

        public static List<FormatOption> CreateOptions(IEnumerable<string> formats)
        {
            return formats
                .Select(format => new FormatOption(format, GetDisplayName(format)))
                .OrderBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string GetFriendlyName(string format)
        {
            FormatInfo info;
            return Formats.TryGetValue(format, out info) ? info.FriendlyName : format;
        }
    }
}
```

Add the file to the app project. In `PandocGUIWrapper/PandocGUIWrapper.csproj`, add a line inside the `ItemGroup` that contains `<Compile Include="App.xaml.cs">`, after the `MainWindow.xaml.cs` `Compile` element:

```xml
    <Compile Include="FormatCatalog.cs" />
```

- [x] **Step 5: Run the tests to make sure they pass**

Run: `dotnet test PandocGUIWrapper.Tests`
Expected: all `FormatCatalogTests` PASS.

---

### Task 2: `PandocService`

**Files:**
- Create: `PandocGUIWrapper/PandocService.cs`
- Create: `PandocGUIWrapper.Tests/PandocServiceTests.cs`
- Modify: `PandocGUIWrapper.Tests/PandocGUIWrapper.Tests.csproj` (link the file)
- Modify: `PandocGUIWrapper/PandocGUIWrapper.csproj` (add a `Compile` item)

**Interfaces:**
- Produces (namespace `PandocGUIWrapper`):
  - `public sealed class ConversionResult { ConversionResult(bool success, int exitCode, string errorText); bool Success; int ExitCode; string ErrorText; }`. All three are get-only properties.
  - `public sealed class PandocService`
    - `PandocService(string pandocPath)`
    - `static string FindPandoc()`: returns the full path to `pandoc.exe`, or `null`.
    - `static string BuildConvertArguments(string inputPath, string outputPath, string fromFormat, string toFormat)`
    - `Task<IReadOnlyList<string>> GetInputFormatsAsync()`: throws `InvalidOperationException` when pandoc fails.
    - `Task<IReadOnlyList<string>> GetOutputFormatsAsync()`: same as above.
    - `Task<ConversionResult> ConvertAsync(string inputPath, string outputPath, string fromFormat, string toFormat)`: never throws. A failure to start pandoc is returned as `Success == false`.

- [x] **Step 1: Write the failing tests**

Add this line to the `Compile` `ItemGroup` in `PandocGUIWrapper.Tests/PandocGUIWrapper.Tests.csproj`:

```xml
    <Compile Include="..\PandocGUIWrapper\PandocService.cs" Link="Linked\PandocService.cs" />
```

`PandocGUIWrapper.Tests/PandocServiceTests.cs`:

```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PandocGUIWrapper.Tests
{
    [TestClass]
    public class PandocServiceTests
    {
        private string _tempDir;

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
            string args = PandocService.BuildConvertArguments(@"C:\in dir\a.md", @"C:\out\a.docx", "markdown", "docx");
            Assert.AreEqual("\"C:\\in dir\\a.md\" -f markdown -t docx -o \"C:\\out\\a.docx\"", args);
        }

        [TestMethod]
        public void BuildConvertArguments_WithoutFromFormat_OmitsFromFlag()
        {
            string args = PandocService.BuildConvertArguments(@"C:\a.xyz", @"C:\a.html", null, "html");
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

            var result = await service.ConvertAsync(input, output, "markdown", "html");

            Assert.IsTrue(result.Success, result.ErrorText);
            Assert.AreEqual(0, result.ExitCode);
            StringAssert.Contains(File.ReadAllText(output), "<h1");
        }

        [TestMethod]
        public async Task ConvertAsync_MissingInput_ReturnsFailureWithError()
        {
            var service = new PandocService(RequirePandoc());

            var result = await service.ConvertAsync(Path.Combine(_tempDir, "missing.md"), Path.Combine(_tempDir, "out.html"), "markdown", "html");

            Assert.IsFalse(result.Success);
            Assert.AreNotEqual(0, result.ExitCode);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorText));
        }

        [TestMethod]
        public async Task ConvertAsync_BadPandocPath_ReturnsFailureInsteadOfThrowing()
        {
            var service = new PandocService(Path.Combine(_tempDir, "nope.exe"));

            var result = await service.ConvertAsync("a.md", "b.html", null, "html");

            Assert.IsFalse(result.Success);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorText));
        }

        private static string RequirePandoc()
        {
            string path = PandocService.FindPandoc();
            if (path == null)
            {
                Assert.Inconclusive("pandoc.exe is not on PATH.");
            }

            return path;
        }
    }

    internal static class ListExtensions
    {
        public static System.Collections.ArrayList ToArrayList(this System.Collections.Generic.IEnumerable<string> items)
        {
            return new System.Collections.ArrayList(System.Linq.Enumerable.ToArray(items));
        }
    }
}
```

- [x] **Step 2: Run the tests to make sure they fail**

Run: `dotnet test PandocGUIWrapper.Tests`
Expected: the build FAILS because `PandocService.cs` is not found.

- [x] **Step 3: Implement `PandocService`**

`PandocGUIWrapper/PandocService.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandocGUIWrapper
{
    public sealed class ConversionResult
    {
        public ConversionResult(bool success, int exitCode, string errorText)
        {
            Success = success;
            ExitCode = exitCode;
            ErrorText = errorText;
        }

        public bool Success { get; }

        public int ExitCode { get; }

        public string ErrorText { get; }
    }

    public sealed class PandocService
    {
        private readonly string _pandocPath;

        public PandocService(string pandocPath)
        {
            _pandocPath = pandocPath ?? throw new ArgumentNullException(nameof(pandocPath));
        }

        public static string FindPandoc()
        {
            string pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (string directory in pathVariable.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    string candidate = Path.Combine(directory.Trim().Trim('"'), "pandoc.exe");
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
                catch (ArgumentException)
                {
                    // PATH entries that contain invalid characters are skipped.
                }
            }

            return null;
        }

        public static string BuildConvertArguments(string inputPath, string outputPath, string fromFormat, string toFormat)
        {
            var builder = new StringBuilder();
            builder.Append(Quote(inputPath));
            if (!string.IsNullOrEmpty(fromFormat))
            {
                builder.Append(" -f ").Append(fromFormat);
            }

            builder.Append(" -t ").Append(toFormat);
            builder.Append(" -o ").Append(Quote(outputPath));
            return builder.ToString();
        }

        public Task<IReadOnlyList<string>> GetInputFormatsAsync() => ListFormatsAsync("--list-input-formats");

        public Task<IReadOnlyList<string>> GetOutputFormatsAsync() => ListFormatsAsync("--list-output-formats");

        public async Task<ConversionResult> ConvertAsync(string inputPath, string outputPath, string fromFormat, string toFormat)
        {
            try
            {
                ProcessOutput output = await RunAsync(BuildConvertArguments(inputPath, outputPath, fromFormat, toFormat)).ConfigureAwait(false);
                return new ConversionResult(output.ExitCode == 0, output.ExitCode, output.StandardError.Trim());
            }
            catch (Exception ex)
            {
                return new ConversionResult(false, -1, "Could not run pandoc: " + ex.Message);
            }
        }

        private async Task<IReadOnlyList<string>> ListFormatsAsync(string argument)
        {
            ProcessOutput output = await RunAsync(argument).ConfigureAwait(false);
            if (output.ExitCode != 0)
            {
                throw new InvalidOperationException("pandoc " + argument + " failed: " + output.StandardError.Trim());
            }

            return output.StandardOutput
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToList();
        }

        private Task<ProcessOutput> RunAsync(string arguments)
        {
            return Task.Run(() =>
            {
                var startInfo = new ProcessStartInfo(_pandocPath, arguments)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                };

                using (var process = Process.Start(startInfo))
                {
                    Task<string> errorTask = process.StandardError.ReadToEndAsync();
                    string standardOutput = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return new ProcessOutput(process.ExitCode, standardOutput, errorTask.Result);
                }
            });
        }

        private static string Quote(string value) => "\"" + value + "\"";

        private sealed class ProcessOutput
        {
            public ProcessOutput(int exitCode, string standardOutput, string standardError)
            {
                ExitCode = exitCode;
                StandardOutput = standardOutput;
                StandardError = standardError;
            }

            public int ExitCode { get; }

            public string StandardOutput { get; }

            public string StandardError { get; }
        }
    }
}
```

`Quote` does not escape anything. Windows paths cannot contain `"`, and the paths here are always files, so they never end with `\`.

Add the file to the app project. In `PandocGUIWrapper/PandocGUIWrapper.csproj`, add this line after `<Compile Include="FormatCatalog.cs" />`:

```xml
    <Compile Include="PandocService.cs" />
```

- [x] **Step 4: Run the tests to make sure they pass**

Run: `dotnet test PandocGUIWrapper.Tests`
Expected: all tests PASS. pandoc is on `PATH` on this machine, so no test should be inconclusive.

---

### Task 3: MVVM layer, view, and composition

**Files:**
- Create: `PandocGUIWrapper/Services/IPandocService.cs`. Holds `GetInputFormatsAsync`, `GetOutputFormatsAsync`, and `ConvertAsync`. `PandocService` implements it.
- Create: `PandocGUIWrapper/Services/IDialogService.cs`. Holds `ShowOpenFileDialog`, `ShowFolderDialog`, `ShowMessage`, `Confirm`, `ShowInExplorer`, and `enum DialogKind { Information, Warning, Error }`.
- Create: `PandocGUIWrapper/Services/DialogService.cs`. This is the WPF implementation. It uses `Microsoft.Win32.OpenFileDialog`, `System.Windows.Forms.FolderBrowserDialog`, `MessageBox`, and `explorer.exe /select`.
- Create: `PandocGUIWrapper/ViewModels/ObservableObject.cs`. Holds `INotifyPropertyChanged` and `SetProperty`.
- Create: `PandocGUIWrapper/ViewModels/RelayCommand.cs`. Holds `ICommand` and `RaiseCanExecuteChanged()`.
- Create: `PandocGUIWrapper/ViewModels/MainViewModel.cs`
- Create: `PandocGUIWrapper.Tests/MainViewModelTests.cs`. Holds 18 tests that use `FakePandocService` and `FakeDialogService`.
- Modify: `PandocGUIWrapper/MainWindow.xaml`. Contains only bindings and `d:DesignInstance`.
- Modify: `PandocGUIWrapper/MainWindow.xaml.cs`. Calls only `InitializeComponent()`.
- Modify: `PandocGUIWrapper/App.xaml`. Remove `StartupUri`.
- Modify: `PandocGUIWrapper/App.xaml.cs`. `OnStartup` creates the services and the ViewModel, shows the window, and awaits `InitializeAsync()`.
- Modify: `PandocGUIWrapper/PandocGUIWrapper.csproj`. Add a `System.Windows.Forms` reference and `Compile` items for all the new files.
- Modify: `PandocGUIWrapper.Tests/PandocGUIWrapper.Tests.csproj`. Link `Models\*.cs`, `ViewModels\*.cs`, `Services\PandocService.cs`, `IPandocService.cs`, and `IDialogService.cs`.

**Interfaces (`MainViewModel`):**
- `MainViewModel(IPandocService pandoc /*null when pandoc is not found*/, IDialogService dialogs)`
- Properties:
  - `string InputPath { get; }`
  - `string OutputName { get; set; }`
  - `string OutputFolder { get; set; }`
  - `IReadOnlyList<FormatOption> OutputFormats { get; }`
  - `FormatOption SelectedOutputFormat { get; set; }`
  - `string Status { get; }`
  - `bool IsBusy { get; }` and `bool IsIdle { get; }`
- Commands, all of type `RelayCommand`: `BrowseInputCommand`, `BrowseOutputFolderCommand`, and `ConvertCommand`.
- Methods: `Task InitializeAsync()` and `Task ConvertAsync()`.
- Edit tracking: the `OutputName` and `OutputFolder` setters mark the field as edited when the value is not blank. Browse-input fills in the defaults through the backing fields, and only for fields the user has not edited.
- `ConvertCommand.CanExecute` is true only when all of these are true:
  - The formats are loaded.
  - The app is not busy.
  - The input file exists.
  - The name is not blank.
  - A format is selected.
  - The folder exists.
- Every property change calls `RaiseCanExecuteChanged`.

- [x] **Step 1: Write the ViewModel tests**. They cover:
  - startup without pandoc
  - loading the formats and selecting the default
  - a failure to load the formats
  - the default name and folder
  - keeping an edited name or folder
  - resetting a cleared name
  - the folder picker
  - the `CanExecute` rules
  - successful conversion and the open-folder prompt
  - declining or accepting an overwrite
  - a pandoc failure, with and without error text
  - an invalid name
  - an output path that is the same as the input path
- [x] **Step 2: Implement the services, `ObservableObject`, `RelayCommand`, and `MainViewModel`.**
- [x] **Step 3: Run `dotnet test PandocGUIWrapper.Tests`.** Expected: 46 tests pass.
- [x] **Step 4: Replace the view, App, and csproj entries. Build the solution with MSBuild.** Expected: `Build succeeded`.
- [x] **Step 5: Smoke check.** The app starts with the title "Pandoc Converter", the status shows "Ready", and Convert is disabled until an input file is chosen.
- [ ] **Step 6: Manual UI pass.** Check that browsing to a file, converting, the overwrite prompt, "Open folder?", and the PDF error dialog all work as described in the spec.