// <copyright file="TeleportTableControllerTests.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>07/29/2025</date>
// <summary>Unit tests for TeleportTableController covering endpoint behavior.</summary>

using FluentAssertions;
using GMS.TifoXRCoreWebAPI.Controllers;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using GMS.TifoXRCoreWebAPI.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Data;

namespace GMS.TifoXRCoreWebAPI.Tests.Controllers
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

        #region GET
        //
        // GET /space/{spaceId}/table
        //

        /// <summary>
        /// Verifies that GetTeleportTablesBySpace returns 200 OK when the repository returns valid data,
        /// including edge cases like additional locales and a null NameKey.
        /// </summary>
        [Theory]
        [InlineData(false, false)]   // default
        [InlineData(true, false)]    // extra-locale
        [InlineData(false, true)]    // null-NameKey
        public async Task GetBySpace_ReturnsOk(bool extraLocale, bool nameKeyNull)
        {
            // ARRANGE
            var builder = new TeleportTableModelBuilder();
            if (extraLocale) builder.WithAdditionalButtonLocale("en_es", "Test");
            if (nameKeyNull) builder.WithNameKey(null!);
            var sample = builder.Build();

            repo.Setup(r => r.GetTeleportTableBySpaceAsync(100))
                .ReturnsAsync(sample);

            // ACT
            var result = await sut.GetTeleportTablesBySpace(100);

            // ASSERT
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(sample, opts => opts.WithStrictOrdering());
        }

        /// <summary>
        /// Verifies that GetTeleportTablesBySpace throws the correct exception
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
                repo.Setup(r => r.GetTeleportTableBySpaceAsync(spaceId))
                    .ReturnsAsync((TeleportTableData?)null);
            }

            // ACT & ASSERT
            if (repoReturnsNull)
            {
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () => sut.GetTeleportTablesBySpace(spaceId)
                );
                repo.Verify(r => r.GetTeleportTableBySpaceAsync(spaceId), Times.Once);
            }
            else
            {
                await Assert.ThrowsAsync<ArgumentException>(
                    () => sut.GetTeleportTablesBySpace(spaceId)
                );
            }
        }


        /// <summary>
        /// Verifies that GetTeleportTablesBySpace throws an exception
        /// when the repository throws an exception (handled by global middleware at runtime).
        /// </summary>
        [Fact]
        public async Task GetBySpaceThrows_OnRepositoryEx()
        {
            // ARRANGE
            repo.Setup(r => r.GetTeleportTableBySpaceAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Simulated repository exception"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(
                () => sut.GetTeleportTablesBySpace(300)
            );

            Assert.Equal("Simulated repository exception", ex.Message);
        }

        #endregion

        #region POST 

        /// <summary>
        /// Ensures CreateTeleportTable throws ArgumentException for invalid spaceId.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-42)]
        public async Task Create_ThrowsArgEx_OnInvalidSpaceId(int spaceId)
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(
                () => sut.CreateTeleportTable(spaceId, new TeleportTableCreateDto())
            );
        }

        /// <summary>
        /// Verifies that CreateTeleportTable throws ArgumentNullException when the input DTO is null.
        /// Ensures that the action does not process a missing payload.
        /// </summary>
        [Fact]
        public async Task Create_ThrowsArgNullEx_OnNullDto()
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => sut.CreateTeleportTable(1, null!)
            );
        }

        /// <summary>
        /// Verifies that CreateTeleportTable throws InvalidOperationException when the repository returns null,
        /// simulating a failure to persist the new teleport table.
        /// </summary>
        [Fact]
        public async Task Create_ThrowsInvalidOpEx_OnCreateFail()
        {
            // ARRANGE
            repo.Setup(r => r.CreateTeleportTableAsync(1, It.IsAny<TeleportTableCreateDto>()))
                .ReturnsAsync((TeleportTableData?)null);

            // ACT & ASSERT
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.CreateTeleportTable(1, new TeleportTableCreateDto())
            );
        }

        /// <summary>
        /// Verifies that CreateTeleportTable returns CreatedAtActionResult with the correct route and value
        /// when a new teleport table is successfully created.
        /// </summary>
        [Fact]
        public async Task Create_ReturnsCreatedAt_OnSuccess()
        {
            // ARRANGE
            var created = new TeleportTableData { Id = 42 /* ...other fields... */ };
            var dto = new TeleportTableCreateDto(); // Populate with valid test data if needed

            repo.Setup(r => r.CreateTeleportTableAsync(1, dto))
                .ReturnsAsync(created);

            // ACT
            var result = await sut.CreateTeleportTable(1, dto);

            // ASSERT
            var createdAt = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdAt.ActionName.Should().Be(nameof(TeleportTableController.GetTeleportTablesBySpace));
            createdAt.RouteValues["spaceId"].Should().Be(1);
            createdAt.RouteValues["tableId"].Should().Be(42);
            createdAt.Value.Should().BeEquivalentTo(created);
        }

        #endregion

        #region PUT
        //
        // PUT /space/{spaceId}/table/{id}
        //

        /// <summary>
        /// Verifies that UpdateTeleportTableById throws ArgumentNullException when the input DTO is null.
        /// This ensures early validation logic short-circuits invalid input.
        /// </summary>
        [Fact]
        public async Task UpdateById_ThrowsArgNullExDtoNull()
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => sut.UpdateTeleportTableById(1, 1, null!)
            );
        }

        /// <summary>
        /// Verifies that UpdateTeleportTableById returns 200 OK when the DTO is valid and the repository successfully updates the data.
        /// Also confirms that the updated result matches the expected structure.
        /// </summary>
        [Fact]
        public async Task UpdateById_ReturnsOk()
        {
            // ARRANGE
            var updated = new TeleportTableModelBuilder()
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
        /// Verifies that UpdateTeleportTableById throws the correct exception
        /// either when the repository returns null (not found)
        /// or when an invalid spaceId or tableId is used (bad request).
        /// </summary>
        [Theory]
        [InlineData(2, 2, true)]    // repo returns null
        [InlineData(-1, 1, false)]  // invalid spaceId
        [InlineData(1, -1, false)]  // invalid tableId
        public async Task UpdateById_ThrowsNotFoundOrInvalidId(int spaceId, int tableId, bool repoReturnsNull)
        {
            // ARRANGE
            if (repoReturnsNull)
            {
                repo.Setup(r => r.UpdateTeleportTableAsync(spaceId, tableId, defaultDto))
                    .ReturnsAsync((TeleportTableData?)null);
            }

            // ACT & ASSERT
            if (repoReturnsNull)
            {
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () => sut.UpdateTeleportTableById(spaceId, tableId, defaultDto)
                );
            }
            else
            {
                await Assert.ThrowsAsync<ArgumentException>(
                    () => sut.UpdateTeleportTableById(spaceId, tableId, defaultDto)
                );
            }
        }

        /// <summary>
        /// Verifies that UpdateTeleportTableById throws an exception when the repository throws,
        /// confirming general exception propagation for PUT operations.
        /// </summary>
        [Fact]
        public async Task UpdateById_ThrowsRepositoryEx()
        {
            // ARRANGE: repository throws
            repo.Setup(r => r.UpdateTeleportTableAsync(
                                It.IsAny<int>(),
                                It.IsAny<int>(),
                                It.IsAny<TeleportTableUpdateDto>()))
                .ThrowsAsync(new Exception("UpdateTeleportTableAsync encountered a database error"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(
                () => sut.UpdateTeleportTableById(3, 3, defaultDto)
            );

            Assert.Equal("UpdateTeleportTableAsync encountered a database error", ex.Message);
        }

        /// <summary>
        /// Verifies that UpdateTeleportTableById throws ArgumentException
        /// when required fields in the DTO (LocalizedPairs or Buttons) are missing.
        /// </summary>
        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public async Task UpdateById_ThrowsArgEx_InvalidDto(bool nullName, bool nullButtons)
        {
            // ARRANGE
            var builder = new TeleportTableUpdateDtoBuilder();
            
            if (nullName) builder.WithNullLocalizedPairs();
            if (nullButtons) builder.WithNullButtons();
            
            var dto = builder.Build();

            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(
                () => sut.UpdateTeleportTableById(1, 1, dto)
            );
        }

        /// <summary>
        /// Verifies that UpdateTeleportTableById throws ArgumentException when the DTO's Buttons collection is empty,
        /// assuming business logic requires at least one button.
        /// </summary>
        [Fact]
        public async Task UpdateById_ThrowsArgEx_ForEmptyButtons()
        {
            // ARRANGE
            var dto = new TeleportTableUpdateDto
            {
                IsActive = true,
                NameKey = "table_key",
                LocalizedPairs = new LocalizedPairs
                {
                    Key = "loc_key",
                    Values =
                    [
                        new() { LocaleId = "en_us", Value = "Main Hall" }
                    ]
                },
                Buttons = [] // Empty list
            };

            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(
                () => sut.UpdateTeleportTableById(1, 1, dto)
            );
        }

        /// <summary>
        /// Verifies that UpdateTeleportTableById throws ArgumentException when the DTO's LocalizedPairs collection is empty,
        /// assuming business logic requires at least one localized name.
        /// </summary>
        [Fact]
        public async Task UpdateById_ThrowsArgEx_ForEmptyLocalizedPairs()
        {
            // ARRANGE
            var dto = new TeleportTableUpdateDto
            {
                IsActive = true,
                NameKey = "table_key",
                LocalizedPairs = new LocalizedPairs
                {
                    Key = "loc_key",
                    Values = [] // Empty list
                },
                Buttons =
                [
                    new ()
                    {
                        Id = 1,
                        NameKey = "btn_key",
                        MapSpot = new MapSpotData { Id = 1, X = 0, Y = 0, Z = 0 },
                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = "btn_loc_key",
                            Values =
                            [
                                new LocalizedValue { LocaleId = "en_us", Value = "Button" }
                            ]
                        },
                        IsActive = true
                    }
                ]
            };

            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(
                () => sut.UpdateTeleportTableById(1, 1, dto)
            );
        }

        /// <summary>
        /// Verifies that UpdateTeleportTableById throws ArgumentException when a button in the DTO has an invalid (negative) ID,
        /// if such validation exists in the business logic.
        /// </summary>
        [Fact]
        public async Task UpdateById_ThrowsArgEx_ForInvalidButtonId()
        {
            // ARRANGE
            var dto = new TeleportTableUpdateDto
            {
                IsActive = true,
                NameKey = "table_key",
                LocalizedPairs = new LocalizedPairs
                {
                    Key = "loc_key",
                    Values =
                    [
                        new LocalizedValue { LocaleId = "en_us", Value = "Main Hall" }
                    ]
                },
                Buttons =
                [
                    new() {
                        Id = -5, // Invalid ID
                        NameKey = "btn_key",
                        MapSpot = new MapSpotData { Id = 1, X = 0, Y = 0, Z = 0 },
                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = "btn_loc_key",
                            Values =
                            [
                                new LocalizedValue { LocaleId = "en_us", Value = "Button" }
                            ]
                        },
                        IsActive = true
                    }
                ]
            };

            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(
                () => sut.UpdateTeleportTableById(1, 1, dto)
            );
        }

        /// <summary>
        /// Verifies that UpdateTeleportTableById throws DBConcurrencyException when the repository detects a row conflict,
        /// such as in optimistic concurrency scenarios.
        /// </summary>
        [Fact]
        public async Task UpdateById_ThrowsConcurrencyEx_OnRowConflict()
        {
            // ARRANGE
            repo.Setup(r => r.UpdateTeleportTableAsync(1, 1, defaultDto))
                .ThrowsAsync(new DBConcurrencyException("Row was modified by another process"));

            // ACT & ASSERT
            await Assert.ThrowsAsync<DBConcurrencyException>(
                () => sut.UpdateTeleportTableById(1, 1, defaultDto)
            );
        }

        #endregion

        #region DELETE

        /// <summary>
        /// Ensures DeleteTeleportTable throws ArgumentException for invalid spaceId or tableId.
        /// </summary>
        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(-1, 1)]
        [InlineData(1, -1)]
        public async Task Delete_ThrowsArgEx_OnInvalidIds(int spaceId, int tableId)
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(
                () => sut.DeleteTeleportTable(spaceId, tableId)
            );
        }

        /// <summary>
        /// Ensures DeleteTeleportTable throws KeyNotFoundException if repo returns false (not found).
        /// </summary>
        [Fact]
        public async Task Delete_ThrowsNotFound_WhenNotFound()
        {
            // ARRANGE
            repo.Setup(r => r.DeleteTeleportTableAsync(1, 2)).ReturnsAsync(false);

            // ACT & ASSERT
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => sut.DeleteTeleportTable(1, 2)
            );
        }

        /// <summary>
        /// Ensures DeleteTeleportTable returns NoContent on successful delete.
        /// </summary>
        [Fact]
        public async Task Delete_ReturnsNoContent_OnSuccess()
        {
            // ARRANGE
            repo.Setup(r => r.DeleteTeleportTableAsync(1, 2)).ReturnsAsync(true);

            // ACT
            var result = await sut.DeleteTeleportTable(1, 2);

            // ASSERT
            result.Should().BeOfType<NoContentResult>();
        }

        #endregion
    }
}
