// <copyright file="PortalUpdateDtoBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>07/29/2025</date>
// <summary>Fluent builder for PortalUpdateDto used in unit tests to simplify setup of valid and edge-case DTOs.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for PortalUpdateDto to simplify and standardize DTO creation in unit tests.
    /// Allows chaining of overrides for link and localization fields.
    /// </summary>
    public class PortalUpdateDtoBuilder
    {
        private string _externalLink = "https://example.com";
        private LocalizedPairs _localizedPairs = new LocalizedPairs
        {
            Key = "portal.name",
            Values = new List<LocalizedValue>
            {
                new LocalizedValue { LocaleId = "en_us", Value = "Updated Portal" }
            }
        };
        private MediaUpdateDto? _corrMedia;
        private MediaUpdateDto? _thumbMedia;

        /// <summary>
        /// Sets the ExternalLink for the DTO.
        /// </summary>
        public PortalUpdateDtoBuilder WithLink(string link)
        {
            _externalLink = link;
            return this;
        }

        /// <summary>
        /// Sets a single localization for the specified locale.
        /// </summary>
        public PortalUpdateDtoBuilder WithLocalization(string locale, string text)
        {
            // replace entire list for this locale
            _localizedPairs.Values.RemoveAll(v => v.LocaleId == locale);
            _localizedPairs.Values.Add(new LocalizedValue { LocaleId = locale, Value = text });
            return this;
        }

        /// <summary>
        /// Sets the entire LocalizedPairs object for custom scenarios.
        /// </summary>
        public PortalUpdateDtoBuilder WithLocalizedPairs(LocalizedPairs pairs)
        {
            _localizedPairs = pairs;
            return this;
        }

        /// <summary>
        /// Builds and returns the configured PortalUpdateDto.
        /// </summary>
        public PortalUpdateDto Build() =>
            new PortalUpdateDto
            {
                ExternalLink = _externalLink,
                LocalizedPairs = _localizedPairs,
                CorrespondingMedia = _corrMedia,
                ThumbnailMedia = _thumbMedia
            };
    }
}
