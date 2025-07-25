using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface IBoothActivityRepository
    {
        Task<List<BoothActivity>> AddUserBoothActivitiesAsync(List<BoothActivity> boothActivityList);
    }
}
