// <copyright file="IBoothActivityRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author></author>
// <date>07/28/2025</date>
// <summary>Interface layer for booth activity repository</summary>
using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories 
{
    public interface IBoothActivityRepository
    {
        Task<List<BoothActivity>> AddUserBoothActivitiesAsync(List<BoothActivity> boothActivityList);
    }
}
