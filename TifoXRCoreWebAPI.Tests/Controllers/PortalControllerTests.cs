// <copyright file="PortalControllerTests.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>07/29/2025</date>
// <summary>Unit tests for PortalController covering endpoint behavior, response structure,and validation logic using mocked repository and FluentAssertions.</summary>

using FluentAssertions;
using GMS.TifoXRCoreWebAPI.Controllers;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GMS.TifoXRCoreWebAPI.Tests.Helpers;

namespace GMS.TifoXRCoreWebAPI.Tests.Controllers
{
    public class PortalControllerTests
    {
        private readonly Mock<IPortalRepository> mockRepo;
        private readonly PortalController sut;
        private readonly PortalCreateDto defaultCreateDto;
        private readonly PortalUpdateDto defaultUpdateDto;
        private readonly PortalModelBuilder portalBuilder;

        public PortalControllerTests()
        {
            mockRepo = new Mock<IPortalRepository>();
            sut = new PortalController(mockRepo.Object);
            defaultCreateDto = new PortalCreateDtoBuilder().Build();
            defaultUpdateDto = new PortalUpdateDtoBuilder().Build();
            portalBuilder = new PortalModelBuilder();
        }

        //
        // GET /space/{spaceId}/portal
        //

        /// <summary>
        /// Verifies that GetPortalsBySpace returns 200 OK when valid data is returned by the repository.
        /// </summary>
        [Fact]
        public async Task getPortalsBySpaceReturnsOk()
        {
            // ARRANGE: repository returns a sample list
            var sampleList = new List<PortalModel>
            {
                portalBuilder.WithId(1).WithSpaceId(100).Build()
            };
            mockRepo.Setup(r => r.GetPortalsBySpaceAsync(100))
                    .ReturnsAsync(sampleList);

            // ACT: invoke controller
            var result = await sut.GetPortalsBySpace(100);

            // ASSERT: 200 OK with expected data
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(sampleList);
        }

        /// <summary>
        /// Verifies that GetPortalsBySpace returns 404 NotFound when repository returns null or an empty list.
        /// </summary>
        [Theory]
        [InlineData(true)]   // null
        [InlineData(false)]  // empty
        public async Task getPortalsBySpaceReturnsNotFound(bool returnsNull)
        {
            // ARRANGE: repository returns null or empty
            var data = returnsNull ? null : new List<PortalModel>();
            mockRepo.Setup(r => r.GetPortalsBySpaceAsync(100))
                    .ReturnsAsync(data);

            // ACT: invoke controller
            var result = await sut.GetPortalsBySpace(100);

            // ASSERT: NotFound result
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        /// <summary>
        /// Verifies that GetPortalsBySpace returns 500 InternalServerError when the repository throws.
        /// </summary>
        [Fact]
        public Task getPortalsBySpaceReturns500OnException()
        {
            // ARRANGE: repository throws exception
            mockRepo.Setup(r => r.GetPortalsBySpaceAsync(100))
                    .ThrowsAsync(new Exception("fail"));

            // ACT & ASSERT: 500 response
            return AssertThrows500(() => sut.GetPortalsBySpace(100), "fail");
        }


        //
        // GET /space/{spaceId}/booth/{boothId}/portals
        //

        /// <summary>
        /// Verifies that GetPortalsByBooth returns 200 OK with valid results.
        /// </summary>
        [Fact]
        public async Task getPortalsByBoothReturnsOk()
        {
            // ARRANGE: repository returns a sample portal
            var sample = portalBuilder.WithSpaceId(100).WithBoothId(200).Build();
            mockRepo.Setup(r => r.GetPortalsByBoothAsync(100, 200))
                    .ReturnsAsync(new List<PortalModel> { sample });

            // ACT: invoke controller
            var result = await sut.GetPortalsByBooth(100, 200);

            // ASSERT: 200 OK with expected data
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(new[] { sample });
        }

        /// <summary>
        /// Verifies that GetPortalsByBooth returns 404 NotFound when repository returns null or empty.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task getPortalsByBoothReturnsNotFound(bool returnsNull)
        {
            // ARRANGE: repository returns null or empty
            var data = returnsNull ? null : new List<PortalModel>();
            mockRepo.Setup(r => r.GetPortalsByBoothAsync(100, 200))
                    .ReturnsAsync(data);

            // ACT: invoke controller
            var result = await sut.GetPortalsByBooth(100, 200);

            // ASSERT: NotFound result
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        /// <summary>
        /// Verifies that GetPortalsByBooth returns 500 InternalServerError when the repository throws.
        /// </summary>
        [Fact]
        public Task getPortalsByBoothReturns500OnException()
        {
            // ARRANGE: repository throws exception
            mockRepo.Setup(r => r.GetPortalsByBoothAsync(100, 200))
                    .ThrowsAsync(new Exception("booth fail"));

            // ACT & ASSERT: 500 response
            return AssertThrows500(() => sut.GetPortalsByBooth(100, 200), "booth fail");
        }

        //
        // GET /space/{spaceId}/portal/{portalId}
        //

        /// <summary>
        /// Verifies that GetPortalById returns 200 OK when valid data is found.
        /// </summary>
        [Fact]
        public async Task getPortalByIdReturnsOk()
        {
            // ARRANGE: repository returns a portal
            var sample = portalBuilder.WithSpaceId(100).WithId(1).Build();
            mockRepo.Setup(r => r.GetPortalByIdAsync(100, 1))
                    .ReturnsAsync(sample);

            // ACT: invoke controller
            var result = await sut.GetPortalById(100, 1);

            // ASSERT: 200 OK with expected data
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(sample);
        }

        /// <summary>
        /// Verifies that GetPortalById returns 404 NotFound when portal does not exist.
        /// </summary>
        [Fact]
        public async Task getPortalByIdReturnsNotFound()
        {
            // ARRANGE: repository returns null
            mockRepo.Setup(r => r.GetPortalByIdAsync(100, 200))
                    .ReturnsAsync((PortalModel?)null);

            // ACT: invoke controller
            var result = await sut.GetPortalById(100, 200);

            // ASSERT: NotFound result
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        /// <summary>
        /// Verifies that GetPortalById returns 500 InternalServerError on exception.
        /// </summary>
        [Fact]
        public Task getPortalByIdReturns500OnException()
        {
            // ARRANGE: repository throws exception
            mockRepo.Setup(r => r.GetPortalByIdAsync(100, 1))
                    .ThrowsAsync(new Exception("id fail"));

            // ACT & ASSERT: 500 response
            return AssertThrows500(() => sut.GetPortalById(100, 1), "id fail");
        }

        //
        // POST /space/{spaceId}/portal/portal
        //

        /// <summary>
        /// Verifies that CreatePortal returns 201 Created when a portal is successfully created.
        /// </summary>
        [Fact]
        public async Task createPortalReturnsCreated()
        {
            // ARRANGE: repository will create and return portal
            var expected = portalBuilder.WithSpaceId(100).WithId(1).Build();
            mockRepo.Setup(r => r.CreatePortalAsync(100, defaultCreateDto))
                    .ReturnsAsync(expected);

            // ACT: invoke controller
            var result = await sut.CreatePortal(100, defaultCreateDto);

            // ASSERT: CreatedAtAction with correct route and value
            var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.StatusCode.Should().Be(StatusCodes.Status201Created);
            created.ActionName.Should().Be(nameof(PortalController.GetPortalById));
            created.RouteValues["spaceId"].Should().Be(100);
            created.RouteValues["portalId"].Should().Be(1);
            created.Value.Should().BeEquivalentTo(expected);
        }

        /// <summary>
        /// Verifies that CreatePortal returns 500 InternalServerError on exception.
        /// </summary>
        [Fact]
        public Task createPortalReturns500OnException()
        {
            // ARRANGE: repository throws exception
            mockRepo.Setup(r => r.CreatePortalAsync(100, defaultCreateDto))
                    .ThrowsAsync(new Exception("create fail"));

            // ACT & ASSERT: 500 response
            return AssertThrows500(() => sut.CreatePortal(100, defaultCreateDto), "create fail");
        }

        /// <summary>
        /// Verifies that CreatePortal returns 400 BadRequest when LocalizedPairs is null.
        /// </summary>
        [Fact]
        public async Task createPortalReturnsBadRequestIfLocalizedPairsIsNull()
        {
            // Arrange
            var invalidDto = new PortalCreateDto
            {
                LocalizedPairs = null
            };

            // Act
            var result = await sut.CreatePortal(100, invalidDto);

            //Assert
            result.Result.Should().BeOfType<BadRequestResult>();
        }

        /// <summary>
        /// Verifies that CreatePortal returns 400 BadRequest when LocalizedPairs.Key is null or empty.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task createPortalReturnsBadRequestIfLocalizedPairKeyIsNullOrEmpty(string? key)
        {
            // Assert
            var invalidDto = new PortalCreateDtoBuilder()
                .WithLocalizedPairs(key!, new Dictionary<string, string> { { "en_us", "Test" } })
                .Build();

            // Act
            var result = await sut.CreatePortal(100, invalidDto);

            // Assert
            result.Result.Should().BeOfType<BadRequestResult>();
        }

        /// <summary>
        /// Verifies that CreatePortal returns 400 BadRequest when LocalizedPairs.Values is null or empty.
        /// </summary>
        [Theory]
        [InlineData(true)] // null
        [InlineData(false)] // empty
        public async Task createPortalReturnsBadRequestIfLocalizedNameLocalizationsIsNullOrEmpty(bool isNull)
        {
            // Arrange
            var invalidDto = isNull
                ? new PortalCreateDto
                {
                    LocalizedPairs = new LocalizedPairs
                    {
                        Key = "portal.name",
                        Values = null
                    }
                }
                : new PortalCreateDto
                {
                    LocalizedPairs = new LocalizedPairs
                    {
                        Key = "portal.name",
                        Values = new List<LocalizedValue>()
                    }
                };

            // Act
            var result = await sut.CreatePortal(100, invalidDto);

            // Assert
            result.Result.Should().BeOfType<BadRequestResult>();
        }

        //
        // PUT /space/{spaceId}/portal/{portalId}
        //

        /// <summary>
        /// Verifies that UpdatePortal returns 200 OK when a portal is updated successfully.
        /// </summary>
        [Fact]
        public async Task updatePortalReturnsOk()
        {
            // ARRANGE: repository will update and return portal
            var expected = portalBuilder.WithSpaceId(100).WithId(1).Build();
            mockRepo.Setup(r => r.UpdatePortalAsync(100, 1, defaultUpdateDto))
                    .ReturnsAsync(expected);

            // ACT: invoke controller
            var result = await sut.UpdatePortal(100, 1, defaultUpdateDto);

            // ASSERT: 200 OK with expected data
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(expected);
        }

        /// <summary>
        /// Verifies that UpdatePortal returns 500 InternalServerError on exception.
        /// </summary>
        [Fact]
        public Task updatePortalReturns500OnException()
        {
            // ARRANGE: repository throws exception
            mockRepo.Setup(r => r.UpdatePortalAsync(100, 1, defaultUpdateDto))
                    .ThrowsAsync(new Exception("update fail"));

            // ACT & ASSERT: 500 response
            return AssertThrows500(() => sut.UpdatePortal(100, 1, defaultUpdateDto), "update fail");
        }

        /// <summary>
        /// Verifies that UpdatePortal returns 400 BadRequest when LocalizedPairs is null.
        /// </summary>
        [Fact]
        public async Task updatePortalReturnsBadRequestIfLocalizedPairsIsNull()
        {
            // Arrange
            var invalidDto = new PortalUpdateDto
            {
                // 'required' is compile-time; assigning null here triggers a warning, which is fine for this negative test.
                LocalizedPairs = null!,
                ExternalLink = null
            };

            // Act
            var result = await sut.UpdatePortal(100, 1, invalidDto);

            // Assert
            result.Result.Should().BeOfType<BadRequestResult>();
        }

        /// <summary>
        /// Verifies that UpdatePortal returns 400 BadRequest when LocalizedPairs.Key is null or empty.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task updatePortalReturnsBadRequestIfLocalizedNameKeyIsNullOrEmpty(string? key)
        {
            // Arrange
            var invalidPairs = new LocalizedPairs
            {
                Key = key!,
                Values = new List<LocalizedValue> { new LocalizedValue { LocaleId = "en_us", Value = "Test" } }
            };

            var invalidDto = new PortalUpdateDtoBuilder()
                .WithLocalizedPairs(invalidPairs)
                .Build();

            // Act
            var result = await sut.UpdatePortal(100, 1, invalidDto);

            // Assert
            result.Result.Should().BeOfType<BadRequestResult>();
        }

        /// <summary>
        /// Verifies that UpdatePortal returns 400 BadRequest when LocalizedPairs.Values is null or empty.
        /// </summary>
        [Theory]
        [InlineData(true)] // null
        [InlineData(false)] // empty
        public async Task updatePortalReturnsBadRequestIfLocalizedNameLocalizationsIsNullOrEmpty(bool isNull)
        {
            // Arrange
            var pairs = new LocalizedPairs
            {
                Key = "portal.name",
                Values = isNull ? null : new List<LocalizedValue>()
            };

            var invalidDto = new PortalUpdateDtoBuilder()
                .WithLocalizedPairs(pairs)
                .Build();

            // Act
            var result = await sut.UpdatePortal(100, 1, invalidDto);

            //Assert
            result.Result.Should().BeOfType<BadRequestResult>();
        }

        //
        // PUT /space/{spaceId}/booth/{boothId}/portal/{portalId}
        //

        /// <summary>
        /// Verifies that UpdatePortalData returns either 200 OK with portal data or 404 NotFound when repository returns null.
        /// </summary>
        [Theory]
        [InlineData(true)]  // Repository returns valid portal
        [InlineData(false)] // Repository returns null
        public async Task updatePortalDataReturnsExpectedResult(bool repoReturnsData)
        {
            // ARRANGE: Setup expected model or null
            var expected = repoReturnsData ? portalBuilder.WithId(1).WithSpaceId(100).WithBoothId(200).Build() : null;
            mockRepo.Setup(r => r.UpdatePortalAsync(100, 200, 1, defaultUpdateDto))
                    .ReturnsAsync(expected);

            // ACT
            var result = await sut.UpdatePortalData(100, 200, 1, defaultUpdateDto);

            // ASSERT
            if (repoReturnsData)
            {
                var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                ok.StatusCode.Should().Be(StatusCodes.Status200OK);
                ok.Value.Should().BeEquivalentTo(expected);
            }
            else
            {
                result.Result.Should().BeOfType<NotFoundResult>();
            }
        }

        /// <summary>
        /// Verifies that UpdatePortalData returns 400 BadRequest when the DTO is null.
        /// </summary>
        [Fact]
        public async Task updatePortalDataReturnsBadRequestIfDtoIsNull()
        {
            // ACT
            var result = await sut.UpdatePortalData(100, 200, 1, null!);

            // ASSERT
            result.Result.Should().BeOfType<BadRequestResult>();
        }

        /// <summary>
        /// Verifies that UpdatePortalData returns 500 InternalServerError when repository throws.
        /// </summary>
        [Fact]
        public Task updatePortalDataReturns500OnException()
        {
            // ARRANGE
            mockRepo.Setup(r => r.UpdatePortalAsync(100, 200, 1, defaultUpdateDto))
                    .ThrowsAsync(new Exception("booth update fail"));

            // ACT & ASSERT using helper
            return AssertThrows500(() => sut.UpdatePortalData(100, 200, 1, defaultUpdateDto), "booth update fail");
        }

        //
        // DELETE (space- and booth-level)
        //

        /// <summary>
        /// Verifies that DeletePortal (space/booth) returns 204 NoContent or 404 NotFound depending on repository return value.
        /// </summary>
        [Theory]
        [InlineData(true, false)]  // space-level delete succeeds
        [InlineData(false, false)] // space-level delete fails
        [InlineData(true, true)]   // booth-level delete succeeds
        [InlineData(false, true)]  // booth-level delete fails
        public async Task deletePortalReturnsExpectedResult(bool repoReturnsTrue, bool isBoothLevel)
        {
            // ARRANGE
            if (isBoothLevel)
                mockRepo.Setup(r => r.DeletePortalAsync(100, 200, 1)).ReturnsAsync(repoReturnsTrue);
            else
                mockRepo.Setup(r => r.DeletePortalAsync(100, 1)).ReturnsAsync(repoReturnsTrue);

            // ACT
            var result = isBoothLevel
                ? await sut.DeletePortal(100, 200, 1)
                : await sut.DeletePortal(100, 1);

            // ASSERT
            if (repoReturnsTrue)
                result.Should().BeOfType<NoContentResult>();
            else
                result.Should().BeOfType<NotFoundResult>();
        }

        /// <summary>
        /// Verifies that DeletePortal (space/booth) returns 500 InternalServerError on exception.
        /// </summary>
        [Theory]
        [InlineData(false, "delete fail")]
        [InlineData(true, "booth delete fail")]
        public async Task deletePortalReturns500OnException(bool isBoothLevel, string errorMsg)
        {
            // ARRANGE
            if (isBoothLevel)
                mockRepo.Setup(r => r.DeletePortalAsync(100, 200, 1)).ThrowsAsync(new Exception(errorMsg));
            else
                mockRepo.Setup(r => r.DeletePortalAsync(100, 1)).ThrowsAsync(new Exception(errorMsg));

            // ACT & ASSERT using helper
            if (isBoothLevel)
                await AssertThrows500(() => sut.DeletePortal(100, 200, 1), errorMsg);
            else
                await AssertThrows500(() => sut.DeletePortal(100, 1), errorMsg);
        }

        //
        // Merged Tests
        //

        /// <summary>
        /// Verifies that Create/UpdatePortal returns 400 BadRequest when the DTO is null.
        /// </summary>
        [Theory]
        [InlineData("Create")]
        [InlineData("Update")]
        public async Task portalEndpointsReturnBadRequestWhenDtoIsNull(string method)
        {
            // ACT: invoke controller with null DTO
            var result = method switch
            {
                "Create" => await sut.CreatePortal(100, null!),
                "Update" => await sut.UpdatePortal(100, 1, null!),
                _ => throw new ArgumentOutOfRangeException(nameof(method))
            };

            // ASSERT: BadRequest result
            result.Result.Should().BeOfType<BadRequestResult>();
        }

        /// <summary>
        /// Verifies that Get/UpdatePortal returns 404 NotFound when repository returns null.
        /// </summary>
        [Theory]
        [InlineData("Get")]
        [InlineData("Update")]
        public async Task portalEndpointsReturnNotFoundWhenRepositoryReturnsNull(string method)
        {
            if (method == "Get")
            {
                // ARRANGE: repository returns null
                mockRepo.Setup(r => r.GetPortalByIdAsync(100, 200))
                        .ReturnsAsync((PortalModel?)null);

                // ACT: invoke controller
                var get = await sut.GetPortalById(100, 200);

                // ASSERT: NotFound result
                get.Result.Should().BeOfType<NotFoundResult>();
            }
            else
            {
                // ARRANGE: repository returns null
                mockRepo.Setup(r => r.UpdatePortalAsync(100, 1, defaultUpdateDto))
                        .ReturnsAsync((PortalModel?)null);

                // ACT: invoke controller
                var update = await sut.UpdatePortal(100, 1, defaultUpdateDto);

                // ASSERT: NotFound result
                update.Result.Should().BeOfType<NotFoundResult>();
            }
        }

        /// <summary>
        /// Helper to assert 500 InternalServerError with a specific message.
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

        private async Task AssertThrows500(
            Func<Task<IActionResult>> action,
            string expectedMessage)
        {
            var obj = (await action()).Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            obj.Value.Should().BeEquivalentTo(new { error = expectedMessage });
        }

    }
}
