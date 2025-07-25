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
