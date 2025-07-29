// <copyright file="IPersonalityRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Interface layer for personality repository</summary>
using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface IPersonalityRepository
    {
        Task<PersonalityData?> GetPersonalityByIdAsync(int id);
        Task<PersonalityData> CreatePersonalityAsync(PersonalityCreateDto dto);

    }

}
