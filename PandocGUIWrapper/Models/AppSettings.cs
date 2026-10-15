using System;
using System.Collections.Generic;

namespace PandocGUIWrapper.Models
{
    /// <summary>
    /// The user settings that are saved to <c>settings.json</c>.
    /// </summary>
    public sealed class AppSettings
    {
        #region Fields

        /// <summary>Backing field for <see cref="ReferenceDocuments"/>.</summary>
        private Dictionary<string, string> _referenceDocuments = CreateReferenceDocumentMap();

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the reference document path for each output format (format → path).
        /// The keys ignore case. Setting <c>null</c> stores an empty map.
        /// </summary>
        public Dictionary<string, string> ReferenceDocuments
        {
            get => _referenceDocuments;
            set
            {
                // Copy into a case-insensitive map, because JSON deserialization creates a case-sensitive one.
                Dictionary<string, string> map = CreateReferenceDocumentMap();
                if (value != null)
                {
                    foreach (KeyValuePair<string, string> pair in value)
                    {
                        if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                        {
                            map[pair.Key] = pair.Value;
                        }
                    }
                }

                _referenceDocuments = map;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates an empty format → path map whose keys ignore case.
        /// </summary>
        /// <returns>A new, empty map.</returns>
        private static Dictionary<string, string> CreateReferenceDocumentMap()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        #endregion
    }
}
