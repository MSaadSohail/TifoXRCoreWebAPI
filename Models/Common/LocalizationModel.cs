// <copyright file="localizationModel.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/09/2025</date>
// <summary>Data for Localization </summary>

namespace GMS.TifoXRCoreWebAPI.Models.Common
{
    /// <summary>
    /// Holds a resource key plus all its localized values.
    /// </summary>
    public class LocalizedPairs
    {
        /// <summary>
        /// The i18n key (e.g. "bth_nm_101").
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// All the localized text for that key.
        /// </summary>
        public List<LocalizedValue> Values { get; set; }
    }

    /// <summary>
    /// A single locale/value pair (e.g. "en_us" => "Main Hall Booth").
    /// </summary>
    public class LocalizedValue
    {
        /// <summary>
        /// Locale identifier (matches i18n.locale_id).
        /// </summary>
        public string LocaleId { get; set; }

        /// <summary>
        /// The localized string.
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// Holds a resource key and its localized string values.
    /// </summary>
    public class LocalizedResource
    {
        /// <summary>
        /// The i18n key (e.g. "bth_nm_101").
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// All the localized values for that key (locale → value).
        /// </summary>
        public Dictionary<string, string> Localizations { get; set; } = [];
    }
}