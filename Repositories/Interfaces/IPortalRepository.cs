// <copyright file="IPortalRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/23/2025</date>
// <summary>Interface to handle portal repository pattern</summary>

using TifoXRCoreWebAPI.Models;

namespace TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface IPortalRepository
    {
        Task<List<PortalModel>> GetPortalsBySpaceAsync(int spaceId);
        Task<List<PortalModel>> GetPortalsByBoothAsync(int spaceId, int boothId);
        Task<PortalModel?> GetPortalByIdAsync(int spaceId, int portalId);
        Task<PortalModel> CreatePortalAsync(int spaceId, PortalCreateDto portalDto);
        Task<PortalModel?> UpdatePortalAsync(int spaceId, int portalId, PortalUpdateDto portalDto);
        Task<PortalModel?> UpdatePortalAsync(int spaceId, int boothId, int portalId, PortalUpdateDto dto);
        Task<bool> DeletePortalAsync(int spaceId, int portalId);
        Task<bool> DeletePortalAsync(int spaceId, int boothId, int portalId);
    }
}
