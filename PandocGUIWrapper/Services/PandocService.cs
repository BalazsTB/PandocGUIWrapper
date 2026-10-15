using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandocGUIWrapper.Services
{
    /// <summary>
    /// The <see cref="IPandocService"/> that starts <c>pandoc.exe</c> as a hidden process.
    /// </summary>
    public sealed class PandocService : IPandocService
    {
        #region Constants

        /// <summary>The pandoc executable file name.</summary>
        private const string PandocFileName = "pandoc.exe";

        #endregion

        #region Fields

        /// <summary>The full path of <c>pandoc.exe</c>.</summary>
        private readonly string _pandocPath;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PandocService"/> class.
        /// </summary>
        /// <param name="pandocPath">The full path of <c>pandoc.exe</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="pandocPath"/> is null.</exception>
        public PandocService(string pandocPath)
        {
            _pandocPath = pandocPath ?? throw new ArgumentNullException(nameof(pandocPath));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Finds <c>pandoc.exe</c> in the folders listed in the <c>PATH</c> environment variable.
        /// </summary>
        /// <returns>The full path of <c>pandoc.exe</c>, or <c>null</c> when it is not found.</returns>
        public static string FindPandoc()
        {
            string pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (string directory in pathVariable.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    string candidate = Path.Combine(directory.Trim().Trim('"'), PandocFileName);
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

        /// <summary>
        /// Builds the pandoc command-line arguments for one conversion.
        /// </summary>
        /// <param name="inputPath">The file to convert.</param>
        /// <param name="outputPath">The file to write.</param>
        /// <param name="fromFormat">The input format, or <c>null</c> to omit <c>-f</c>.</param>
        /// <param name="toFormat">The output format.</param>
        /// <param name="referenceDocumentPath">The reference document, or <c>null</c> to omit <c>--reference-doc</c>.</param>
        /// <returns>The argument string.</returns>
        public static string BuildConvertArguments(string inputPath, string outputPath, string fromFormat, string toFormat, string referenceDocumentPath)
        {
            var builder = new StringBuilder();
            builder.Append(Quote(inputPath));
            if (!string.IsNullOrEmpty(fromFormat))
            {
                builder.Append(" -f ").Append(fromFormat);
            }

            builder.Append(" -t ").Append(toFormat);
            if (!string.IsNullOrEmpty(referenceDocumentPath))
            {
                builder.Append(" --reference-doc ").Append(Quote(referenceDocumentPath));
            }

            builder.Append(" -o ").Append(Quote(outputPath));
            return builder.ToString();
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<string>> GetInputFormatsAsync() => ListFormatsAsync("--list-input-formats");

        /// <inheritdoc />
        public Task<IReadOnlyList<string>> GetOutputFormatsAsync() => ListFormatsAsync("--list-output-formats");

        /// <inheritdoc />
        public async Task<ConversionResult> ConvertAsync(string inputPath, string outputPath, string fromFormat, string toFormat, string referenceDocumentPath)
        {
            try
            {
                string arguments = BuildConvertArguments(inputPath, outputPath, fromFormat, toFormat, referenceDocumentPath);
                ProcessOutput output = await RunAsync(arguments).ConfigureAwait(false);
                return new ConversionResult(output.ExitCode == 0, output.ExitCode, output.StandardError.Trim());
            }
            catch (Exception ex)
            {
                return new ConversionResult(false, -1, "Could not run pandoc: " + ex.Message);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Runs a pandoc list command and splits its output into format names.
        /// </summary>
        /// <param name="argument">The list option, for example <c>--list-output-formats</c>.</param>
        /// <returns>The format names.</returns>
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

        /// <summary>
        /// Starts pandoc without a window and collects its output.
        /// </summary>
        /// <param name="arguments">The command-line arguments.</param>
        /// <returns>The exit code, standard output, and standard error.</returns>
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
                    // Read standard error at the same time as standard output so a full buffer cannot block pandoc.
                    Task<string> errorTask = process.StandardError.ReadToEndAsync();
                    string standardOutput = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return new ProcessOutput(process.ExitCode, standardOutput, errorTask.Result);
                }
            });
        }

        /// <summary>
        /// Puts a path in double quotes. Windows paths cannot contain <c>"</c>, so no escaping is needed.
        /// </summary>
        /// <param name="value">The path to quote.</param>
        /// <returns>The quoted path.</returns>
        private static string Quote(string value) => "\"" + value + "\"";

        #endregion

        /// <summary>
        /// The collected output of one pandoc process.
        /// </summary>
        private sealed class ProcessOutput
        {
            #region Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="ProcessOutput"/> class.
            /// </summary>
            /// <param name="exitCode">The process exit code.</param>
            /// <param name="standardOutput">The text written to standard output.</param>
            /// <param name="standardError">The text written to standard error.</param>
            public ProcessOutput(int exitCode, string standardOutput, string standardError)
            {
                ExitCode = exitCode;
                StandardOutput = standardOutput;
                StandardError = standardError;
            }

            #endregion

            #region Public Properties

            /// <summary>Gets the process exit code.</summary>
            public int ExitCode { get; }

            /// <summary>Gets the text written to standard output.</summary>
            public string StandardOutput { get; }

            /// <summary>Gets the text written to standard error.</summary>
            public string StandardError { get; }

            #endregion
        }
    }
}
