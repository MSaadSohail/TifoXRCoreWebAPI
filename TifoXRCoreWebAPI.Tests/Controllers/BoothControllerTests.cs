// <copyright file="BoothControllerTests.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/08/2025</date>
// <summary>Unit tests for BoothController covering endpoint behavior.</summary>

using FluentAssertions;
using GMS.TifoXRCoreWebAPI.Controllers;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using GMS.TifoXRCoreWebAPI.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GMS.TifoXRCoreWebAPI.Tests.Controllers
{
    public class BoothControllerTests
    {
        private readonly Mock<IBoothRepository> repo;
        private readonly BoothController sut;
        private readonly BoothUpdateDto defaultUpdateDto;
        private readonly BoothCreateDto defaultCreateDto;
        private readonly BoothModelBuilder boothBuilder;

        public BoothControllerTests()
        {
            // Initialize mock repository and controller under test
            repo = new Mock<IBoothRepository>();
            sut = new BoothController(repo.Object);
            defaultUpdateDto = new BoothUpdateDtoBuilder().Build();
            defaultCreateDto = new BoothCreateDtoBuilder().Build();
            boothBuilder = new BoothModelBuilder();
        }

        #region GET
        //
        // GET /space/{spaceId}/booths
        //

        /// <summary>
        /// Verifies that GetAllBoothsBySpace returns 200 OK when the repository returns valid data.
        /// </summary>
        [Fact]
        public async Task GetBySpace_ReturnsOk()
        {
            // ARRANGE
            var sample = new List<BoothModel> { boothBuilder.WithId(1).Build() };

            repo.Setup(r => r.GetAllBoothsBySpaceAsync(100))
                .ReturnsAsync(sample);

            // ACT
            var result = await sut.GetAllBoothsBySpace(100);

            // ASSERT
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(sample, opts => opts.WithStrictOrdering());
        }

        /// <summary>
        /// Verifies that GetAllBoothsBySpace throws the correct exception
        /// when the repository returns null (resource not found)
        /// or when an invalid spaceId (e.g., negative) is passed.
        /// </summary>
        [Theory]
        [InlineData(200, true)]   // repo returns null => not found
        [InlineData(-1, false)]   // invalid id => argument error
        public async Task GetBySpace_ThrowsNotFoundOrArg(int spaceId, bool repoReturnsNull)
        {
            // ARRANGE
            if (repoReturnsNull)
            {
                repo.Setup(r => r.GetAllBoothsBySpaceAsync(spaceId))
                    .ReturnsAsync((List<BoothModel>?)null);
            }

            // ACT & ASSERT
            if (repoReturnsNull)
            {
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () => sut.GetAllBoothsBySpace(spaceId)
                );
                repo.Verify(r => r.GetAllBoothsBySpaceAsync(spaceId), Times.Once);
            }
            else
            {
                await Assert.ThrowsAsync<ArgumentException>(
                    () => sut.GetAllBoothsBySpace(spaceId)
                );
            }
        }

        /// <summary>
        /// Verifies that GetAllBoothsBySpace throws an exception
        /// when the repository throws an exception (handled by global middleware at runtime).
        /// </summary>
        [Fact]
        public async Task GetBySpaceThrows_OnRepositoryEx()
        {
            // ARRANGE
            repo.Setup(r => r.GetAllBoothsBySpaceAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Simulated repository exception"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(
                () => sut.GetAllBoothsBySpace(300)
            );

            Assert.Equal("Simulated repository exception", ex.Message);
        }

        #endregion

        #region POST 

        /// <summary>
        /// Ensures CreateBooth throws ArgumentException for invalid spaceId.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-42)]
        public async Task Create_ThrowsArgEx_OnInvalidSpaceId(int spaceId)
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(
                () => sut.CreateBooth(spaceId, defaultCreateDto)
            );
        }

        /// <summary>
        /// Verifies that CreateBooth throws ArgumentNullException when the input DTO is null.
        /// Ensures that the action does not process a missing payload.
        /// </summary>
        [Fact]
        public async Task Create_ThrowsArgNullEx_OnNullDto()
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => sut.CreateBooth(1, null!)
            );
        }

        /// <summary>
        /// Verifies that CreateBooth throws InvalidOperationException when the repository returns null,
        /// simulating a failure to persist the new booth.
        /// </summary>
        [Fact]
        public async Task Create_ThrowsInvalidOpEx_OnCreateFail()
        {
            // ARRANGE
            repo.Setup(r => r.CreateBoothAsync(1, defaultCreateDto))
                .ReturnsAsync((BoothModel?)null);

            // ACT & ASSERT
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.CreateBooth(1, defaultCreateDto)
            );
        }

        /// <summary>
        /// Verifies that CreateBooth returns CreatedAtActionResult with the correct route and value
        /// when a new booth is successfully created.
        /// </summary>
        [Fact]
        public async Task Create_ReturnsCreatedAt_OnSuccess()
        {
            // ARRANGE
            var created = boothBuilder.WithId(42).Build();

            repo.Setup(r => r.CreateBoothAsync(1, defaultCreateDto))
                .ReturnsAsync(created);

            // ACT
            var result = await sut.CreateBooth(1, defaultCreateDto);

            // ASSERT
            var createdAt = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdAt.ActionName.Should().Be(nameof(BoothController.GetAllBoothsBySpace));
            createdAt.RouteValues["spaceId"].Should().Be(1);
            createdAt.Value.Should().BeEquivalentTo(new BoothWrapper { booth = created });
        }

        #endregion

        #region PUT
        //
        // PUT /space/{spaceId}/booth/{id}
        //

        /// <summary>
        /// Verifies that UpdateBooth throws ArgumentNullException when the input DTO is null.
        /// This ensures early validation logic short-circuits invalid input.
        /// </summary>
        [Fact]
        public async Task UpdateById_ThrowsArgNullExDtoNull()
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => sut.UpdateBooth(1, 1, null!)
            );
        }

        /// <summary>
        /// Verifies that UpdateBooth returns 200 OK when the DTO is valid and the repository successfully updates the data.
        /// Also confirms that the updated result matches the expected structure.
        /// </summary>
        [Fact]
        public async Task UpdateById_ReturnsOk()
        {
            // ARRANGE
            var updated = boothBuilder
                .WithId(1)
                .WithSpaceId(1)
                .WithLocalizedPairsKey("booth_key")
                .Build();

            repo.Setup(r => r.UpdateBoothAsync(1, 1, defaultUpdateDto))
                .ReturnsAsync(updated);

            // ACT
            var result = await sut.UpdateBooth(1, 1, defaultUpdateDto);

            // ASSERT
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(new Response { Booth = updated });
            repo.Verify(r => r.UpdateBoothAsync(1, 1, defaultUpdateDto), Times.Once);
        }

        /// <summary>
        /// Verifies that UpdateBooth throws the correct exception
        /// either when the repository returns null (not found)
        /// or when an invalid spaceId or boothId is used (bad request).
        /// </summary>
        [Theory]
        [InlineData(2, 2, true)]    // repo returns null
        [InlineData(-1, 1, false)]  // invalid spaceId
        [InlineData(1, -1, false)]  // invalid boothId
        public async Task UpdateById_ThrowsNotFoundOrInvalidId(int spaceId, int boothId, bool repoReturnsNull)
        {
            // ARRANGE
            if (repoReturnsNull)
            {
                repo.Setup(r => r.UpdateBoothAsync(spaceId, boothId, defaultUpdateDto))
                    .ReturnsAsync((BoothModel?)null);
            }

            // ACT & ASSERT
            if (repoReturnsNull)
            {
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () => sut.UpdateBooth(spaceId, boothId, defaultUpdateDto)
                );
            }
            else
            {
                await Assert.ThrowsAsync<ArgumentException>(
                    () => sut.UpdateBooth(spaceId, boothId, defaultUpdateDto)
                );
            }
        }

        /// <summary>
        /// Verifies that UpdateBooth throws an exception when the repository throws,
        /// confirming general exception propagation for PUT operations.
        /// </summary>
        [Fact]
        public async Task UpdateById_ThrowsRepositoryEx()
        {
            // ARRANGE: repository throws
            repo.Setup(r => r.UpdateBoothAsync(
                                It.IsAny<int>(),
                                It.IsAny<int>(),
                                It.IsAny<BoothUpdateDto>()))
                .ThrowsAsync(new Exception("UpdateBoothAsync encountered a database error"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(
                () => sut.UpdateBooth(3, 3, defaultUpdateDto)
            );

            Assert.Equal("UpdateBoothAsync encountered a database error", ex.Message);
        }

        #endregion

        #region DELETE

        /// <summary>
        /// Ensures DeleteBooth throws ArgumentException for invalid spaceId or boothId.
        /// </summary>
        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(-1, 1)]
        [InlineData(1, -1)]
        public async Task Delete_ThrowsArgEx_OnInvalidIds(int spaceId, int boothId)
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(
                () => sut.DeleteBoothCascade(spaceId, boothId)
            );
        }

        /// <summary>
        /// Ensures DeleteBooth throws KeyNotFoundException if repo returns false (not found).
        /// </summary>
        [Fact]
        public async Task Delete_ThrowsNotFound_WhenNotFound()
        {
            // ARRANGE
            repo.Setup(r => r.DeleteBoothCascadeAsync(1, 2)).ReturnsAsync(false);

            // ACT & ASSERT
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => sut.DeleteBoothCascade(1, 2)
            );
        }

        /// <summary>
        /// Ensures DeleteBooth returns NoContent on successful delete.
        /// </summary>
        [Fact]
        public async Task Delete_ReturnsNoContent_OnSuccess()
        {
            // ARRANGE
            repo.Setup(r => r.DeleteBoothCascadeAsync(1, 2)).ReturnsAsync(true);

            // ACT
            var result = await sut.DeleteBoothCascade(1, 2);

            // ASSERT
            result.Should().BeOfType<NoContentResult>();
        }

        #endregion
    }
}
