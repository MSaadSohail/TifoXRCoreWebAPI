using TifoXRCoreWebAPI.Models;

namespace TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface IPersonalityRepository
    {
        Task<PersonalityData?> GetPersonalityByIdAsync(int id);
    }

}
