// <copyright file="IBoothActivityRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Interface layer for booth repository</summary>

using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public interface IBoothRepository
    {
        Task<List<BoothModel>> GetAllBoothsBySpaceAsync(int spaceId);
        Task<BoothModel?> UpdateBoothAsync(int spaceId, int boothId, BoothUpdateDto dto);
        Task<BoothModel> CreateBoothAsync(int spaceId, BoothCreateDto boothDto);
        Task<MediaData?> AddMediaToBoothAsync(int spaceId, int boothId, MediaCreateDto dto);
        Task<bool> DeleteBoothMediaAsync(int spaceId, int boothId, string mediaId);
        Task<bool> DeleteBoothCascadeAsync(int spaceId, int boothId);
    }
}
