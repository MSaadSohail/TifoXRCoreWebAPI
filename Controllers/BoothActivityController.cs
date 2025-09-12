// <copyright file="MetricsController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/23/2025</date>
// <summary>Controller to handle Metrics</summary>

using Microsoft.AspNetCore.Mvc;
//
using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [Route("api/metrics")]
    [ApiController]
    public class BoothActivityController : ControllerBase
    {
        private readonly IBoothActivityRepository _boothActivityRepository;

        public BoothActivityController(IBoothActivityRepository boothActivityRepository)
        {
            _boothActivityRepository = boothActivityRepository;
        }

        /// <summary>
        /// POST /api/metrics/boothActivity
        /// Inserts a batch of user booth activity metrics.
        /// </summary>
        [HttpPost("boothActivity")]
        [ProducesResponseType(typeof(List<BoothActivity>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<BoothActivity>>> AddUserBoothActivities(
        [FromBody] List<BoothActivity> boothActivityList)
        {
            if (boothActivityList is null || boothActivityList.Count == 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "boothActivityList cannot be null or empty.",
                        nameof(AddUserBoothActivities),
                        new { count = boothActivityList?.Count }
                    ),
                    nameof(boothActivityList)
                );


            var createdRecords = await _boothActivityRepository.AddUserBoothActivitiesAsync(boothActivityList);

            if (createdRecords is null)
                throw new InvalidOperationException(
                    GlobalException.FormatExceptionMessage(
                        "Creation failed.",
                        nameof(AddUserBoothActivities),
                        new { requested = boothActivityList.Count }
                    )
                );

            return Ok(createdRecords);
        }

    }
}
