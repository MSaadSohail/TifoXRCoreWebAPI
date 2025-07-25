// <copyright file="ITeleportRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/23/2025</date>
// <summary>Interface to handle TeleportRepository repository pattern</summary>


using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface ITeleportTableRepository
    {
        Task<TeleportTableData> GetTeleportTableBySpaceAsync(int spaceId);
        Task<TeleportTableData> UpdateTeleportTableAsync(int spaceId, int tableId, TeleportTableUpdateDto dto);
    }
}
