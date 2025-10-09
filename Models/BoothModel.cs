// <copyright file="BoothModel.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Data for Booth </summary>
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Models
{
    public class BoothCreateDto
    {
        public int SpaceId { get; set; }
        public LocalizedPairs LocalizedPairs { get; set; }
        public int MapSpotId { get; set; }

        //public MapSpotModel MapSpot { get; set; }

        public List<MediaCreateDto>? MediaItems { get; set; }

    }

    public class BoothWrapper
    {
        public BoothModel booth { get; set; }
    }

    public class BoothModel
    {
        public int Id { get; set; }
        public int SpaceId { get; set; }
        public int MapSpotId { get; set; }
        //public MapSpotModel? MapSpot { get; set; }
        public LocalizedPairs LocalizedPairs { get; set; }
        public List<MediaData> MediaItems { get; set; } = new();
    }
    public class MapSpotModel
    {
        public decimal X { get; set; }
        public decimal Y { get; set; }
        public decimal Z { get; set; }
    }

    public class BoothLocalizedPairsUpdateDto
    {
        public List<LocalizedValue>? Values { get; set; }
    }

    public class BoothMediaUpdateDto
    {
        public int MediaTypeId { get; set; }
        public string? TextKey { get; set; }
        public string? DescriptionKey { get; set; }
        public List<MediaLocalization>? LinkLocalizations { get; set; }
    }

    public class BoothUpdateDto
    {
        public BoothLocalizedPairsUpdateDto? LocalizedPairs { get; set; }
        public int MapSpotId { get; set; }
        public List<BoothMediaUpdateDto>? MediaItems { get; set; }

    }

    public class Response
    {
        public BoothModel Booth { get; set; }
    }

    /// <summary>
    /// Client provides these fields.
    /// </summary>
    public class BoothActivity
    {
        public int BoothId { get; set; }
        public string UserId { get; set; }
        public int ExitCode { get; set; }
        public string BoothNameKey { get; set; }
        public string SessionId { get; set; }
        public DateTime EntryDatetime { get; set; }
        public int SessionDuration { get; set; }
        public DateTime? ExitDatetime { get; set; }
    }
}
