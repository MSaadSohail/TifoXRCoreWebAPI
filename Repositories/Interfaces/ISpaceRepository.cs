// <copyright file="ISpaceRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Interface layer for space repository</summary>
using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface ISpaceRepository
    {
        Task<SpaceData> GetSpaceByIdAsync(int id);
        Task<SpaceData> CreateSpaceAsync(Space spaceDto);
        Task<SpaceData> UpdateSpaceAsync(int id, Space spaceDto);

        Task<bool> DeleteSpaceAsync(int id);

    }
}
