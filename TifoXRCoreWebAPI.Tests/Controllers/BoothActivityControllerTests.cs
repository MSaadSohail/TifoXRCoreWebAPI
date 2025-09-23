// <copyright file="MetricsControllerTests.cs" company="Global Mobile Software">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/13/2025</date>
// <summary>Unit tests for MetricsController covering endpoint behavior</summary>

using FluentAssertions;
using GMS.TifoXRCoreWebAPI.Controllers;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GMS.TifoXRCoreWebAPI.Tests.Controllers
{
    /// <summary>
    /// Tests the MetricsController using a mocked IMetricsRepository.
    /// Organized similarly to TeleportTableControllerTests (regions, explicit route notes, negative-path focus).
    /// </summary>
    public class BoothActivityControllerTests
    {
        private readonly Mock<IBoothActivityRepository> repo;
        private readonly BoothActivityController sut;

        public BoothActivityControllerTests()
        {
            repo = new Mock<IBoothActivityRepository>(MockBehavior.Strict);
            sut = new BoothActivityController(repo.Object);
        }

        #region POST
        //
        // POST /api/metrics/boothActivity
        //

        /// <summary>
        /// Ensures AddUserBoothActivities throws ArgumentException when the input list is null.
        /// </summary>
        [Fact]
        public async Task AddUserBoothActivities_ThrowsArgEx_OnNullList()
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() => sut.AddUserBoothActivities(null!));

            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Ensures AddUserBoothActivities throws ArgumentException when the input list is empty.
        /// </summary>
        [Fact]
        public async Task AddUserBoothActivities_ThrowsArgEx_OnEmptyList()
        {
            // ARRANGE
            var empty = new List<BoothActivity>();

            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() => sut.AddUserBoothActivities(empty));

            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that AddUserBoothActivities throws InvalidOperationException when the repository returns null,
        /// simulating a failure to persist the batch (controller logic).
        /// </summary>
        [Fact]
        public async Task AddUserBoothActivities_ThrowsInvalidOp_WhenRepoReturnsNull()
        {
            // ARRANGE
            var input = new BoothActivityListBuilder().WithCount(2).Build();

            repo.Setup(r => r.AddUserBoothActivitiesAsync(input))
                .ReturnsAsync((List<BoothActivity>?)null);

            // ACT & ASSERT
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AddUserBoothActivities(input));

            repo.Verify(r => r.AddUserBoothActivitiesAsync(input), Times.Once);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that AddUserBoothActivities propagates repository exceptions
        /// (handled by global middleware at runtime).
        /// </summary>
        [Fact]
        public async Task AddUserBoothActivities_ThrowsRepositoryEx()
        {
            // ARRANGE
            var input = new BoothActivityListBuilder().WithCount(3).Build();

            repo.Setup(r => r.AddUserBoothActivitiesAsync(input))
                .ThrowsAsync(new Exception("AddUserBoothActivitiesAsync encountered a database error"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.AddUserBoothActivities(input));
            ex.Message.Should().Be("AddUserBoothActivitiesAsync encountered a database error");

            repo.Verify(r => r.AddUserBoothActivitiesAsync(input), Times.Once);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that AddUserBoothActivities returns 200 OK with the created records
        /// when the repository successfully inserts the batch.
        /// Also ensures the same list is passed through to the repository.
        /// </summary>
        [Fact]
        public async Task AddUserBoothActivities_ReturnsOk_OnSuccess()
        {
            // ARRANGE
            var input = new BoothActivityListBuilder()
                            .WithCount(2)
                            .WithExitDatetime(0, DateTime.UtcNow.AddMinutes(5))
                            .Build();

            // Simulate repo echoing back created rows (common pattern in other tests).
            var created = input.Select(x => new BoothActivity
            {
                BoothId = x.BoothId,
                UserId = x.UserId,
                ExitCode = x.ExitCode,
                BoothNameKey = x.BoothNameKey,
                SessionId = x.SessionId,
                EntryDatetime = x.EntryDatetime,
                SessionDuration = x.SessionDuration,
                ExitDatetime = x.ExitDatetime
            }).ToList();

            repo.Setup(r => r.AddUserBoothActivitiesAsync(input)).ReturnsAsync(created);

            // ACT
            var result = await sut.AddUserBoothActivities(input);

            // ASSERT
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(created, opts => opts.WithStrictOrdering());

            repo.Verify(r => r.AddUserBoothActivitiesAsync(input), Times.Once);
            repo.VerifyNoOtherCalls();
        }

        #endregion
    }
}
