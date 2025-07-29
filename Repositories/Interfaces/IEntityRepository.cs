// <copyright file="IEntityRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Interface layer for Entity repository</summary>
using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface IEntityRepository
    {
        Task<EntityData> GetByIdAsync(int id);
        Task<EntityData> CreateAsync(Entity dto);
        Task<EntityData> UpdateAsync(int id, Entity dto);
    }
}
