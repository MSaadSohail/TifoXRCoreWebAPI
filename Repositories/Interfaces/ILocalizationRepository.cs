// <copyright file="ILocalizationRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/31/2025</date>
// <summary>Interface to handle localization repository pattern</summary>

using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public interface ILocalizationRepository
    {
        Task<List<LocalizedPairs>> GetAllLocalizationsBySpaceAsync(int spaceId);
        Task<LocalizedPairs?> GetLocalizationByKeyAsync(int spaceId, string key);
        Task<LocalizedPairs> CreateLocalizationAsync(int spaceId, LocalizedPairs dto);
        Task<LocalizedPairs?> UpdateLocalizationAsync(int spaceId, string key, LocalizedPairs dto);
        Task<bool> DeleteLocalizationAsync(int spaceId, string key);
    }
}