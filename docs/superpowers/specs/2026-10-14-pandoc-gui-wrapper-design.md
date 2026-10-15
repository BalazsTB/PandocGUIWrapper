# Pandoc GUI Wrapper — Design

## Goal

A small WPF (.NET Framework 4.8) desktop app that converts one document with pandoc. The user picks an input file, an output name, an output format, and an output folder, then clicks Convert.

## Decisions

- pandoc is found on `PATH`. If it is not found, the app shows a clear error at startup and disables Convert.
- The input and output format lists come from pandoc at startup (`--list-input-formats`, `--list-output-formats`). They are mapped to friendly names and file extensions.
- If the output file already exists, the user must confirm the overwrite.
- When conversion succeeds, the app shows a message with an "Open folder" button.
- Scope is minimal: four fields and a Convert button. There is no reference-doc field and no field for extra arguments.

## Architecture

The app uses MVVM. The ViewModel has no WPF dependencies, so it can be unit-tested with fake services.

| Layer | Type | Responsibility |
|---|---|---|
| Model | `Models/FormatCatalog` (+ `FormatOption`) | Maps pandoc format names to friendly names and extensions. |
| Service | `Services/IPandocService`, `PandocService` (+ `ConversionResult`) | Finds pandoc on `PATH`, lists the formats, and runs the conversion. |
| Service | `Services/IDialogService`, `DialogService` (+ `DialogKind`) | Shows the Open-file dialog, the folder picker, message boxes, and Yes/No prompts, and opens the output in Explorer. |
| ViewModel | `ViewModels/ObservableObject`, `RelayCommand`, `MainViewModel` | Holds all the UI state and behaviour. |
| View | `MainWindow.xaml` | Contains bindings only. The code-behind calls only `InitializeComponent`. |
| Composition | `App.OnStartup` | Creates the services and the ViewModel, shows the window, and calls `InitializeAsync`. |

### `FormatCatalog` (static)
- Holds a static map from each pandoc format name to a friendly name and a default extension. For example, `docx` maps to "Word" and `.docx`, and `markdown` maps to "Pandoc Markdown" and `.md`.
- `GetExtension(string format)`: returns the mapped extension. For a format that is not mapped, it returns `"." + format`.
- `GetDisplayName(string format)`: returns `"Friendly (.ext)"`. For a format that is not mapped, it returns the raw format name.
- `BuildOpenFilter(IEnumerable<string> inputFormats)`: returns a filter string. The first entry is "All supported files". Then there is one entry for each mapped input format that pandoc lists. The last entry is "All files (*.*)".
- `GuessInputFormat(string path)`: maps a file extension to a pandoc input format. It returns `null` when the extension is unknown. In that case, pandoc uses its own detection.
- `CreateOptions(IEnumerable<string> formats)`: returns a list of `FormatOption`, sorted by display name.

### `IPandocService` / `PandocService`
- `static string FindPandoc()`: returns the full path of `pandoc.exe` found on `PATH`, or `null`.
- `GetInputFormatsAsync()` and `GetOutputFormatsAsync()`: return one format per line from pandoc.
- `ConvertAsync(input, output, fromFormat /*nullable*/, toFormat)`: runs `pandoc "<input>" [-f from] -t to -o "<output>"` with no window. It returns `ConversionResult` (`Success`, `ExitCode`, `ErrorText`). This method never throws.

### `IDialogService`
- `ShowOpenFileDialog(title, filter, initialDirectory)` and `ShowFolderDialog(description, initialPath)`: return the selected path, or `null` when the user cancels. The folder picker uses `System.Windows.Forms.FolderBrowserDialog`.
- `ShowMessage(message, title, kind)` shows a message. `Confirm(message, title, kind)` asks a Yes/No question and returns `true` for Yes.
- `ShowInExplorer(filePath)`: runs `explorer.exe /select,"<file>"`.

### `MainViewModel`
- Properties:
  - `InputPath` (read-only for the view)
  - `OutputName`
  - `OutputFormats`
  - `SelectedOutputFormat`
  - `OutputFolder`
  - `Status`
  - `IsBusy` and `IsIdle`
- Commands:
  - `BrowseInputCommand`
  - `BrowseOutputFolderCommand`
  - `ConvertCommand`. Its `CanExecute` uses the same enable rule as the Convert button.
- `InitializeAsync()` loads the formats. `ConvertAsync()` runs the conversion and is public so that tests can call it.
- The constructor takes `IPandocService`, which is `null` when pandoc was not found, and `IDialogService`.

### `MainWindow`
The layout is a single grid with these rows, from top to bottom:
1. Input file: a read-only TextBox and a "Browse…" button.
2. Output name.
3. Output format: a ComboBox. The default is `docx` if pandoc supports it. Otherwise the default is the first format.
4. Output folder: a TextBox and a "Browse…" button.
5. A Convert button and a status TextBlock.
## Behaviour and data flow

- When the user selects an input file:
  - The output name is set to the input file name without its extension, unless the user has edited the name.
  - The output folder is set to the input folder, unless the user has edited the folder.
- The final output path is `Path.Combine(folder, name + FormatCatalog.GetExtension(selectedFormat))`.
- Convert is enabled only when all of these are true:
  - pandoc was found.
  - The input file exists.
  - The name is not empty.
  - A format is selected.
  - The folder exists.
- On Convert:
  - If the output file exists, the app asks the user to confirm the overwrite. If the user selects No, the app stops.
  - If the output path is the same as the input path, the app blocks the conversion and shows an error.
  - While pandoc runs, the app disables the UI and the status shows "Converting…".
  - On success, the app shows a `MessageBox` with the text "Converted to <output>. Open folder?" and the buttons Yes and No. Yes runs `explorer.exe /select,"<output>"`.
  - On failure, the app shows the stderr text from pandoc in an error dialog. The status shows "Failed".

## Error handling

- pandoc is missing: the app shows an error dialog at startup, disables Convert, and the status shows "pandoc not found on PATH".
- The format list cannot be read: the app shows an error and disables Convert.
- The conversion fails: the app shows the stderr text as it is. For example, PDF output fails when no LaTeX engine is installed.

## Testing

- The solution builds with MSBuild.
- Unit tests (MSTest, net48) cover `FormatCatalog`, `PandocService` (integration tests against the real pandoc), and `MainViewModel` (with fake `IPandocService` and `IDialogService`).
- Manual smoke test:
  - Convert a sample `.md` file to `.docx` and to `.html`.
  - Confirm that the default values fill in.
  - Confirm that the overwrite prompt appears.
  - Confirm that "Open folder" works.
  - Confirm that a forced failure shows an error, such as PDF output without LaTeX.
