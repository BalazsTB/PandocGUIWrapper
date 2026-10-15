using System;
using PandocGUIWrapper.Models;

namespace PandocGUIWrapper.Services
{
    /// <summary>
    /// The <see cref="IReferenceDocumentStore"/> that keeps the settings in memory
    /// and saves them through <see cref="ISettingsService"/> after every change.
    /// </summary>
    public sealed class ReferenceDocumentStore : IReferenceDocumentStore
    {
        #region Fields

        /// <summary>The service that saves the settings.</summary>
        private readonly ISettingsService _settingsService;

        /// <summary>The settings loaded at startup and changed in memory.</summary>
        private readonly AppSettings _settings;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDocumentStore"/> class and loads the settings.
        /// </summary>
        /// <param name="settingsService">The service that loads and saves the settings.</param>
        /// <exception cref="ArgumentNullException"><paramref name="settingsService"/> is null.</exception>
        public ReferenceDocumentStore(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _settings = _settingsService.Load() ?? new AppSettings();
        }

        #endregion

        #region Events

        /// <inheritdoc />
        public event EventHandler<ReferenceDocumentChangedEventArgs> Changed;

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public string GetPath(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return null;
            }

            string path;
            return _settings.ReferenceDocuments.TryGetValue(format, out path) ? path : null;
        }

        /// <inheritdoc />
        public void SetPath(string format, string path)
        {
            ValidateFormat(format);
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A reference document path is required.", nameof(path));
            }

            _settings.ReferenceDocuments[format] = path;
            SaveAndNotify(format);
        }

        /// <inheritdoc />
        public void ClearPath(string format)
        {
            ValidateFormat(format);
            if (!_settings.ReferenceDocuments.Remove(format))
            {
                return;
            }

            SaveAndNotify(format);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Throws when the format name is blank.
        /// </summary>
        /// <param name="format">The format name to check.</param>
        private static void ValidateFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentException("A format is required.", nameof(format));
            }
        }

        /// <summary>
        /// Saves the settings and raises <see cref="Changed"/>.
        /// </summary>
        /// <param name="format">The format that changed.</param>
        private void SaveAndNotify(string format)
        {
            try
            {
                _settingsService.Save(_settings);
            }
            finally
            {
                // The in-memory value has changed even if saving fails, so the UI must still update.
                Changed?.Invoke(this, new ReferenceDocumentChangedEventArgs(format));
            }
        }

        #endregion
    }
}
