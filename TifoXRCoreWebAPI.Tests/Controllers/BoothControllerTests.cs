// <copyright file="BoothControllerTests.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/11/2025</date>
// <summary>Unit tests for BoothController covering endpoint behavior</summary>

using FluentAssertions;
using GMS.TifoXRCoreWebAPI.Controllers;
using GMS.TifoXRCoreWebAPI.Middleware.Exceptions; // <-- for ResourceNotFoundException
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Tests.Helpers;
using GMS.TifoXRCoreWebAPI.Utilities.Logger.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;

namespace GMS.TifoXRCoreWebAPI.Tests.Controllers
{
    /// <summary>
    /// Tests the BoothController using a mocked IBoothRepository.
    /// Follows the same organization and thoroughness as TeleportTableControllerTests.
    /// </summary>
    public class BoothControllerTests
    {
        private readonly Mock<IBoothRepository> repo;
        private readonly BoothController sut;
        private readonly BoothUpdateDto defaultUpdateDto;
        private readonly BoothCreateDto defaultCreateDto;

        private readonly Mock<IDiagnosticContext> diag;
        private readonly Mock<IAppLogger<BoothController>> log;

        /// <summary>
        /// Initializes the test fixture with a mock repository, controller under test, and default DTOs.
        /// </summary>
        public BoothControllerTests()
        {
            repo = new Mock<IBoothRepository>();
            sut = new BoothController(repo.Object);

            defaultUpdateDto = new BoothUpdateDtoBuilder().Build();
            defaultCreateDto = new BoothCreateDtoBuilder().Build();

            // Serilog diagnostic context – allow any Set(...)
            diag = new Mock<Serilog.IDiagnosticContext>();
            diag.Setup(d => d.Set(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<bool>()));

            // App logger – no-op everything; provide disposable scope for WithProperties
            log = new Mock<IAppLogger<BoothController>>();
            log.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(false);
            log.Setup(l => l.WithProperties(It.IsAny<(string, object)[]>()))
               .Returns(Mock.Of<IDisposable>());

        }

        // ---------- helpers ----------
        private Task<ActionResult<List<BoothModel>>> CallGetAll(int spaceId) =>
            sut.GetAllBoothsBySpace(spaceId, diag.Object, log.Object);

        #region GET
        //
        // GET /api/space/{spaceId}/booths
        //

        /// <summary>
        /// Verifies that GetAllBoothsBySpace returns 200 OK with the correct payload
        /// when the repository returns a non-empty list of booths.
        /// </summary>
        [Fact]
        public async Task GetAllBoothsBySpace_ReturnsOk_OnSuccess()
        {
            // ARRANGE
            var list = new List<BoothModel>
            {
                new BoothModelBuilder().WithId(1).WithSpaceId(100).Build(),
                new BoothModelBuilder().WithId(2).WithSpaceId(100).Build()
            };

            repo.Setup(r => r.GetAllBoothsBySpaceAsync(100))
                .ReturnsAsync(list);

            // ACT
            var result = await CallGetAll(100);

            // ASSERT
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(list, opts => opts.WithStrictOrdering());
            repo.Verify(r => r.GetAllBoothsBySpaceAsync(100), Times.Once);
        }

        /// <summary>
        /// Verifies that GetAllBoothsBySpace throws ResourceNotFoundException when the repository
        /// returns null or an empty list (resource not found).
        /// </summary>
        /// <param name="repoReturnsNull">If true, repo returns null; otherwise an empty list.</param>
        [Theory]
        [InlineData(true)]   // repo returns null
        [InlineData(false)]  // repo returns empty list
        public async Task GetAllBoothsBySpace_ThrowsNotFound_WhenNullOrEmpty(bool repoReturnsNull)
        {
            // ARRANGE
            if (repoReturnsNull)
                repo.Setup(r => r.GetAllBoothsBySpaceAsync(200)).ReturnsAsync((List<BoothModel>?)null);
            else
                repo.Setup(r => r.GetAllBoothsBySpaceAsync(200)).ReturnsAsync(new List<BoothModel>());

            // ACT & ASSERT
            await Assert.ThrowsAsync<ResourceNotFoundException>(() => CallGetAll(200));
            repo.Verify(r => r.GetAllBoothsBySpaceAsync(200), Times.Once);
        }

        /// <summary>
        /// Verifies that GetAllBoothsBySpace throws ArgumentException for invalid spaceId values.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-42)]
        public async Task GetAllBoothsBySpace_ThrowsArgEx_OnInvalidSpaceId(int spaceId)
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() => CallGetAll(spaceId));

            // Ensure repository is not called when input is invalid
            repo.Verify(r => r.GetAllBoothsBySpaceAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Verifies that GetAllBoothsBySpace propagates exceptions thrown by the repository,
        /// to be handled by global exception handling middleware at runtime.
        /// </summary>
        [Fact]
        public async Task GetAllBoothsBySpace_ThrowsRepositoryEx()
        {
            // ARRANGE
            repo.Setup(r => r.GetAllBoothsBySpaceAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Simulated repository failure"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() => CallGetAll(321));
            ex.Message.Should().Be("Simulated repository failure");
        }

        #endregion

        #region POST
        //
        // POST /api/space/{spaceId}/booth
        //

        /// <summary>
        /// Ensures CreateBooth throws ArgumentException for invalid spaceId values.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-99)]
        public async Task CreateBooth_ThrowsArgEx_OnInvalidSpaceId(int spaceId)
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateBooth(spaceId, defaultCreateDto));

            // Ensure repository is not called when input is invalid
            repo.Verify(r => r.CreateBoothAsync(It.IsAny<int>(), It.IsAny<BoothCreateDto>()), Times.Never);
        }

        /// <summary>
        /// Ensures CreateBooth throws ArgumentNullException when the input DTO is null.
        /// </summary>
        [Fact]
        public async Task CreateBooth_ThrowsArgNullEx_OnNullDto()
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentNullException>(() => sut.CreateBooth(1, null!));

            // Ensure repository is not called when input is invalid
            repo.Verify(r => r.CreateBoothAsync(It.IsAny<int>(), It.IsAny<BoothCreateDto>()), Times.Never);
        }

        /// <summary>
        /// Verifies CreateBooth throws InvalidOperationException when the repository returns null,
        /// simulating a failure to persist the new booth.
        /// </summary>
        [Fact]
        public async Task CreateBooth_ThrowsInvalidOpEx_OnCreateFail()
        {
            // ARRANGE
            repo.Setup(r => r.CreateBoothAsync(1, It.IsAny<BoothCreateDto>()))
                .ReturnsAsync((BoothModel?)null);

            // ACT & ASSERT
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateBooth(1, defaultCreateDto));
        }

        /// <summary>
        /// Verifies that CreateBooth returns CreatedAtAction with a BoothWrapper payload
        /// when creation succeeds.
        /// </summary>
        [Fact]
        public async Task CreateBooth_ReturnsCreatedAt_OnSuccess()
        {
            // ARRANGE
            var created = new BoothModelBuilder()
                            .WithId(77)
                            .WithSpaceId(5)
                            .WithMapSpot(1.1m, 2.2m, 3.3m)
                            .WithLocalizedPair("en_us", "Booth EN")
                            .Build();

            var dto = new BoothCreateDtoBuilder()
                        .WithSpaceId(5)
                        .WithKey("booth_key")
                        .WithLocalizedPair("en_us", "Booth EN")
                        .Build();

            repo.Setup(r => r.CreateBoothAsync(5, dto)).ReturnsAsync(created);

            // ACT
            var result = await sut.CreateBooth(5, dto);

            // ASSERT
            var createdAt = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdAt.ActionName.Should().Be(nameof(BoothController.GetAllBoothsBySpace));
            createdAt.RouteValues.Should().ContainKey("spaceId").WhoseValue.Should().Be(5);

            var wrapper = createdAt.Value.Should().BeOfType<BoothWrapper>().Subject;
            wrapper.booth.Should().BeEquivalentTo(created);
        }

        #endregion

        #region PUT
        //
        // PUT /api/space/{spaceId}/booth/{boothId}
        //

        /// <summary>
        /// Ensures UpdateBooth throws ArgumentException for invalid spaceId/boothId values.
        /// </summary>
        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(-1, 2)]
        [InlineData(2, -1)]
        public async Task UpdateBooth_ThrowsArgEx_OnInvalidIds(int spaceId, int boothId)
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() => sut.UpdateBooth(spaceId, boothId, defaultUpdateDto));

            // Ensure repository is not called when input is invalid
            repo.Verify(r => r.UpdateBoothAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<BoothUpdateDto>()), Times.Never);
        }

        /// <summary>
        /// Ensures UpdateBooth throws ArgumentNullException when the input DTO is null.
        /// </summary>
        [Fact]
        public async Task UpdateBooth_ThrowsArgNullEx_OnNullDto()
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentNullException>(() => sut.UpdateBooth(1, 2, null!));

            // Ensure repository is not called when input is invalid
            repo.Verify(r => r.UpdateBoothAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<BoothUpdateDto>()), Times.Never);
        }

        /// <summary>
        /// Verifies that UpdateBooth returns 200 OK with a Response envelope containing the updated BoothModel.
        /// </summary>
        [Fact]
        public async Task UpdateBooth_ReturnsOk_OnSuccess()
        {
            // ARRANGE
            var updated = new BoothModelBuilder()
                            .WithId(10)
                            .WithSpaceId(1)
                            .WithKey("updated_key")
                            .WithMapSpot(9.9m, 8.8m, 7.7m)
                            .WithLocalizedPair("en_us", "Updated Name")
                            .Build();

            repo.Setup(r => r.UpdateBoothAsync(1, 10, defaultUpdateDto))
                .ReturnsAsync(updated);

            // ACT
            var result = await sut.UpdateBooth(1, 10, defaultUpdateDto);

            // ASSERT
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);

            var envelope = ok.Value.Should().BeOfType<Response>().Subject;
            envelope.Booth.Should().BeEquivalentTo(updated);
            repo.Verify(r => r.UpdateBoothAsync(1, 10, defaultUpdateDto), Times.Once);
        }

        /// <summary>
        /// Verifies that UpdateBooth throws ResourceNotFoundException when the repository returns null (not found).
        /// </summary>
        [Fact]
        public async Task UpdateBooth_ThrowsNotFound_WhenRepoReturnsNull()
        {
            // ARRANGE
            repo.Setup(r => r.UpdateBoothAsync(1, 999, It.IsAny<BoothUpdateDto>()))
                .ReturnsAsync((BoothModel?)null);

            // ACT & ASSERT
            await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.UpdateBooth(1, 999, defaultUpdateDto));
        }

        /// <summary>
        /// Verifies that UpdateBooth propagates a repository exception (handled by middleware at runtime).
        /// </summary>
        [Fact]
        public async Task UpdateBooth_ThrowsRepositoryEx()
        {
            // ARRANGE
            repo.Setup(r => r.UpdateBoothAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<BoothUpdateDto>()))
                .ThrowsAsync(new Exception("Failed to update booth record in the database"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.UpdateBooth(3, 4, defaultUpdateDto));
            ex.Message.Should().Be("Failed to update booth record in the database");
        }

        /// <summary>
        /// Verifies controller behavior when LocalizedPairs is null in the incoming DTO.
        /// and asserting the controller propagates it.
        /// </summary>
        [Fact]
        public async Task UpdateBooth_ThrowsArgEx_WhenLocalizedPairsIsNull()
        {
            // ARRANGE
            var dto = new BoothUpdateDtoBuilder()
                        .WithNullLocalizedPairs()
                        .Build();

            repo.Setup(r => r.UpdateBoothAsync(1, 1, dto))
                .ThrowsAsync(new ArgumentException("LocalizedPairs cannot be null."));

            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() => sut.UpdateBooth(1, 1, dto));
        }

        /// <summary>
        /// Verifies controller behavior when MapSpot is null in the incoming DTO.
        /// The repository throws an ArgumentException and the controller propagates it.
        /// </summary>
        [Fact]
        public async Task UpdateBooth_ThrowsArgEx_WhenMapSpotIsNull()
        {
            // ARRANGE
            var dto = new BoothUpdateDtoBuilder()
                        .WithNullMapSpot()
                        .Build();

            repo.Setup(r => r.UpdateBoothAsync(1, 1, dto))
                .ThrowsAsync(new ArgumentException("MapSpot cannot be null."));

            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() => sut.UpdateBooth(1, 1, dto));
        }

        #endregion

        #region DELETE
        //
        // DELETE /api/space/{spaceId}/booth/{boothId}
        //

        /// <summary>
        /// Ensures DeleteBoothCascade throws ArgumentException for invalid spaceId/boothId values.
        /// </summary>
        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(-1, 1)]
        [InlineData(1, -1)]
        public async Task DeleteBoothCascade_ThrowsArgEx_OnInvalidIds(int spaceId, int boothId)
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() => sut.DeleteBoothCascade(spaceId, boothId));

            // Ensure repository is not called when input is invalid
            repo.Verify(r => r.DeleteBoothCascadeAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Ensures DeleteBoothCascade throws ResourceNotFoundException when repository returns false (not found).
        /// </summary>
        [Fact]
        public async Task DeleteBoothCascade_ThrowsNotFound_WhenRepoReturnsFalse()
        {
            // ARRANGE
            repo.Setup(r => r.DeleteBoothCascadeAsync(1, 2)).ReturnsAsync(false);

            // ACT & ASSERT
            await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.DeleteBoothCascade(1, 2));
        }

        /// <summary>
        /// Ensures DeleteBoothCascade returns 204 NoContent upon successful deletion.
        /// </summary>
        [Fact]
        public async Task DeleteBoothCascade_ReturnsNoContent_OnSuccess()
        {
            // ARRANGE
            repo.Setup(r => r.DeleteBoothCascadeAsync(5, 6)).ReturnsAsync(true);

            // ACT
            var result = await sut.DeleteBoothCascade(5, 6);

            // ASSERT
            result.Should().BeOfType<NoContentResult>();
            repo.Verify(r => r.DeleteBoothCascadeAsync(5, 6), Times.Once);
        }

        /// <summary>
        /// Verifies that DeleteBoothCascade propagates repository exceptions (middleware handles at runtime).
        /// </summary>
        [Fact]
        public async Task DeleteBoothCascade_ThrowsRepositoryEx()
        {
            // ARRANGE
            repo.Setup(r => r.DeleteBoothCascadeAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Cascade delete failed"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.DeleteBoothCascade(9, 9));
            ex.Message.Should().Be("Cascade delete failed");
        }

        #endregion
    }
}
