// <copyright file="ICharityRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>12/08/2025</date>
// <summary>Interface Charity Repository</summary>
using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface ICharityRepository
    {
        Task<CharityData> CreateCharityAsync(CharityCreateDto dto);
        Task<CharityData?> GetCharityByIdAsync(int id, int spaceId);
        Task<List<CharityData>> GetCharitiesBySpaceAsync(int spaceId);

        Task<CharityData> UpdateCharityAsync(int spaceId, int id, CharityUpdateDto dto);

        Task<bool> DeleteCharityAsync(int spaceId, int id);

    }
}
