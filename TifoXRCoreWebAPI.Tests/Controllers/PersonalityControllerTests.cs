// <copyright file="PersonalityControllerTests.cs" company="Global Mobile Software">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/13/2025</date>
// <summary>Unit tests for PersonalityController covering endpoint behavior</summary>

using FluentAssertions;
using GMS.TifoXRCoreWebAPI.Controllers;
using GMS.TifoXRCoreWebAPI.Middleware.Exceptions;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GMS.TifoXRCoreWebAPI.Tests.Controllers
{
    public class PersonalityControllerTests
    {
        private readonly Mock<IPersonalityRepository> repo;
        private readonly PersonalityController sut;
        private readonly PersonalityCreateDtoBuilder dtoBuilder;

        public PersonalityControllerTests()
        {
            repo = new Mock<IPersonalityRepository>(MockBehavior.Strict);
            sut = new PersonalityController(repo.Object);
            dtoBuilder = new PersonalityCreateDtoBuilder();
        }

        #region GET
        //
        // GET /api/personality/{id}
        //

        /// <summary>
        /// Verifies that GetPersonalityById returns 200 OK when the repository returns valid data.
        /// </summary>
        [Fact]
        public async Task Get_ReturnsOk_OnSuccess()
        {
            // ARRANGE
            var sample = new PersonalityDataBuilder()
                .WithId(101)
                .WithName("Alex Morgan")
                .WithCountry("en_us", "United States")
                .WithBio("en_us", "Forward")
                .WithMedia("en_us", "https://cdn/media/alex.jpg")
                .Build();

            repo.Setup(r => r.GetPersonalityByIdAsync(101))
                .ReturnsAsync(sample);

            // ACT
            var result = await sut.GetPersonalityById(101);

            // ASSERT
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(sample, opts => opts.WithStrictOrdering());

            repo.Verify(r => r.GetPersonalityByIdAsync(101), Times.Once);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that GetPersonalityById throws ResourceNotFoundException when repo returns null,
        /// and ArgumentException when id is invalid (<= 0).
        /// </summary>
        [Theory]
        [InlineData(200, true)]  // repo returns null -> not found
        [InlineData(0, false)]   // invalid id -> arg error
        [InlineData(-7, false)]  // invalid id -> arg error
        public async Task Get_ThrowsNotFoundOrArg(int id, bool repoReturnsNull)
        {
            // ARRANGE
            if (repoReturnsNull)
            {
                repo.Setup(r => r.GetPersonalityByIdAsync(id))
                    .ReturnsAsync((PersonalityData?)null);
            }

            // ACT & ASSERT
            if (repoReturnsNull)
            {
                await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.GetPersonalityById(id));
                repo.Verify(r => r.GetPersonalityByIdAsync(id), Times.Once);
            }
            else
            {
                await Assert.ThrowsAsync<ArgumentException>(() => sut.GetPersonalityById(id));
                repo.Verify(r => r.GetPersonalityByIdAsync(It.IsAny<int>()), Times.Never);
            }

            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that GetPersonalityById propagates repository exceptions.
        /// </summary>
        [Fact]
        public async Task Get_ThrowsRepositoryEx()
        {
            // ARRANGE
            repo.Setup(r => r.GetPersonalityByIdAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("GetPersonalityByIdAsync encountered a database error"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.GetPersonalityById(9));
            ex.Message.Should().Be("GetPersonalityByIdAsync encountered a database error");

            repo.Verify(r => r.GetPersonalityByIdAsync(9), Times.Once);
            repo.VerifyNoOtherCalls();
        }

        #endregion

        #region POST
        //
        // POST /api/personality
        //

        /// <summary>
        /// Ensures CreatePersonality throws ArgumentNullException when the input DTO is null.
        /// </summary>
        [Fact]
        public async Task Create_ThrowsArgNull_OnNullDto()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => sut.CreatePersonality(null!));
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies CreatePersonality throws InvalidOperationException when the repository returns null.
        /// </summary>
        [Fact]
        public async Task Create_ThrowsInvalidOp_WhenRepoReturnsNull()
        {
            // ARRANGE
            var dto = dtoBuilder
                .WithName("Erling Haaland")
                .WithSpaceId(10)
                .WithCountry("en_us", "Norway")
                .WithBio("en_us", "Striker")
                .Build();

            repo.Setup(r => r.CreatePersonalityAsync(dto))
                .ReturnsAsync((PersonalityData?)null);

            // ACT & ASSERT
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreatePersonality(dto));

            repo.Verify(r => r.CreatePersonalityAsync(dto), Times.Once);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies CreatePersonality propagates repository exceptions.
        /// </summary>
        [Fact]
        public async Task Create_ThrowsRepositoryEx()
        {
            // ARRANGE
            var dto = dtoBuilder
                .WithName("Aitana Bonmatí")
                .WithSpaceId(5)
                .WithCountry("en_es", "España")
                .WithBio("en_es", "Centrocampista")
                .Build();

            repo.Setup(r => r.CreatePersonalityAsync(dto))
                .ThrowsAsync(new Exception("CreatePersonalityAsync encountered a database error"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.CreatePersonality(dto));
            ex.Message.Should().Be("CreatePersonalityAsync encountered a database error");

            repo.Verify(r => r.CreatePersonalityAsync(dto), Times.Once);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that CreatePersonality returns CreatedAtAction with correct route and value on success.
        /// </summary>
        [Fact]
        public async Task Create_ReturnsCreatedAt_OnSuccess()
        {
            // ARRANGE
            var dto = dtoBuilder
                .WithName("Marta")
                .WithSpaceId(7)
                .WithCountry("en_us", "Brazil")
                .WithBio("en_us", "Legend")
                .Build();

            var created = new PersonalityDataBuilder()
                .WithId(77)
                .WithName(dto.Name)
                .WithCountry("en_us", "Brazil")
                .WithBio("en_us", "Legend")
                .Build();

            repo.Setup(r => r.CreatePersonalityAsync(dto))
                .ReturnsAsync(created);

            // ACT
            var result = await sut.CreatePersonality(dto);

            // ASSERT
            var createdAt = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdAt.ActionName.Should().Be(nameof(PersonalityController.GetPersonalityById));
            createdAt.RouteValues["id"].Should().Be(77);
            createdAt.Value.Should().BeEquivalentTo(created, opts => opts.WithStrictOrdering());

            repo.Verify(r => r.CreatePersonalityAsync(dto), Times.Once);
            repo.VerifyNoOtherCalls();
        }

        /// <summary>
        /// - DTO has NO media (null)
        /// - Localizations use different locales than the default
        /// </summary>
        [Fact]
        public async Task Create_ReturnsCreatedAt_OnSuccess_NoMedia_AndDifferentLocales()
        {
            // ARRANGE: different locales + explicitly no media
            var dto = dtoBuilder
                .WithName("Pelé")
                .WithSpaceId(3)
                .WithoutMedia()
                .WithCountry("de_de", "Deutschland")
                .WithBio("pt_br", "Atacante")
                .Build();

            // Repository returns the created record (as controller would echo)
            var created = new PersonalityDataBuilder()
                .WithId(88)
                .WithName(dto.Name)
                .WithCountry("de_de", "Deutschland")
                .WithBio("pt_br", "Atacante")
                .WithoutMedia()
                .Build();

            // Explicitly ensure the builder used above doesn't inject media implicitly
            created.Media.Should().BeNull();

            repo.Setup(r => r.CreatePersonalityAsync(dto))
                .ReturnsAsync(created);

            // ACT
            var result = await sut.CreatePersonality(dto);

            // ASSERT
            var createdAt = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdAt.StatusCode.Should().Be(StatusCodes.Status201Created);
            createdAt.ActionName.Should().Be(nameof(PersonalityController.GetPersonalityById));
            createdAt.RouteValues["id"].Should().Be(88);
            createdAt.Value.Should().BeEquivalentTo(created, opts => opts.WithStrictOrdering());

            repo.Verify(r => r.CreatePersonalityAsync(dto), Times.Once);
            repo.VerifyNoOtherCalls();
        }

        #endregion
    }
}
