using System;

namespace PandocGUIWrapper.Services
{
    /// <summary>
    /// Event data for <see cref="IReferenceDocumentStore.Changed"/>.
    /// </summary>
    public sealed class ReferenceDocumentChangedEventArgs : EventArgs
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDocumentChangedEventArgs"/> class.
        /// </summary>
        /// <param name="format">The output format whose reference document changed.</param>
        public ReferenceDocumentChangedEventArgs(string format)
        {
            Format = format;
        }

        #endregion

        #region Public Properties

        /// <summary>Gets the output format whose reference document changed.</summary>
        public string Format { get; }

        #endregion
    }
}
