# Reference Documents — Design

## Goal

Add a second tab where the user sets one reference document for each format that pandoc supports with `--reference-doc`. When the user converts to one of those formats, the app uses the reference document set for that format.

## Decisions

- Formats with reference documents: `docx`, `odt`, and `pptx`. A format appears only if the installed pandoc lists it as an output format.
- There is one reference document per format. The app applies it automatically.
- Settings are saved in `%AppData%\PandocGUIWrapper\settings.json` with Newtonsoft.Json. Every change saves at once. There is no Save button.
- A settings file that is missing or corrupt loads as empty settings. The app does not crash.
- The Convert tab shows the reference document as read-only text, for example "corp.docx" or "None (pandoc default)". The tooltip shows the full path. The row is hidden for formats that do not support reference documents.
- If the reference document no longer exists when the user clicks Convert, the app asks: "Convert without it?" Yes converts without `--reference-doc`. No cancels the conversion.
- MVVM uses CommunityToolkit.Mvvm base classes only: `ObservableObject`, `RelayCommand`, and `AsyncRelayCommand`. It does not use source generators. The language version stays at C# 7.3. The hand-written `ObservableObject` and `RelayCommand` are deleted.
- Code style follows the user's codingconventions skill:
  - XML doc comments on types and members.
  - `#region` blocks in every class, using the standard names and order.
  - Inline comments only where the intent is not clear from the code.

## Architecture

| Layer | Type | Responsibility |
|---|---|---|
| Model | `FormatCatalog.SupportsReferenceDocument(format)` and `FormatCatalog.ReferenceDocumentFormats` | Says which formats accept `--reference-doc`. |
| Model | `AppSettings` | Holds `Dictionary<string, string> ReferenceDocuments` (format → path). This is the data saved to JSON. |
| Service | `ISettingsService` / `JsonSettingsService` | `Load()` and `Save(AppSettings)`. The file path is passed in through the constructor so tests can use a temp file. |
| Service | `IReferenceDocumentStore` / `ReferenceDocumentStore` | `GetPath(format)`, `SetPath(format, path)`, and `ClearPath(format)`. It saves through `ISettingsService` and raises a `Changed` event with the format. |
| Service | `IPandocService.ConvertAsync(..., referenceDocumentPath)` | Adds `--reference-doc "<path>"` when the path is not empty. |
| Service | `IDialogService.ShowOpenFileDialog` | Used again with a filter for one format, such as `Word (*.docx)\|*.docx`. |
| ViewModel | `MainViewModel` | Holds `Convert` (`ConvertViewModel`) and `ReferenceDocuments` (`ReferenceDocumentsViewModel`). `InitializeAsync()` loads the pandoc formats once and gives them to both tabs. |
| ViewModel | `ConvertViewModel` | Holds the existing convert behaviour, plus `IsReferenceDocumentSupported`, `ReferenceDocumentText`, and `ReferenceDocumentPath`. |
| ViewModel | `ReferenceDocumentsViewModel` | Holds `ObservableCollection<ReferenceDocumentItemViewModel> Items`. |
| ViewModel | `ReferenceDocumentItemViewModel` | Holds `Format`, `DisplayName`, `Path`, `HasPath`, `BrowseCommand`, and `ClearCommand`. |
| View | `MainWindow` | Holds a `TabControl` with the tabs "Convert" and "Reference documents". |
| View | `Views/ConvertView`, `Views/ReferenceDocumentsView` | `UserControl`s with bindings only. |
| Composition | `App.OnStartup` | Creates and connects all the services and view models. |

## Data flow

1. At startup, `App` creates the services and the view models, then calls `MainViewModel.InitializeAsync()`. This method gets the formats from pandoc and calls `ConvertViewModel.Load(inputFormats, outputFormats)` and `ReferenceDocumentsViewModel.Load(outputFormats)`.
2. On the Reference documents tab, Browse… opens a file dialog filtered to the format's extension. The selected path goes to `store.SetPath`, which saves the settings and raises `Changed`. Clear calls `store.ClearPath`.
3. `ConvertViewModel` subscribes to `store.Changed`. It updates the reference document text when the selected output format changes and when the store changes.
4. On Convert, the app gets the path from the store. If the path is set but the file is missing, the app asks the user. Then it calls `ConvertAsync` with the path, or with `null`.

## Error handling

- The settings file cannot be read or parsed: `Load` returns empty settings.
- The settings file cannot be written: the store reports the error through `IDialogService.ShowMessage`, and the in-memory value is still set.
- The reference document is missing at Convert: the app asks the user, as described above.

## Testing (MSTest, net48)

- `JsonSettingsServiceTests`:
  - A save and load returns the same values.
  - A missing file gives empty settings.
  - A corrupt file gives empty settings.
- `ReferenceDocumentStoreTests`:
  - `SetPath`, `GetPath`, and `ClearPath` work, save the settings, and raise `Changed`.
  - Format names are not case-sensitive.
- `PandocServiceTests`:
  - The arguments contain `--reference-doc` when a path is given, and do not contain it otherwise.
  - An integration test converts to docx with a reference document.
- `ConvertViewModelTests` (moved from `MainViewModelTests`):
  - All the existing behaviour.
  - The reference document is passed to pandoc.
  - The reference document text updates.
  - The row is hidden for html.
  - The missing-file prompt converts without the document on Yes and cancels on No.
- `ReferenceDocumentsViewModelTests`:
  - Only the supported formats that pandoc lists appear.
  - Browse sets and saves the path.
  - A cancelled Browse keeps the old path.
  - Clear removes the path.
