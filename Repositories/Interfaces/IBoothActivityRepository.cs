using TifoXRCoreWebAPI.Models;

namespace TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface IBoothActivityRepository
    {
        Task<List<BoothActivity>> AddUserBoothActivitiesAsync(List<BoothActivity> boothActivityList);
    }
}
