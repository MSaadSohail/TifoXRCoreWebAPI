// <copyright file="MetricsController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/23/2025</date>
// <summary>Controller to handle Metrics</summary>
using Microsoft.AspNetCore.Mvc;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [Route("api/metrics")]
    [ApiController]
    public class MetricsController : ControllerBase
    {
        private readonly IBoothActivityRepository _boothActivityRepository;

        public MetricsController(IBoothActivityRepository boothActivityRepository)
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
            [FromBody] List<BoothActivity> boothActivityList
        )
        {
            if (boothActivityList == null || boothActivityList.Count == 0)
                return BadRequest();

            try
            {
                var createdRecords = await _boothActivityRepository.AddUserBoothActivitiesAsync(boothActivityList);
                return Ok(createdRecords);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
