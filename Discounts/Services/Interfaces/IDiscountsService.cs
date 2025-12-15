// <copyright file="IDiscountService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>11/26/2025</date>
// <summary>SQL statements for discount definitions listing.</summary>

using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public interface IDiscountsService
    {
        Task<IReadOnlyList<DiscountDto>> ListActiveBySpaceAsync(int spaceId);
        Task<DiscountDto> CreateAsync(int spaceId, CreateDiscountDto dto, string modifiedBy);

        Task<DiscountDto?> UpdateAsync(int spaceId, int discountId, UpdateDiscountDto dto, string modifiedBy);
        Task DeleteAsync(int spaceId, int discountId, bool hard, string modifiedBy);

    }
}
