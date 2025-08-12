// <copyright file="EventControllerTests.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/11/2025</date>
// <summary>Unit tests for EventController covering endpoint behavior.</summary>

using FluentAssertions;
using GMS.TifoXRCoreWebAPI.Controllers;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using GMS.TifoXRCoreWebAPI.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Data;

namespace GMS.TifoXRCoreWebAPI.Tests.Controllers
{
    public class EventControllerTests
    {
        private readonly Mock<IEventRepository> repo;
        private readonly EventController sut;
        private readonly EventDtoBuilder dtoBuilder;

        public EventControllerTests()
        {
            // Initialize mock repository and controller under test
            repo = new Mock<IEventRepository>();
            sut = new EventController(repo.Object);
            dtoBuilder = new EventDtoBuilder();
        }

        #region GET
        //
        // GET /api/event/{event_id}
        //

        /// <summary>
        /// Verifies that GetEventDataByID returns 200 OK when the repository returns valid data,
        /// including edge cases like additional locales and presence of description localization.
        /// </summary>
        [Fact]
        public async Task GetById_ReturnsOk()
        {
            // ARRANGE
            var sample = new EventModelBuilder()
                .WithId(100)
                .WithAdditionalNameLocale("en_es", "Partido")
                .WithAdditionalDescLocale("en_es", "Descripción")
                .Build();

            repo.Setup(r => r.GetEventByIdAsync(100))
                .ReturnsAsync(sample);

            // ACT
            var result = await sut.GetEventDataByID(100);

            // ASSERT
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(sample, opts => opts.WithStrictOrdering());
        }

        /// <summary>
        /// Verifies that GetEventDataByID throws the correct exception
        /// when the repository returns null (resource not found)
        /// or when an invalid event_id (e.g., non-positive) is passed.
        /// </summary>
        [Theory]
        [InlineData(200, true)]   // repo returns null => not found
        [InlineData(0, false)]   // invalid id => argument error
        [InlineData(-1, false)]   // invalid id => argument error
        public async Task GetById_ThrowsNotFoundOrArg(int eventId, bool repoReturnsNull)
        {
            // ARRANGE
            if (repoReturnsNull)
            {
                repo.Setup(r => r.GetEventByIdAsync(eventId))
                    .ReturnsAsync((EventData?)null);
            }

            // ACT & ASSERT
            if (repoReturnsNull)
            {
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () => sut.GetEventDataByID(eventId)
                );
                repo.Verify(r => r.GetEventByIdAsync(eventId), Times.Once);
            }
            else
            {
                await Assert.ThrowsAsync<ArgumentException>(
                    () => sut.GetEventDataByID(eventId)
                );
            }
        }

        /// <summary>
        /// Verifies that GetEventDataByID throws an exception
        /// when the repository throws an exception (handled by global middleware at runtime).
        /// </summary>
        [Fact]
        public async Task GetByIdThrows_OnRepositoryEx()
        {
            // ARRANGE
            repo.Setup(r => r.GetEventByIdAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Simulated repository exception"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.GetEventDataByID(300));
            Assert.Equal("Simulated repository exception", ex.Message);
        }

        #endregion

        #region POST

        /// <summary>
        /// Verifies that CreateEvent throws ArgumentNullException when the input DTO is null.
        /// Ensures that the action does not process a missing payload.
        /// </summary>
        [Fact]
        public async Task Create_ThrowsArgNullEx_OnNullDto()
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => sut.CreateEvent(null!)
            );
        }

        /// <summary>
        /// Verifies that CreateEvent throws InvalidOperationException when the repository returns null,
        /// simulating a failure to persist the new event.
        /// </summary>
        [Fact]
        public async Task Create_ThrowsInvalidOpEx_OnCreateFail()
        {
            // ARRANGE
            var dto = dtoBuilder.Build();
            repo.Setup(r => r.CreateEventAsync(dto))
                .ReturnsAsync((EventData?)null);

            // ACT & ASSERT
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.CreateEvent(dto)
            );
        }

        /// <summary>
        /// Verifies that CreateEvent returns CreatedAtActionResult with the correct route and value
        /// when a new event is successfully created.
        /// </summary>
        [Fact]
        public async Task Create_ReturnsCreatedAt_OnSuccess()
        {
            // ARRANGE
            var dto = dtoBuilder.WithSpaceId(7).Build();
            var created = new EventModelBuilder()
                .WithId(42)
                .FromDto(dto)
                .Build();

            repo.Setup(r => r.CreateEventAsync(dto))
                .ReturnsAsync(created);

            // ACT
            var result = await sut.CreateEvent(dto);

            // ASSERT
            var createdAt = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdAt.ActionName.Should().Be(nameof(EventController.GetEventDataByID));
            createdAt.RouteValues["event_id"].Should().Be(42);
            createdAt.Value.Should().BeEquivalentTo(created, opts => opts.WithStrictOrdering());
            repo.Verify(r => r.CreateEventAsync(dto), Times.Once);
        }

        #endregion

        #region PUT
        //
        // PUT /api/event/{eventId}
        //

        /// <summary>
        /// Verifies that UpdateEvent throws ArgumentNullException when the input DTO is null.
        /// This ensures early validation logic short-circuits invalid input.
        /// </summary>
        [Fact]
        public async Task UpdateById_ThrowsArgNullExDtoNull()
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => sut.UpdateEvent(1, null!)
            );
        }

        /// <summary>
        /// Verifies that UpdateEvent returns 200 OK when the DTO is valid and the repository successfully updates the data.
        /// Also confirms that the updated result matches the expected structure.
        /// </summary>
        [Fact]
        public async Task UpdateById_ReturnsOk()
        {
            // ARRANGE
            var dto = dtoBuilder.WithIsLive(true).Build();
            var updated = new EventModelBuilder()
                .WithId(1)
                .FromDto(dto)
                .Build();

            repo.Setup(r => r.UpdateEventAsync(1, dto))
                .ReturnsAsync(updated);

            // ACT
            var result = await sut.UpdateEvent(1, dto);

            // ASSERT
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(updated, opts => opts.WithStrictOrdering());
            repo.Verify(r => r.UpdateEventAsync(1, dto), Times.Once);
        }

        /// <summary>
        /// Verifies that UpdateEvent throws the correct exception
        /// either when the repository returns null (not found)
        /// or when an invalid eventId is used (bad request).
        /// </summary>
        [Theory]
        [InlineData(2, true)]   // repo returns null
        [InlineData(0, false)]  // invalid eventId
        [InlineData(-5, false)] // invalid eventId
        public async Task UpdateById_ThrowsNotFoundOrInvalidId(int eventId, bool repoReturnsNull)
        {
            // ARRANGE
            var dto = dtoBuilder.Build();

            if (repoReturnsNull && eventId > 0)
            {
                repo.Setup(r => r.UpdateEventAsync(eventId, dto))
                    .ReturnsAsync((EventData?)null);
            }

            // ACT & ASSERT
            if (repoReturnsNull && eventId > 0)
            {
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () => sut.UpdateEvent(eventId, dto)
                );
            }
            else
            {
                await Assert.ThrowsAsync<ArgumentException>(
                    () => sut.UpdateEvent(eventId, dto)
                );
            }
        }

        /// <summary>
        /// Verifies that UpdateEvent throws an exception when the repository throws,
        /// confirming general exception propagation for PUT operations.
        /// </summary>
        [Fact]
        public async Task UpdateById_ThrowsRepositoryEx()
        {
            // ARRANGE
            var dto = dtoBuilder.Build();
            repo.Setup(r => r.UpdateEventAsync(It.IsAny<int>(), It.IsAny<Event>()))
                .ThrowsAsync(new Exception("UpdateEventAsync encountered a database error"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(
                () => sut.UpdateEvent(3, dto)
            );

            Assert.Equal("UpdateEventAsync encountered a database error", ex.Message);
        }

        /// <summary>
        /// Verifies that UpdateEvent bubbles DBConcurrencyException when the repo detects a row conflict.
        /// </summary>
        [Fact]
        public async Task UpdateById_ThrowsConcurrencyEx_OnRowConflict()
        {
            // ARRANGE
            var dto = dtoBuilder.Build();
            repo.Setup(r => r.UpdateEventAsync(It.IsAny<int>(), It.IsAny<Event>()))
                .ThrowsAsync(new DBConcurrencyException("Row was modified by another process"));

            // ACT & ASSERT
            await Assert.ThrowsAsync<DBConcurrencyException>(
                () => sut.UpdateEvent(3, dto)
            );
        }

        #endregion
    }
}
