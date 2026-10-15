using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using PandocGUIWrapper.Models;

namespace PandocGUIWrapper.Services
{
    /// <summary>
    /// Stores <see cref="AppSettings"/> as an indented JSON file, using Newtonsoft.Json.
    /// </summary>
    public sealed class JsonSettingsService : ISettingsService
    {
        #region Fields

        /// <summary>The full path of the settings file.</summary>
        private readonly string _filePath;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSettingsService"/> class.
        /// </summary>
        /// <param name="filePath">The full path of the settings file.</param>
        /// <exception cref="ArgumentException"><paramref name="filePath"/> is null or blank.</exception>
        public JsonSettingsService(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("A settings file path is required.", nameof(filePath));
            }

            _filePath = filePath;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the default settings file path: <c>%AppData%\PandocGUIWrapper\settings.json</c>.
        /// </summary>
        public static string DefaultFilePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PandocGUIWrapper", "settings.json");

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public AppSettings Load()
        {
            if (!File.Exists(_filePath))
            {
                return new AppSettings();
            }

            try
            {
                string json = File.ReadAllText(_filePath, Encoding.UTF8);
                return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            catch (Exception ex) when (ex is JsonException || ex is IOException || ex is UnauthorizedAccessException)
            {
                // A damaged or locked settings file must not stop the app from starting.
                return new AppSettings();
            }
        }

        /// <inheritdoc />
        public void Save(AppSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(_filePath, json, Encoding.UTF8);
        }

        #endregion
    }
}
