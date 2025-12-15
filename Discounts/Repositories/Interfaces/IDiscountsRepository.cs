// <copyright file="IDiscountRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>11/26/2025</date>
// <summary>SQL statements for discount definitions listing.</summary>

using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public interface IDiscountsRepository
    {
        Task<IReadOnlyList<DiscountDto>> ListActiveBySpaceAsync(int spaceId);
        Task<HashSet<string>> GetSupportedLocalesAsync(int spaceId);
        Task<bool> DiscountTypeExistsAsync(int discountTypeId);

        Task<DiscountDto> CreateAsync(int spaceId, CreateDiscountDto dto, string modifiedBy);
        Task<DiscountDto?> UpdateAsync(int spaceId, int discountId, UpdateDiscountDto dto, string modifiedBy);

        // ADD to IDiscountsRepository.cs
        Task<bool> ExistsInSpaceAsync(int spaceId, int discountId);
        Task<int> SoftExpireNowAsync(int spaceId, int discountId, string modifiedBy);
        Task<int> HardDeleteAsync(int spaceId, int discountId);

    }
}

