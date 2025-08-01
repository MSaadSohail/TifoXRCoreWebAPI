// <copyright file="BoothControllerTests.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>07/30/2025</date>
// <summary>Unit tests for BoothController covering all endpoint behavior.</summary>

using FluentAssertions;
using GMS.TifoXRCoreWebAPI.Controllers;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using GMS.TifoXRCoreWebAPI.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GMS.TifoXRCoreWebAPI.Tests.Controllers
{
    public class BoothControllerTests
    {
        private readonly Mock<IBoothRepository> mockRepo;
        private readonly BoothController sut;
        private readonly BoothCreateDto defaultCreateDto;
        private readonly BoothUpdateDto defaultUpdateDto;
        private readonly BoothModelBuilder boothBuilder;

        public BoothControllerTests()
        {
            mockRepo = new Mock<IBoothRepository>();
            sut = new BoothController(mockRepo.Object);
            defaultCreateDto = new BoothCreateDtoBuilder().Build();
            defaultUpdateDto = new BoothUpdateDtoBuilder().Build();
            boothBuilder = new BoothModelBuilder();
        }

        //
        // GET /space/{spaceId}/booths
        //

        /// <summary>
        /// Verifies that GetAllBoothsBySpace returns 200 OK when booths exist,
        /// and 404 NotFound when the repo returns an empty list or null.
        /// </summary>
        [Theory]
        [InlineData(true, false)]  // booths exist → 200
        [InlineData(false, false)] // empty list → 404
        [InlineData(false, true)]  // null         → 404
        public async Task getAllBoothsBySpaceReturnsOkOrNotFoundForListVariations(bool hasBooths, bool returnNull)
        {
            // ARRANGE
            List<BoothModel>? booths = returnNull
                ? null
                : hasBooths
                    ? new List<BoothModel> { boothBuilder.WithId(1).Build() }
                    : new List<BoothModel>();

            mockRepo.Setup(r => r.GetAllBoothsBySpaceAsync(100)).ReturnsAsync(booths);

            // ACT
            var result = await sut.GetAllBoothsBySpace(100);

            // ASSERT
            if (hasBooths)
            {
                var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                ok.StatusCode.Should().Be(StatusCodes.Status200OK);
                ok.Value.Should().BeEquivalentTo(booths);
            }
            else
            {
                result.Result.Should().BeOfType<NotFoundResult>();
            }
        }

        /// <summary>
        /// Verifies that GetAllBoothsBySpace returns 500 when the repository throws.
        /// </summary>
        [Fact]
        public async Task getBySpaceReturns500OnException()
        {
            // ARRANGE
            mockRepo.Setup(r => r.GetAllBoothsBySpaceAsync(It.IsAny<int>())).ThrowsAsync(new Exception("fail"));

            // ACT & ASSERT
            await AssertThrows500(() => sut.GetAllBoothsBySpace(100), "fail");
        }

        //
        // POST /space/{spaceId}/booth
        //

        /// <summary>
        /// Verifies CreateBooth returns 201 Created or 400 BadRequest when DTO is null.
        /// </summary>
        [Theory]
        [InlineData(true)]  // dto is null
        [InlineData(false)] // valid dto
        public async Task createBoothReturnsExpectedResult(bool isNullDto)
        {
            if (isNullDto)
            {
                // ACT
                var result = await sut.CreateBooth(100, null!);

                // ASSERT
                result.Result.Should().BeOfType<BadRequestResult>();
                return;
            }

            // ARRANGE
            var booth = boothBuilder.WithId(1).Build();
            mockRepo.Setup(r => r.CreateBoothAsync(100, defaultCreateDto)).ReturnsAsync(booth);

            // ACT
            var result2 = await sut.CreateBooth(100, defaultCreateDto);

            // ASSERT
            var created = result2.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.RouteValues["spaceId"].Should().Be(100);
            created.StatusCode.Should().Be(StatusCodes.Status201Created);
            created.Value.Should().BeEquivalentTo(new BoothWrapper { booth = booth });
        }

        /// <summary>
        /// Verifies CreateBooth returns 500 when LocalizedPair is invalid.
        /// </summary>
        [Theory]
        [InlineData(true, false, false, "null pairs")]   // LocalizedPairs null
        [InlineData(false, true, false, "null key")]     // key null
        [InlineData(false, false, true, "empty values")] // values empty
        public async Task createBoothReturns500OnInvalidLocalizedName(
            bool nullPairs,
            bool nullKey,
            bool emptyValues,
            string expectedMessage)
        {
            // ARRANGE
            var builder = new BoothCreateDtoBuilder();
            if (nullPairs)
                builder.WithNullLocalizedPairs();
            else if (nullKey)
                builder.WithLocalizedPairs(null!, new Dictionary<string, string> { { "en", "Name" } });
            else if (emptyValues)
                builder.WithLocalizedPairs("key", new Dictionary<string, string>());

            var dto = builder.Build();
            mockRepo
                .Setup(r => r.CreateBoothAsync(100, dto))
                .ThrowsAsync(new Exception(expectedMessage));

            // ACT & ASSERT
            await AssertThrows500<BoothWrapper>(
                () => sut.CreateBooth(100, dto),
                expectedMessage
            );
        }

        /// <summary>
        /// Verifies CreateBooth returns 500 when the repository throws.
        /// </summary>
        [Fact]
        public async Task createBoothReturns500OnException()
        {
            // ARRANGE
            mockRepo.Setup(r => r.CreateBoothAsync(100, defaultCreateDto)).ThrowsAsync(new Exception("create fail"));

            // ACT & ASSERT
            await AssertThrows500(() => sut.CreateBooth(100, defaultCreateDto), "create fail");
        }

        //
        // PUT /space/{spaceId}/booth/{boothId}
        //

        /// <summary>
        /// Verifies UpdateBooth returns 200 OK, 400 BadRequest, or 404 NotFound.
        /// </summary>
        [Theory]
        [InlineData("nullDto")]
        [InlineData("notFound")]
        [InlineData("success")]
        public async Task updateBoothReturnsExpectedResult(string scenario)
        {
            // ACT & ASSERT
            switch (scenario)
            {
                case "nullDto":
                    // ACT
                    var bad = await sut.UpdateBooth(100, 1, null!);
                    // ASSERT
                    bad.Result.Should().BeOfType<BadRequestResult>();
                    return;

                case "notFound":
                    // ARRANGE
                    mockRepo.Setup(r => r.UpdateBoothAsync(100, 1, defaultUpdateDto)).ReturnsAsync((BoothModel?)null);
                    // ACT
                    var nf = await sut.UpdateBooth(100, 1, defaultUpdateDto);
                    // ASSERT
                    nf.Result.Should().BeOfType<NotFoundResult>();
                    return;

                case "success":
                    // ARRANGE
                    var updated = boothBuilder.WithId(1).Build();
                    mockRepo.Setup(r => r.UpdateBoothAsync(100, 1, defaultUpdateDto)).ReturnsAsync(updated);
                    // ACT
                    var ok = await sut.UpdateBooth(100, 1, defaultUpdateDto);
                    // ASSERT
                    var okRes = ok.Result.Should().BeOfType<OkObjectResult>().Subject;
                    okRes.StatusCode.Should().Be(StatusCodes.Status200OK);
                    okRes.Value.Should().BeEquivalentTo(new Response { Booth = updated });
                    return;
            }
        }

        /// <summary>
        /// Verifies UpdateBooth returns 500 when repository throws.
        /// </summary>
        [Fact]
        public async Task updateBoothReturns500OnException()
        {
            // ARRANGE
            mockRepo.Setup(r => r.UpdateBoothAsync(100, 1, defaultUpdateDto)).ThrowsAsync(new Exception("update fail"));

            // ACT & ASSERT
            await AssertThrows500(() => sut.UpdateBooth(100, 1, defaultUpdateDto), "update fail");
        }

        //
        // DELETE /space/{spaceId}/booth/{boothId}
        //

        /// <summary>
        /// Verifies DeleteBoothCascade returns 204 NoContent or 404 NotFound.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task deleteBoothReturnsExpectedResult(bool repoReturnsTrue)
        {
            // ARRANGE
            mockRepo.Setup(r => r.DeleteBoothCascadeAsync(100, 1)).ReturnsAsync(repoReturnsTrue);

            // ACT
            var result = await sut.DeleteBoothCascade(100, 1);

            // ASSERT
            if (repoReturnsTrue)
                result.Should().BeOfType<NoContentResult>();
            else
                result.Should().BeOfType<NotFoundResult>();
        }

        /// <summary>
        /// Verifies DeleteBoothCascade returns 500 when repository throws.
        /// </summary>
        [Fact]
        public async Task deleteBoothReturns500OnException()
        {
            // ARRANGE
            mockRepo.Setup(r => r.DeleteBoothCascadeAsync(100, 1)).ThrowsAsync(new Exception("delete fail"));

            // ACT & ASSERT
            await AssertThrows500(() => sut.DeleteBoothCascade(100, 1), "delete fail");
        }

        //
        // Helpers
        //

        /// <summary>
        /// Helper method that asserts a 500 InternalServerError result with matching error message.
        /// </summary>
        private async Task AssertThrows500<T>(Func<Task<ActionResult<T>>> action, string expectedMessage)
        {
            var obj = (await action()).Result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            obj.Value.Should().BeEquivalentTo(new { error = expectedMessage });
        }

        /// <summary>
        /// Overload for IActionResult-based endpoints to avoid generic inference issues.
        /// </summary>
        private async Task AssertThrows500(Func<Task<IActionResult>> action, string expectedMessage)
        {
            var obj = (await action()).Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            obj.Value.Should().BeEquivalentTo(new { error = expectedMessage });
        }
    }
}
