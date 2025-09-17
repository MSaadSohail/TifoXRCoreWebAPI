// <copyright file="SpaceControllerTests.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/14/2025</date>
// <summary>
// Unit tests for SpaceController covering endpoint behavior.
// </summary>

using FluentAssertions;
using GMS.TifoXRCoreWebAPI.Controllers;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GMS.TifoXRCoreWebAPI.Middleware.Exceptions;
using Moq;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;

namespace GMS.TifoXRCoreWebAPI.Tests.Controllers
{
    public class SpaceControllerTests
    {
        private readonly Mock<ISpaceRepository> repo;
        private readonly SpaceController sut;
        private readonly SpaceDtoBuilder dtoBuilder;

        public SpaceControllerTests()
        {
            // Initialize mock repository and controller under test
            repo = new Mock<ISpaceRepository>(MockBehavior.Strict);
            sut = new SpaceController(repo.Object);
            dtoBuilder = new SpaceDtoBuilder();
        }

        #region GET
        //
        // GET /api/space/{id}
        //

        /// <summary>
        /// Verifies that GetSpaceById returns 200 OK when the repository returns valid data.
        /// </summary>
        [Fact]
        public async Task Get_ReturnsOk_OnSuccess()
        {
            // ARRANGE
            var sample = new SpaceDataBuilder()
                .WithId(101)
                .WithPlatformTypeId(2)
                .WithEntityId(10)
                .WithDescription("en_us", "A cool space")
                .Build();

            repo.Setup(r => r.GetSpaceByIdAsync(101)).ReturnsAsync(sample);

            // ACT
            var result = await sut.GetSpaceById(101);

            // ASSERT
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(sample, opts => opts.WithStrictOrdering());
            repo.Verify(r => r.GetSpaceByIdAsync(101), Times.Once);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that GetSpaceById throws the correct exception
        /// when repo returns null (not found) or when id is invalid.
        /// Also checks param names and ensures repo is not called for invalid ids.
        /// </summary>
        [Theory]
        [InlineData(200, true)]  // repo returns null => not found
        [InlineData(0, false)] // invalid id => argument error
        [InlineData(-5, false)] // invalid id => argument error
        public async Task Get_ThrowsNotFoundOrArg(int id, bool repoReturnsNull)
        {
            // ARRANGE
            if (repoReturnsNull)
            {
                repo.Setup(r => r.GetSpaceByIdAsync(id))
                    .ReturnsAsync((SpaceData?)null);
            }

            // ACT & ASSERT
            if (repoReturnsNull)
            {
                var ex = await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.GetSpaceById(id));
                ex.Message.Should().ContainAny("not found", "No data", "NotFound", "Not Found");
                ex.Message.Should().Contain(nameof(SpaceController.GetSpaceById));
                repo.Verify(r => r.GetSpaceByIdAsync(id), Times.Once);
            }
            else
            {
                var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.GetSpaceById(id));
                ex.ParamName.Should().Be("id");
                ex.Message.Should().ContainAny("positive integer", "must be > 0");
                ex.Message.Should().Contain(nameof(SpaceController.GetSpaceById));
                repo.Verify(r => r.GetSpaceByIdAsync(It.IsAny<int>()), Times.Never);
            }

            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that GetSpaceById propagates repository exceptions (middleware handles at runtime).
        /// </summary>
        [Fact]
        public async Task Get_ThrowsRepositoryEx()
        {
            // ARRANGE
            repo.Setup(r => r.GetSpaceByIdAsync(9))
                .ThrowsAsync(new Exception("Simulated repository exception from GetSpaceByIdAsync"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.GetSpaceById(9));
            ex.Message.Should().Be("Simulated repository exception from GetSpaceByIdAsync");
            repo.Verify(r => r.GetSpaceByIdAsync(9), Times.Once);
            repo.VerifyNoOtherCalls();
        }

        #endregion

        #region POST
        //
        // POST /api/space
        //

        /// <summary>
        /// Ensures CreateSpace throws ArgumentNullException when the input DTO is null.
        /// </summary>
        [Fact]
        public async Task Create_ThrowsArgNull_OnNullDto()
        {
            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => sut.CreateSpace(null!));
            ex.ParamName.Should().Be("dto");
            ex.Message.Should().ContainAny("cannot be null", "Missing parameter", "is required");
            ex.Message.Should().Contain(nameof(SpaceController.CreateSpace));

            repo.Verify(r => r.CreateSpaceAsync(It.IsAny<Space>()), Times.Never);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Ensures CreateSpace throws ArgumentException when LocalizedDescription is null.
        /// (Top-level null check distinct from Values null/empty.)
        /// </summary>
        [Fact]
        public async Task Create_ThrowsArgEx_OnNullLocalizedDescription()
        {
            // ARRANGE
            var dto = dtoBuilder.Build();
            dto.LocalizedDescription = null;

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateSpace(dto));
            // controller passes paramName: nameof(dto.LocalizedDescription) -> "LocalizedDescription"
            ex.ParamName.Should().Be("LocalizedDescription");
            ex.Message.Should().ContainAny("cannot be empty", "cannot be null or empty", "Empty collection");
            ex.Message.Should().Contain(nameof(SpaceController.CreateSpace));

            repo.Verify(r => r.CreateSpaceAsync(It.IsAny<Space>()), Times.Never);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Ensures CreateSpace throws ArgumentException when LocalizedDescription.Values is null or empty.
        /// </summary>
        [Theory]
        [InlineData(true)]   // null values
        [InlineData(false)]  // empty list
        public async Task Create_ThrowsArgEx_OnInvalidLocalizedDescription(bool makeNull)
        {
            // ARRANGE
            var bad = makeNull
                ? dtoBuilder.WithNullDescriptionValues().Build()
                : dtoBuilder.WithEmptyDescriptionValues().Build();

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateSpace(bad));
            ex.ParamName.Should().Be("LocalizedDescription");
            ex.Message.Should().ContainAny("cannot be empty", "cannot be null or empty", "Empty collection");
            ex.Message.Should().Contain(nameof(SpaceController.CreateSpace));

            repo.Verify(r => r.CreateSpaceAsync(It.IsAny<Space>()), Times.Never);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies CreateSpace throws InvalidOperationException when the repository returns null
        /// (simulated persistence failure).
        /// </summary>
        [Fact]
        public async Task Create_ThrowsInvalidOp_WhenRepoReturnsNull()
        {
            // ARRANGE
            var dto = dtoBuilder.Build();
            repo.Setup(r => r.CreateSpaceAsync(dto)).ReturnsAsync((SpaceData?)null);

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateSpace(dto));
            ex.Message.Should().ContainAny("Conflict", "conflict", "failed", "could not");
            ex.Message.Should().Contain(nameof(SpaceController.CreateSpace));

            repo.Verify(r => r.CreateSpaceAsync(dto), Times.Once);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that CreateSpace propagates generic repository exceptions.
        /// </summary>
        [Fact]
        public async Task Create_ThrowsRepositoryEx()
        {
            // ARRANGE
            var dto = dtoBuilder.Build();
            repo.Setup(r => r.CreateSpaceAsync(dto)).ThrowsAsync(new Exception("Simulated repository exception from CreateSpaceAsync"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.CreateSpace(dto));
            ex.Message.Should().Be("Simulated repository exception from CreateSpaceAsync");

            repo.Verify(r => r.CreateSpaceAsync(dto), Times.Once);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that CreateSpace returns 201 Created with route and value when creation succeeds.
        /// Also explicitly asserts the 201 status code.
        /// </summary>
        [Fact]
        public async Task Create_ReturnsCreatedAt_OnSuccess()
        {
            // ARRANGE
            var dto = dtoBuilder
                .WithPlatformTypeId(3)
                .WithEntityId(22)
                .WithDescription("en_us", "Main Space")
                .Build();

            var created = new SpaceDataBuilder()
                .WithId(77)
                .FromDto(dto)
                .Build();

            repo.Setup(r => r.CreateSpaceAsync(dto)).ReturnsAsync(created);

            // ACT
            var result = await sut.CreateSpace(dto);

            // ASSERT
            var createdAt = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdAt.StatusCode.Should().Be(StatusCodes.Status201Created);
            createdAt.ActionName.Should().Be(nameof(SpaceController.GetSpaceById));
            createdAt.RouteValues["id"].Should().Be(77);
            createdAt.Value.Should().BeEquivalentTo(created, opts => opts.WithStrictOrdering());

            repo.Verify(r => r.CreateSpaceAsync(dto), Times.Once);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// SECOND “SHAPE” success case:
        /// Demonstrates robustness with different locales and nullable SKU/Link (no branch change).
        /// </summary>
        [Fact]
        public async Task Create_ReturnsCreatedAt_OnSuccess_DifferentLocales_AndNullSkuLink()
        {
            // ARRANGE: different locales + null SKU/Link
            var dto = dtoBuilder
                .WithSku(null)
                .WithLink(null)
                .WithDescription("fr_fr", "Espace principal")
                .WithDescription("es_es", "Espacio principal")
                .Build();

            var created = new SpaceDataBuilder()
                .WithId(88)
                .FromDto(dto)
                .Build();

            repo.Setup(r => r.CreateSpaceAsync(dto)).ReturnsAsync(created);

            // ACT
            var result = await sut.CreateSpace(dto);

            // ASSERT
            var createdAt = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdAt.StatusCode.Should().Be(StatusCodes.Status201Created);
            createdAt.RouteValues["id"].Should().Be(88);
            createdAt.Value.Should().BeEquivalentTo(created, opts => opts.WithStrictOrdering());

            repo.Verify(r => r.CreateSpaceAsync(dto), Times.Once);
            repo.VerifyNoOtherCalls();
        }

        #endregion

        #region PUT
        //
        // PUT /api/space/{id}
        //

        /// <summary>
        /// Ensures UpdateSpaceById throws ArgumentNullException when the DTO is null.
        /// </summary>
        [Fact]
        public async Task Update_ThrowsArgNull_OnNullDto()
        {
            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => sut.UpdateSpaceById(1, null!));
            ex.ParamName.Should().Be("spaceDto");
            ex.Message.Should().ContainAny("cannot be null", "Missing parameter", "is required");
            ex.Message.Should().Contain(nameof(SpaceController.UpdateSpaceById));

            repo.Verify(r => r.UpdateSpaceAsync(It.IsAny<int>(), It.IsAny<Space>()), Times.Never);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Ensures UpdateSpaceById throws ArgumentException for invalid id values.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task Update_ThrowsArgEx_OnInvalidId(int id)
        {
            // ARRANGE
            var dto = dtoBuilder.Build();

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.UpdateSpaceById(id, dto));
            ex.ParamName.Should().Be("id");
            ex.Message.Should().Contain("id must be a positive integer.").And.Contain(nameof(SpaceController.UpdateSpaceById));

            repo.Verify(r => r.UpdateSpaceAsync(It.IsAny<int>(), It.IsAny<Space>()), Times.Never);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Ensures UpdateSpaceById throws ArgumentException when LocalizedDescription is null.
        /// (Top-level null check distinct from Values null/empty.)
        /// </summary>
        [Fact]
        public async Task Update_ThrowsArgEx_OnNullLocalizedDescription()
        {
            // ARRANGE
            var bad = dtoBuilder.Build();
            bad.LocalizedDescription = null;

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.UpdateSpaceById(10, bad));
            // controller passes paramName: nameof(spaceDto.LocalizedDescription) -> "LocalizedDescription"
            ex.ParamName.Should().Be("LocalizedDescription");
            ex.Message.Should().ContainAny("cannot be empty", "cannot be null or empty", "Empty collection");
            ex.Message.Should().Contain(nameof(SpaceController.UpdateSpaceById));

            repo.Verify(r => r.UpdateSpaceAsync(It.IsAny<int>(), It.IsAny<Space>()), Times.Never);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Ensures UpdateSpaceById throws ArgumentException when LocalizedDescription.Values is null/empty.
        /// </summary>
        [Theory]
        [InlineData(true)]   // null values
        [InlineData(false)]  // empty values
        public async Task Update_ThrowsArgEx_OnInvalidLocalizedDescription(bool makeNull)
        {
            // ARRANGE
            var bad = makeNull
                ? dtoBuilder.WithNullDescriptionValues().Build()
                : dtoBuilder.WithEmptyDescriptionValues().Build();

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.UpdateSpaceById(10, bad));
            ex.ParamName.Should().Be("LocalizedDescription");
            ex.Message.Should().ContainAny("cannot be empty", "cannot be null or empty", "Empty collection");
            ex.Message.Should().Contain(nameof(SpaceController.UpdateSpaceById));

            repo.Verify(r => r.UpdateSpaceAsync(It.IsAny<int>(), It.IsAny<Space>()), Times.Never);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies UpdateSpaceById throws ResourceNotFoundException when the repository returns null (not found).
        /// </summary>
        [Fact]
        public async Task Update_ThrowsNotFound_WhenRepoReturnsNull()
        {
            // ARRANGE
            var dto = dtoBuilder.Build();
            repo.Setup(r => r.UpdateSpaceAsync(55, dto))
                .ReturnsAsync((SpaceData?)null);

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.UpdateSpaceById(55, dto));
            ex.Message.Should().ContainAny("not found", "No data");
            ex.Message.Should().Contain(nameof(SpaceController.UpdateSpaceById));

            repo.Verify(r => r.UpdateSpaceAsync(55, dto), Times.Once);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that UpdateSpaceById propagates repository exceptions (middleware handles at runtime).
        /// </summary>
        [Fact]
        public async Task Update_ThrowsRepositoryEx()
        {
            // ARRANGE
            var dto = dtoBuilder.Build();
            repo.Setup(r => r.UpdateSpaceAsync(9, dto))
                .ThrowsAsync(new Exception("Simulated repository exception from UpdateSpaceAsync"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.UpdateSpaceById(9, dto));
            ex.Message.Should().Be("Simulated repository exception from UpdateSpaceAsync");

            repo.Verify(r => r.UpdateSpaceAsync(9, dto), Times.Once);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that UpdateSpaceById returns 200 OK when the DTO is valid and the repository returns updated data.
        /// </summary>
        [Fact]
        public async Task Update_ReturnsOk_OnSuccess()
        {
            // ARRANGE
            var dto = dtoBuilder
                .WithPlatformTypeId(5)
                .WithEntityId(12)
                .WithDescription("en_us", "Updated")
                .Build();

            var updated = new SpaceDataBuilder().WithId(12).FromDto(dto).Build();

            repo.Setup(r => r.UpdateSpaceAsync(12, dto)).ReturnsAsync(updated);

            // ACT
            var result = await sut.UpdateSpaceById(12, dto);

            // ASSERT
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(updated, opts => opts.WithStrictOrdering());

            repo.Verify(r => r.UpdateSpaceAsync(12, dto), Times.Once);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// (Optional explicit) Verifies a specific exception type also flows (e.g., DBConcurrencyException),
        /// documenting that the controller lets repository exceptions bubble to middleware.
        /// </summary>
        [Fact]
        public async Task Update_PropagatesSpecificRepoExceptions()
        {
            // ARRANGE
            var dto = dtoBuilder.Build();
            var dbcx = new System.Data.DBConcurrencyException("Simulated DB concurrency conflict from UpdateSpaceAsync");
            repo.Setup(r => r.UpdateSpaceAsync(12, dto)).ThrowsAsync(dbcx);

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<System.Data.DBConcurrencyException>(() => sut.UpdateSpaceById(12, dto));
            ex.Message.Should().Be("Simulated DB concurrency conflict from UpdateSpaceAsync");

            repo.Verify(r => r.UpdateSpaceAsync(12, dto), Times.Once);
            repo.VerifyNoOtherCalls();
        }

        #endregion
    }
}
