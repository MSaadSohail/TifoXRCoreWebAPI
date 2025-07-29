// <copyright file="TeleportTableControllerTests.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>07/29/2025</date>
// <summary>Unit tests for TeleportTableController covering endpoint behavior.</summary>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using GMS.TifoXRCoreWebAPI.Controllers;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using TifoXRCoreWebAPI.Tests.Helpers;
using GMS.TifoXRCoreWebAPI.Models;

namespace TifoXRCoreWebAPI.Tests.Controllers
{
    public class TeleportTableControllerTests
    {
        private readonly Mock<ITeleportTableRepository> repo;
        private readonly TeleportTableController sut;
        private readonly TeleportTableUpdateDto defaultDto;

        public TeleportTableControllerTests()
        {
            // Initialize mock repository and controller under test
            repo = new Mock<ITeleportTableRepository>();
            sut = new TeleportTableController(repo.Object);
            defaultDto = new TeleportTableUpdateDtoBuilder().Build();
        }

        //
        // GET /space/{spaceId}/table
        //

        /// <summary>
        /// Verifies that GetTeleportTablesBySpace returns 200 OK when the repository returns valid data,
        /// including edge cases like additional locales and a null NameKey.
        /// </summary>
        [Theory]
        [InlineData(false, false)]   // default
        [InlineData(true, false)]   // extra-locale
        [InlineData(false, true)]    // null-NameKey
        public async Task getBySpaceReturnsOk(bool extraLocale, bool nameKeyNull)
        {
            // ARRANGE: build sample with builder
            var builder = new TeleportTableDataBuilder();
            if (extraLocale) builder.WithAdditionalButtonLocale("en_es", "Test");
            if (nameKeyNull) builder.WithNameKey(null);
            var sample = builder.Build();

            repo.Setup(r => r.GetTeleportTableBySpaceAsync(100))
                .ReturnsAsync(sample);

            // ACT: invoke controller
            var result = await sut.GetTeleportTablesBySpace(100);

            // ASSERT: 200 OK with exact sample
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(sample, opts => opts.WithStrictOrdering());
        }

        /// <summary>
        /// Verifies that GetTeleportTablesBySpace returns 404 NotFound when the repository returns null when wrong spaceId is passed
        /// or when an invalid spaceId (e.g., negative) is passed.
        /// </summary>
        [Theory]
        [InlineData(200, true)]   // repo returns null
        [InlineData(-1, false)]  // invalid id
        public async Task getBySpaceReturnsNotFound(int spaceId, bool repoReturnsNull)
        {
            // ARRANGE
            if (repoReturnsNull)
            {
                repo.Setup(r => r.GetTeleportTableBySpaceAsync(spaceId))
                    .ReturnsAsync((TeleportTableData)null);
            }

            // ACT
            var result = await sut.GetTeleportTablesBySpace(spaceId);

            // ASSERT
            result.Result.Should().BeOfType<NotFoundResult>();
            if (repoReturnsNull)
                repo.Verify(r => r.GetTeleportTableBySpaceAsync(spaceId), Times.Once);
        }

        /// <summary>
        /// Verifies that GetTeleportTablesBySpace returns 500 InternalServerError when the repository throws an exception.
        /// Exception handling is done using private helper function
        /// </summary>
        [Fact]
        public async Task getBySpaceReturns500OnException()
        {
            // ARRANGE: repository throws
            repo.Setup(r => r.GetTeleportTableBySpaceAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("fail"));

            // ACT & ASSERT
            await AssertThrows500(
                () => sut.GetTeleportTablesBySpace(300),
                "fail"
            );
        }

        //
        // PUT /space/{spaceId}/table/{id}
        //

        /// <summary>
        /// Verifies that UpdateTeleportTableById returns 400 BadRequest when the input DTO is null.
        /// This ensures early validation logic short-circuits invalid input.
        /// </summary>
        [Fact]
        public async Task updateByIdReturnsBadRequestWhenDtoNull()
        {
            // ACT
            var result = await sut.UpdateTeleportTableById(1, 1, null!);

            // ASSERT
            result.Result.Should().BeOfType<BadRequestResult>();
        }

        /// <summary>
        /// Verifies that UpdateTeleportTableById returns 200 OK when the DTO is valid and the repository successfully updates the data.
        /// Also confirms that the updated result matches the expected structure.
        /// </summary>
        [Fact]
        public async Task updateByIdReturnsOk()
        {
            // ARRANGE
            var updated = new TeleportTableDataBuilder()
                              .WithSpaceId(1)
                              .WithNameKey("table_key")
                              .Build();
            updated.IsActive = false;

            repo.Setup(r => r.UpdateTeleportTableAsync(1, 1, defaultDto))
                .ReturnsAsync(updated);

            // ACT
            var result = await sut.UpdateTeleportTableById(1, 1, defaultDto);

            // ASSERT
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(updated);
            repo.Verify(r => r.UpdateTeleportTableAsync(1, 1, defaultDto), Times.Once);
        }

        /// <summary>
        /// Verifies that UpdateTeleportTableById returns 404 NotFound either when the repository returns null
        /// or when an invalid spaceId or tableId is used.
        /// </summary>
        [Theory]
        [InlineData(2, 2, true)]   // repo returns null
        [InlineData(-1, 1, false)]   // invalid spaceId
        [InlineData(1, -1, false)]  // invalid tableId
        public async Task updateByIdReturnsNotFound(int spaceId, int tableId, bool repoReturnsNull)
        {
            // ARRANGE
            if (repoReturnsNull)
            {
                repo.Setup(r => r.UpdateTeleportTableAsync(spaceId, tableId, defaultDto))
                    .ReturnsAsync((TeleportTableData)null);
            }

            // ACT
            var result = await sut.UpdateTeleportTableById(spaceId, tableId, defaultDto);

            // ASSERT
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        /// <summary>
        /// Verifies that UpdateTeleportTableById returns 500 InternalServerError when the repository throws an exception.
        /// Confirms that general exception handling is functional for PUT operations.
        /// </summary>
        [Fact]
        public async Task updateByIdReturns500OnException()
        {
            // ARRANGE: repository throws
            repo.Setup(r => r.UpdateTeleportTableAsync(
                                It.IsAny<int>(),
                                It.IsAny<int>(),
                                It.IsAny<TeleportTableUpdateDto>()))
                .ThrowsAsync(new Exception("db error"));

            // ACT & ASSERT
            await AssertThrows500(
                () => sut.UpdateTeleportTableById(3, 3, defaultDto),
                "db error"
            );
        }

        /// <summary>
        /// Verifies that UpdateTeleportTableById returns 400 BadRequestObjectResult with appropriate error messages
        /// when required fields in the DTO (LocalizedName or Buttons) are missing.
        /// </summary>
        [Theory]
        [InlineData(true, false, "LocalizedName is required")]
        [InlineData(false, true, "Buttons are required")]
        public async Task updateByIdBadRequestForInvalidDto(bool nullName, bool nullButtons, string expectedMessage)
        {
            // ARRANGE
            var builder = new TeleportTableUpdateDtoBuilder();
            if (nullName) builder.WithNullLocalizedName();
            if (nullButtons) builder.WithNullButtons();
            var dto = builder.Build();

            // ACT
            var result = await sut.UpdateTeleportTableById(1, 1, dto);

            // ASSERT
            var bad = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().Be(expectedMessage);
        }

        //
        // Helpers
        //

        /// <summary>
        /// Helper method that asserts whether an ActionResult returns a 500 InternalServerError
        /// with a specific error message payload.
        /// </summary>
        private async Task AssertThrows500<T>(
            Func<Task<ActionResult<T>>> action,
            string expectedMessage)
        {
            var obj = (await action()).Result
                        .Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            obj.Value.Should().BeEquivalentTo(new { error = expectedMessage });
        }
    }
}
