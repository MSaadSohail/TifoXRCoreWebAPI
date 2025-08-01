using GMS.TifoXRCoreWebAPI.Models.Common;

public interface ILocalizationRepository
{
    Task<List<LocalizedPairs>> GetAllLocalizationsBySpaceAsync(int spaceId);
    Task<LocalizedPairs?> GetLocalizationByKeyAsync(int spaceId, string key);
    Task<LocalizedPairs> CreateLocalizationAsync(int spaceId, LocalizedPairs dto);
    Task<LocalizedPairs?> UpdateLocalizationAsync(int spaceId, string key, LocalizedPairs dto);
    Task<bool> DeleteLocalizationAsync(int spaceId, string key);
}
