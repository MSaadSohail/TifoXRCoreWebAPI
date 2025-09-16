// <copyright file="EntityControllerTests.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/12/2025</date>
// <summary>Unit tests for EntityController covering endpoint behavior.</summary>

using FluentAssertions;
using GMS.TifoXRCoreWebAPI.Controllers;
using GMS.TifoXRCoreWebAPI.Middleware.Exceptions;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using GMS.TifoXRCoreWebAPI.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;

namespace GMS.TifoXRCoreWebAPI.Tests.Controllers
{
    /// <summary>
    /// Tests the EntityController using a mocked IEntityRepository.
    /// Organized similarly to TeleportTableControllerTests (GET/POST/PUT regions, explicit route notes).
    /// </summary>
    public class EntityControllerTests
    {
        private readonly Mock<IEntityRepository> repo;
        private readonly Mock<ILogger<EntityController>> logger;
        private readonly EntityController sut;
        private readonly EntityDtoBuilder dtoBuilder;

        /// <summary>
        /// Initializes the test fixture with a mock repository, controller under test, and default DTO builder.
        /// </summary>
        public EntityControllerTests()
        {
            repo = new Mock<IEntityRepository>();
            logger = new Mock<ILogger<EntityController>>();
            sut = new EntityController(repo.Object);
            dtoBuilder = new EntityDtoBuilder();
        }

        #region GET
        //
        // GET /api/entity/{id}
        //

        /// <summary>
        /// Verifies that Get returns 200 OK when the repository returns valid data,
        /// including typical localization fields (name + description).
        /// </summary>
        [Fact]
        public async Task Get_ReturnsOk_OnSuccess()
        {
            // ARRANGE
            var sample = new EntityModelBuilder()
                .WithId(101)
                .WithEntityTypeId(2)
                .WithName("en_us", "Main Hall")
                .WithDescription("en_us", "Desc")
                .Build();

            repo.Setup(r => r.GetByIdAsync(101)).ReturnsAsync(sample);

            // ACT
            var result = await sut.Get(101);

            // ASSERT
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(sample, opts => opts.WithStrictOrdering());
            repo.Verify(r => r.GetByIdAsync(101), Times.Once);
        }

        /// <summary>
        /// Verifies that Get throws the correct exception
        /// when the repository returns null (resource not found)
        /// or when an invalid id (e.g., non-positive) is passed.
        /// </summary>
        /// <param name="id">Entity id to request.</param>
        /// <param name="repoReturnsNull">If true, repo returns null; otherwise we pass invalid id.</param>
        [Theory]
        [InlineData(200, true)]  // repo returns null => not found
        [InlineData(0, false)]   // invalid id => argument error
        [InlineData(-7, false)]  // invalid id => argument error
        public async Task Get_ThrowsNotFoundOrArg(int id, bool repoReturnsNull)
        {
            // ARRANGE
            if (repoReturnsNull)
                repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((EntityData?)null);

            // ACT & ASSERT
            if (repoReturnsNull)
            {
                await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.Get(id));
                repo.Verify(r => r.GetByIdAsync(id), Times.Once);
            }
            else
            {
                await Assert.ThrowsAsync<ArgumentException>(() => sut.Get(id));
                repo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
            }
        }

        /// <summary>
        /// Verifies that Get propagates a repository exception (handled by middleware at runtime).
        /// </summary>
        [Fact]
        public async Task Get_ThrowsRepositoryEx()
        {
            // ARRANGE
            repo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Simulated repository failure during entity retrieval"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.Get(9));
            ex.Message.Should().Be("Simulated repository failure during entity retrieval");
        }

        #endregion

        #region POST
        //
        // POST /api/entity
        //

        /// <summary>
        /// Ensures Create throws ArgumentNullException when the input DTO is null.
        /// </summary>
        [Fact]
        public async Task Create_ThrowsArgNull_OnNullDto()
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentNullException>(() => sut.Create(null!));
            repo.Verify(r => r.CreateAsync(It.IsAny<Entity>()), Times.Never);
        }

        /// <summary>
        /// Ensures Create throws ArgumentException when LocalizedPairs.Values is empty.
        /// </summary>
        [Fact]
        public async Task Create_ThrowsArgEx_OnEmptyLocalizedPairs()
        {
            // ARRANGE
            var bad = dtoBuilder.WithEmptyNameValues().Build();

            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() => sut.Create(bad));
            repo.Verify(r => r.CreateAsync(It.IsAny<Entity>()), Times.Never);
        }

        /// <summary>
        /// Verifies Create throws InvalidOperationException when the repository returns null,
        /// simulating a failure to persist the new entity.
        /// </summary>
        [Fact]
        public async Task Create_ThrowsInvalidOp_WhenRepoReturnsNull()
        {
            // ARRANGE
            var dto = dtoBuilder.Build();
            repo.Setup(r => r.CreateAsync(dto)).ReturnsAsync((EntityData?)null);

            // ACT & ASSERT
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Create(dto));
            repo.Verify(r => r.CreateAsync(dto), Times.Once);
        }

        /// <summary>
        /// Verifies that Create propagates generic repository exceptions.
        /// </summary>
        [Fact]
        public async Task Create_ThrowsRepositoryEx()
        {
            // ARRANGE
            var dto = dtoBuilder.Build();
            repo.Setup(r => r.CreateAsync(dto)).ThrowsAsync(new Exception("Simulated repository failure during entity creation"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.Create(dto));
            ex.Message.Should().Be("Simulated repository failure during entity creation");
            repo.Verify(r => r.CreateAsync(dto), Times.Once);
        }

        /// <summary>
        /// Verifies that Create returns CreatedAtActionResult with the correct route and value
        /// when a new entity is successfully created.
        /// </summary>
        [Fact]
        public async Task Create_ReturnsCreatedAt_OnSuccess()
        {
            // ARRANGE
            var dto = dtoBuilder.WithSpaceId(5).WithEntityTypeId(3).Build();
            var created = new EntityModelBuilder()
                .WithId(77)
                .FromDto(dto)
                .Build();

            repo.Setup(r => r.CreateAsync(dto)).ReturnsAsync(created);

            // ACT
            var result = await sut.Create(dto);

            // ASSERT
            var createdAt = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdAt.ActionName.Should().Be(nameof(EntityController.Get));
            createdAt.RouteValues["id"].Should().Be(77);
            createdAt.Value.Should().BeEquivalentTo(created, opts => opts.WithStrictOrdering());
            repo.Verify(r => r.CreateAsync(dto), Times.Once);
        }

        #endregion

        #region PUT
        //
        // PUT /api/entity/{id}
        //

        /// <summary>
        /// Ensures Update throws ArgumentNullException when the input DTO is null.
        /// </summary>
        [Fact]
        public async Task Update_ThrowsArgNull_OnNullDto()
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentNullException>(() => sut.Update(1, null!));
            repo.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Entity>()), Times.Never);
        }

        /// <summary>
        /// Ensures Update throws ArgumentException for invalid id values.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task Update_ThrowsArgEx_OnInvalidId(int id)
        {
            // ARRANGE
            var dto = dtoBuilder.Build();

            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() => sut.Update(id, dto));
            repo.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Entity>()), Times.Never);
        }

        /// <summary>
        /// Ensures Update throws ArgumentException when LocalizedPairs.Values is empty.
        /// </summary>
        [Fact]
        public async Task Update_ThrowsArgEx_OnEmptyLocalizedPairs()
        {
            // ARRANGE
            var dto = dtoBuilder.WithEmptyNameValues().Build();

            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() => sut.Update(10, dto));
            repo.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Entity>()), Times.Never);
        }

        /// <summary>
        /// Verifies that Update throws ResourceNotFoundException when the repository returns null (not found).
        /// </summary>
        [Fact]
        public async Task Update_ThrowsNotFound_WhenRepoReturnsNull()
        {
            // ARRANGE
            var dto = dtoBuilder.Build();
            repo.Setup(r => r.UpdateAsync(55, dto)).ReturnsAsync((EntityData?)null);

            // ACT & ASSERT
            await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.Update(55, dto));
            repo.Verify(r => r.UpdateAsync(55, dto), Times.Once);
        }

        /// <summary>
        /// Verifies that Update propagates a repository exception (handled by middleware at runtime).
        /// </summary>
        [Fact]
        public async Task Update_ThrowsRepositoryEx()
        {
            // ARRANGE
            var dto = dtoBuilder.Build();
            repo.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Entity>()))
                .ThrowsAsync(new Exception("Simulated repository failure during entity update"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.Update(9, dto));
            ex.Message.Should().Be("Simulated repository failure during entity update");
        }

        /// <summary>
        /// Verifies that Update bubbles DBConcurrencyException when the repo detects a row conflict.
        /// </summary>
        [Fact]
        public async Task Update_ThrowsConcurrencyEx_OnRowConflict()
        {
            // ARRANGE
            var dto = dtoBuilder.Build();
            repo.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Entity>()))
                .ThrowsAsync(new DBConcurrencyException("Simulated concurrency conflict during entity update: row was changed by another user"));

            // ACT & ASSERT
            await Assert.ThrowsAsync<DBConcurrencyException>(() => sut.Update(12, dto));
        }

        /// <summary>
        /// Verifies that Update returns 200 OK when the DTO is valid and the repository successfully updates the data.
        /// </summary>
        [Fact]
        public async Task Update_ReturnsOk_OnSuccess()
        {
            // ARRANGE
            var dto = dtoBuilder.WithEntityTypeId(9).WithName("en_us", "Updated").Build();
            var updated = new EntityModelBuilder().WithId(12).FromDto(dto).Build();

            repo.Setup(r => r.UpdateAsync(12, dto)).ReturnsAsync(updated);

            // ACT
            var result = await sut.Update(12, dto);

            // ASSERT
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(updated, opts => opts.WithStrictOrdering());
            repo.Verify(r => r.UpdateAsync(12, dto), Times.Once);
        }

        #endregion
    }
}
