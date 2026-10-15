using System;

namespace PandocGUIWrapper.Services
{
    /// <summary>
    /// Holds the reference document configured for each output format and saves every change.
    /// </summary>
    public interface IReferenceDocumentStore
    {
        #region Events

        /// <summary>
        /// Occurs after the reference document for a format is set or cleared.
        /// </summary>
        event EventHandler<ReferenceDocumentChangedEventArgs> Changed;

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the reference document path for a format.
        /// </summary>
        /// <param name="format">The output format. The lookup ignores case.</param>
        /// <returns>The configured path, or <c>null</c> when none is configured.</returns>
        string GetPath(string format);

        /// <summary>
        /// Sets and saves the reference document path for a format.
        /// </summary>
        /// <param name="format">The output format.</param>
        /// <param name="path">The full path of the reference document.</param>
        /// <exception cref="ArgumentException"><paramref name="format"/> or <paramref name="path"/> is blank.</exception>
        /// <exception cref="System.IO.IOException">The settings cannot be saved.</exception>
        /// <exception cref="UnauthorizedAccessException">The settings file cannot be written.</exception>
        void SetPath(string format, string path);

        /// <summary>
        /// Removes and saves the reference document for a format.
        /// </summary>
        /// <param name="format">The output format.</param>
        /// <exception cref="ArgumentException"><paramref name="format"/> is blank.</exception>
        /// <exception cref="System.IO.IOException">The settings cannot be saved.</exception>
        /// <exception cref="UnauthorizedAccessException">The settings file cannot be written.</exception>
        void ClearPath(string format);

        #endregion
    }
}
