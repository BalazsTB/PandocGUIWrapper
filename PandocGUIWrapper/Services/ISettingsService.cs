using PandocGUIWrapper.Models;

namespace PandocGUIWrapper.Services
{
    /// <summary>
    /// Loads and saves the user settings.
    /// </summary>
    public interface ISettingsService
    {
        #region Public Methods

        /// <summary>
        /// Loads the settings.
        /// </summary>
        /// <returns>The saved settings, or empty settings when the file is missing or cannot be read.</returns>
        AppSettings Load();

        /// <summary>
        /// Saves the settings.
        /// </summary>
        /// <param name="settings">The settings to save.</param>
        /// <exception cref="System.IO.IOException">The file cannot be written.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Access to the file is denied.</exception>
        void Save(AppSettings settings);

        #endregion
    }
}
