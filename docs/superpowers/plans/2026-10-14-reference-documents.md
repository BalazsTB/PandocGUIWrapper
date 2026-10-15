# Reference Documents Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a "Reference documents" tab that stores one `--reference-doc` file for each of docx, odt, and pptx, and apply it automatically on the Convert tab.

**Architecture:** MVVM with CommunityToolkit.Mvvm base classes.
- `MainViewModel` holds `ConvertViewModel` and `ReferenceDocumentsViewModel`.
- The two tabs share `IReferenceDocumentStore`. It stores its data through `ISettingsService` (`JsonSettingsService`, which uses Newtonsoft.Json).
- Each tab is a `UserControl` in `Views\`.

**Tech Stack:**
- C# 7.3, WPF, .NET Framework 4.8
- CommunityToolkit.Mvvm and Newtonsoft.Json (PackageReference in the legacy csproj)
- MSTest

**Spec:** `docs/superpowers/specs/2026-10-14-reference-documents-design.md`

## Global Constraints

- C# 7.3. Do not use toolkit source generators (`[ObservableProperty]` or `[RelayCommand]`). Use only `ObservableObject`, `RelayCommand`, and `AsyncRelayCommand`.
- Formats with reference documents: `docx`, `odt`, and `pptx`. A format is shown only if pandoc lists it as an output format.
- Settings file: `%AppData%\PandocGUIWrapper\settings.json`. Every change saves at once. A missing or corrupt file loads as empty settings.
- Reference document missing at Convert: ask "convert without it?" Yes converts without it. No cancels.
- Convert tab: show the reference document as read-only text ("corp.docx" or "None (pandoc default)") with the full path as a tooltip. Hide the row for formats that do not support reference documents.
- Code style (codingconventions skill):
  - Every class gets `#region` blocks with the standard names and order: Constants, Fields, Constructors, Public Properties, Events, Public Methods, Private Methods, Event Handlers.
  - There must be one blank line after each `#region` line and one before each `#endregion` line.
  - Do not nest regions.
  - XML doc comments go on all types and members.
  - Private fields use `_camelCase`.
- The ViewModels must not use WPF types.
- The folder is not a git repository, so the plan has no commit steps.

## Commands

- Build: `& "C:\Program Files\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\MSBuild.exe" PandocGUIWrapper.slnx -restore -t:Build -p:Configuration=Debug -v:minimal -nologo`
- Test: `dotnet test PandocGUIWrapper.Tests`

## File structure

| File | Change |
|---|---|
| `PandocGUIWrapper/Models/FormatOption.cs` | Split out of `FormatCatalog.cs`. |
| `PandocGUIWrapper/Models/FormatCatalog.cs` | Add `ReferenceDocumentFormats`, `SupportsReferenceDocument`, `BuildSingleFormatFilter`, regions, and comments. |
| `PandocGUIWrapper/Models/AppSettings.cs` | New. |
| `PandocGUIWrapper/Services/ConversionResult.cs` | Split out of `PandocService.cs`. |
| `PandocGUIWrapper/Services/PandocService.cs`, `IPandocService.cs` | Add a `referenceDocumentPath` parameter. |
| `PandocGUIWrapper/Services/ISettingsService.cs`, `JsonSettingsService.cs` | New. |
| `PandocGUIWrapper/Services/IReferenceDocumentStore.cs`, `ReferenceDocumentStore.cs`, `ReferenceDocumentChangedEventArgs.cs` | New. |
| `PandocGUIWrapper/Services/IDialogService.cs`, `DialogService.cs` | Add regions and comments only. |
| `PandocGUIWrapper/ViewModels/ObservableObject.cs`, `RelayCommand.cs` | Delete. |
| `PandocGUIWrapper/ViewModels/MainViewModel.cs` | Rewrite as the shell that holds the two tabs. |
| `PandocGUIWrapper/ViewModels/ConvertViewModel.cs` | New. Holds the old `MainViewModel` logic plus the reference document behaviour. |
| `PandocGUIWrapper/ViewModels/ReferenceDocumentsViewModel.cs`, `ReferenceDocumentItemViewModel.cs` | New. |
| `PandocGUIWrapper/Views/ConvertView.xaml(.cs)`, `ReferenceDocumentsView.xaml(.cs)` | New. |
| `PandocGUIWrapper/MainWindow.xaml`, `App.xaml.cs` | Add the `TabControl` and update the composition. |
| `PandocGUIWrapper/PandocGUIWrapper.csproj` | Add the two packages and the new files. |
| `PandocGUIWrapper.Tests/*` | Add the packages and links, the new test classes, and the renamed `ConvertViewModelTests`. |

---

### Task 1: Packages and the toolkit migration

- [x] Add `<PackageReference>` for `CommunityToolkit.Mvvm` and `Newtonsoft.Json` to both csproj files. Use the latest stable versions.
- [x] Delete `ViewModels/ObservableObject.cs` and `ViewModels/RelayCommand.cs`. Use `CommunityToolkit.Mvvm.ComponentModel.ObservableObject` and `CommunityToolkit.Mvvm.Input.RelayCommand` / `AsyncRelayCommand` instead. The toolkit's `RelayCommand` has `NotifyCanExecuteChanged()` in place of `RaiseCanExecuteChanged()`.
- [x] Build and test. Expected: the 46 existing tests pass.

### Task 2: Models and the settings service

**Produces:**
- `FormatCatalog.ReferenceDocumentFormats` (`IReadOnlyList<string>`: `docx`, `odt`, `pptx`)
- `FormatCatalog.SupportsReferenceDocument(string format)`: returns `bool` and ignores case. It returns `false` for null.
- `FormatCatalog.BuildSingleFormatFilter(string format)`: returns, for example, `"Word (*.docx)|*.docx"`.
- `AppSettings { Dictionary<string, string> ReferenceDocuments }`: the dictionary ignores case and is never null after `Load`.
- `ISettingsService { AppSettings Load(); void Save(AppSettings settings); }`
- `JsonSettingsService(string filePath)` and `JsonSettingsService.DefaultFilePath`. `Save` creates the folder.

**Tests:**
- `FormatCatalogTests`:
  - `SupportsReferenceDocument` returns true for docx, ODT, and pptx, and false for html and null.
  - `BuildSingleFormatFilter("docx")` returns `"Word (*.docx)|*.docx"`.
- `JsonSettingsServiceTests`:
  - A save and load returns the same values, and the folder is created.
  - A missing file gives empty settings.
  - A corrupt file gives empty settings.
  - A JSON `null` for the dictionary gives an empty dictionary.
  - The loaded dictionary ignores case.

### Task 3: Reference document store and the pandoc argument

**Produces:**
- `IReferenceDocumentStore`:
  - `string GetPath(string format)`: returns `null` when no path is set.
  - `void SetPath(string format, string path)`
  - `void ClearPath(string format)`
  - `event EventHandler<ReferenceDocumentChangedEventArgs> Changed`
- `ReferenceDocumentChangedEventArgs { string Format }`
- `ReferenceDocumentStore(ISettingsService settings)`:
  - Loads the settings once.
  - `SetPath` and `ClearPath` save the settings, then raise `Changed`. A save failure throws `IOException` or `UnauthorizedAccessException`, and the ViewModel catches it.
  - Throws `ArgumentException` when the format or path is blank.
- `IPandocService.ConvertAsync(string inputPath, string outputPath, string fromFormat, string toFormat, string referenceDocumentPath)`
- `PandocService.BuildConvertArguments(..., string referenceDocumentPath)`: adds ` --reference-doc "<path>"` after `-t`.

**Tests:**
- `ReferenceDocumentStoreTests`, using a fake `ISettingsService`:
  - `GetPath` returns the loaded values and ignores case.
  - `SetPath` saves and raises `Changed`.
  - `ClearPath` removes the path, saves, and raises `Changed`.
  - `SetPath` with a blank format or path throws.
- `PandocServiceTests`:
  - The arguments contain `--reference-doc` when a path is given, and do not contain it otherwise.
  - An integration test converts md to docx with a reference document. Create the reference document with `pandoc -o ref.docx --print-default-data-file reference.docx`.

### Task 4: ViewModels

**Produces:**
- `MainViewModel(ConvertViewModel convert, ReferenceDocumentsViewModel referenceDocuments, IPandocService pandoc /*nullable*/, IDialogService dialogs)`:
  - Properties: `Convert` and `ReferenceDocuments`.
  - `Task InitializeAsync()`: shows the pandoc-missing and format-load errors, then calls `Convert.Load(inputFormats, outputFormats)` and `ReferenceDocuments.Load(outputFormats)`. On failure it calls `Convert.SetStatus(text)`.
- `ConvertViewModel(IPandocService pandoc, IDialogService dialogs, IReferenceDocumentStore store)`:
  - Keeps every existing member.
  - `Load(IReadOnlyList<string> inputFormats, IReadOnlyList<string> outputFormats)`
  - `SetStatus(string status)`
  - `bool IsReferenceDocumentSupported`
  - `string ReferenceDocumentPath`
  - `string ReferenceDocumentText`
  - `ConvertCommand` is an `AsyncRelayCommand`.
- `ReferenceDocumentsViewModel(IReferenceDocumentStore store, IDialogService dialogs)`:
  - `ObservableCollection<ReferenceDocumentItemViewModel> Items`
  - `void Load(IEnumerable<string> outputFormats)`
- `ReferenceDocumentItemViewModel(string format, IReferenceDocumentStore store, IDialogService dialogs)`:
  - Properties: `Format`, `DisplayName`, `Path`, and `HasPath`.
  - Commands: `BrowseCommand` and `ClearCommand`. `ClearCommand` can run only when `HasPath` is true.
  - The item updates itself on `store.Changed`.
  - If a save fails, it shows a warning that the setting could not be saved.

**Tests:**
- `MainViewModelTests`:
  - Missing pandoc: error message and status, and Convert stays disabled.
  - Format-load failure: error message.
  - Success: both tabs are loaded.
- `ConvertViewModelTests` (moved from the old `MainViewModelTests`, which used `vm.InitializeAsync()`; the tests now call `Load`):
  - All the existing tests, ported.
  - docx with a stored path: the path is passed to pandoc, and the text shows the file name.
  - html: `IsReferenceDocumentSupported` is false, and `null` is passed to pandoc.
  - The text updates when the store changes.
  - Stored file missing:
    - "Yes" converts with a `null` reference document.
    - "No" does not call pandoc.
- `ReferenceDocumentsViewModelTests`:
  - Only docx, odt, and pptx that pandoc lists appear, in that order.
  - An item shows the stored path.
  - Browse sets the path in the store.
  - A cancelled Browse keeps the old path.
  - Clear removes the path.
  - `ClearCommand.CanExecute` is false when no path is set.
  - A save failure shows a warning.

### Task 5: Views and composition

- [x] `Views/ConvertView.xaml` contains the existing grid, plus a "Reference document:" row bound to `ReferenceDocumentText`. The row's tooltip is bound to `ReferenceDocumentPath`. The row is visible only when `IsReferenceDocumentSupported` is true, using `BooleanToVisibilityConverter`.
- [x] `Views/ReferenceDocumentsView.xaml` contains a short explanation and an `ItemsControl`. Each row has the format name, a read-only path TextBox ("None (pandoc default)" when empty), Browse…, and Clear.
- [x] `MainWindow.xaml` contains a `TabControl` with "Convert" (`DataContext={Binding Convert}`) and "Reference documents" (`DataContext={Binding ReferenceDocuments}`).
- [x] `App.OnStartup` creates `JsonSettingsService(DefaultFilePath)`, `ReferenceDocumentStore`, the services, and the ViewModels.
- [x] Build, run the tests, and launch the app to check that both tabs render, using UI Automation.

## As built

- All tasks done. 93 tests pass; the solution builds with no warnings; both tabs were checked with UI Automation.
- `ConvertViewModel` keeps the version 1 names `Status`, `SelectedOutputFormat`, `IsIdle`, and a public `ConvertAsync()`, so the XAML and the ported tests did not change.
- Clearing the output name or folder resets edit tracking, so the next input file fills in the default again.
- If saving the settings fails, `ReferenceDocumentStore` still raises `Changed` (the in-memory value changed) and then throws; the item ViewModel shows a warning.
- Test fakes are shared in `PandocGUIWrapper.Tests\Fakes.cs`. The test project links all `Services\*.cs` files except `DialogService.cs`.
