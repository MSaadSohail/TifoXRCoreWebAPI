using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface IPersonalityRepository
    {
        Task<PersonalityData?> GetPersonalityByIdAsync(int id);
        Task<PersonalityData> CreatePersonalityAsync(PersonalityCreateDto dto);

    }

}
