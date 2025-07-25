// <copyright file="TeleportControllerTests.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>07/23/2025</date>
// <summary>Unit tests for TeleportController covering endpoint behavior, response structure,and validation logic using mocked repository and FluentAssertions.</summary>

using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TifoXRCoreWebAPI.Controllers;
using TifoXRCoreWebAPI.Models;
using TifoXRCoreWebAPI.Models.Common;
using TifoXRCoreWebAPI.Repositories.Interfaces;

namespace TifoXRCoreWebAPI.Tests.Controllers
{
    public class CustomAutoDataAttribute : AutoDataAttribute
    {
        public CustomAutoDataAttribute() : base(() =>
            new Fixture().Customize(new AutoMoqCustomization()))
        { }
    }

    public class InlineCustomAutoDataAttribute : InlineAutoDataAttribute
    {
        public InlineCustomAutoDataAttribute(params object[] values)
            : base(new CustomAutoDataAttribute(), values) { }
    }

    public class TeleportTableControllerTests
    {
        private readonly Mock<ITeleportTableRepository> repoMock;
        private readonly TeleportTableController sut;

        public TeleportTableControllerTests()
        {
            repoMock = new Mock<ITeleportTableRepository>();
            sut = new TeleportTableController(repoMock.Object);
        }

        private static TeleportTableData CreateSampleTeleportTable(int spaceId = 100, int tableId = 1) =>
            new TeleportTableData
            {
                Id = tableId,
                SpaceId = spaceId,
                NameKey = "table_key",
                IsActive = true,
                LocalizedName = new Dictionary<string, string>
                {
                    { "en", "Start" }
                },
                Buttons = new List<ButtonData>
                {
                    new()
                    {
                        Id = 1,
                        NameKey = "btn_key",
                        BoothToVisit = 10,
                        LocalizedName = new Dictionary<string, string>
                        {
                            { "en", "Go" }
                        }
                    }
                }
            };

        private static TeleportTableUpdateDto CreateValidUpdateDto() =>
            new TeleportTableUpdateDto
            {
                IsActive = false,
                NameKey = "table_key",
                LocalizedName = new Dictionary<string, string>
                {
                    { "en", "Updated" }
                },
                Buttons = new List<ButtonUpdateDto>
                {
                    new()
                    {
                        Id = 1,
                        NameKey = "btn_key",
                        BoothToVisit = 99,
                        LocalizedName = new Dictionary<string, string>
                        {
                            { "en", "Teleport" }
                        }
                    }
                }
            };

        // GET
        [Fact]
        public async Task GetTeleportTablesBySpace_ReturnsOk_WhenTablesExist()
        {
            // Arrange
            var sample = CreateSampleTeleportTable();
            repoMock.Setup(r => r.GetTeleportTableBySpaceAsync(100)).ReturnsAsync(sample);

            // Act
            var result = await sut.GetTeleportTablesBySpace(100);

            // Assert
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(sample, options => options
                .WithStrictOrdering()
            );
        }

        [Theory]
        [InlineData(200)]
        [InlineData(404)]
        public async Task GetTeleportTablesBySpace_ReturnsNotFound_WhenEmpty(int spaceId)
        {
            // Arrange
            repoMock.Setup(r => r.GetTeleportTableBySpaceAsync(spaceId))
                    .ReturnsAsync((TeleportTableData?)null);

            // Act
            var result = await sut.GetTeleportTablesBySpace(spaceId);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
            repoMock.Verify(r => r.GetTeleportTableBySpaceAsync(spaceId), Times.Once);
        }

        [Fact]
        public async Task GetTeleportTablesBySpace_ReturnsNotFound_WhenSpaceIdIsNegative()
        {
            // Act
            var result = await sut.GetTeleportTablesBySpace(-1);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }


        [Fact]
        public async Task GetTeleportTablesBySpace_Returns500_OnException()
        {
            // Arrange
            repoMock.Setup(r => r.GetTeleportTableBySpaceAsync(It.IsAny<int>()))
                    .ThrowsAsync(new Exception("fail"));

            // Act
            var result = await sut.GetTeleportTablesBySpace(300);

            // Assert
            var obj = result.Result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
            obj.Value.Should().BeEquivalentTo(new { error = "fail" });
        }

        [Fact]
        public async Task GetTeleportTablesBySpace_ReturnsOk_WithButtonsHavingMultipleLocales()
        {
            // Arrange
            var sample = CreateSampleTeleportTable();
            sample.Buttons[0].LocalizedName["es"] = "Ir";

            repoMock.Setup(r => r.GetTeleportTableBySpaceAsync(100)).ReturnsAsync(sample);

            // Act
            var result = await sut.GetTeleportTablesBySpace(100);

            // Assert
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(sample, options => options.WithStrictOrdering());
        }

        [Fact]
        public async Task GetTeleportTablesBySpace_ReturnsOk_WhenNameKeyIsNull()
        {
            // Arrange
            var sample = CreateSampleTeleportTable();
            sample.NameKey = null;

            repoMock.Setup(r => r.GetTeleportTableBySpaceAsync(100)).ReturnsAsync(sample);

            // Act
            var result = await sut.GetTeleportTablesBySpace(100);

            // Assert
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(sample);
        }

        [Fact]
        public async Task GetTeleportTablesBySpace_ReturnsOk_WhenEmptyButtonsAndLocalization()
        {
            // Arrange
            var sample = CreateSampleTeleportTable();
            sample.LocalizedName = new Dictionary<string, string>();
            sample.Buttons = new List<ButtonData>();

            repoMock.Setup(r => r.GetTeleportTableBySpaceAsync(100)).ReturnsAsync(sample);

            // Act
            var result = await sut.GetTeleportTablesBySpace(100);

            // Assert
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(sample);
        }


        // PUT
        [Fact]
        public async Task UpdateTeleportTableById_ReturnsBadRequest_WhenDtoIsNull()
        {
            // Act
            var result = await sut.UpdateTeleportTableById(1, 1, null!);

            // Assert
            result.Result.Should().BeOfType<BadRequestResult>();
        }

        [Fact]
        public async Task UpdateTeleportTableById_ReturnsOk_WhenUpdated()
        {
            // Arrange
            var dto = CreateValidUpdateDto();
            var updated = CreateSampleTeleportTable(spaceId: 1, tableId: 1);
            updated.IsActive = false;
            repoMock.Setup(r => r.UpdateTeleportTableAsync(1, 1, dto)).ReturnsAsync(updated);

            // Act
            var result = await sut.UpdateTeleportTableById(1, 1, dto);

            // Assert
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(updated);
            repoMock.Verify(r => r.UpdateTeleportTableAsync(1, 1, dto), Times.Once);
        }

        [Fact]
        public async Task UpdateTeleportTableById_ReturnsNotFound_WhenNullReturned()
        {
            // Arrange
            var dto = CreateValidUpdateDto();
            repoMock.Setup(r => r.UpdateTeleportTableAsync(2, 2, dto)).ReturnsAsync((TeleportTableData)null);

            // Act
            var result = await sut.UpdateTeleportTableById(2, 2, dto);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task UpdateTeleportTableById_Returns500_OnException()
        {
            // Arrange
            var dto = CreateValidUpdateDto();
            repoMock.Setup(r => r.UpdateTeleportTableAsync(
                                It.IsAny<int>(),
                                It.IsAny<int>(),
                                It.IsAny<TeleportTableUpdateDto>()))
                    .ThrowsAsync(new Exception("db error"));

            // Act
            var result = await sut.UpdateTeleportTableById(3, 3, dto);

            // Assert
            var obj = result.Result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
            obj.Value.Should().BeEquivalentTo(new { error = "db error" });
        }

        [Theory]
        [InlineData(-1, 1)]
        [InlineData(1, -1)]
        [InlineData(-1, -1)]
        public async Task UpdateTeleportTableById_ReturnsNotFound_ForInvalidIds(int spaceId, int tableId)
        {
            // Arrange
            var dto = CreateValidUpdateDto();

            // Act
            var result = await sut.UpdateTeleportTableById(spaceId, tableId, dto);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        // AUTOFIXTURE TESTS
        // AutoFixture tests validate controller behavior with randomized valid inputs to ensure robustness against a wide range of data scenarios.

        [Theory]
        [CustomAutoData]
        public async Task GetTeleportTablesBySpace_WithRandomId_ReturnsNotFound_WhenNull(
            int spaceId)
        {
            repoMock.Setup(r => r.GetTeleportTableBySpaceAsync(spaceId)).ReturnsAsync((TeleportTableData?)null);

            var result = await sut.GetTeleportTablesBySpace(spaceId);

            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Theory]
        [InlineCustomAutoData(5, 9)]
        public async Task UpdateTeleportTableById_WithRandomDto_ReturnsOk(
            int spaceId,
            int tableId,
            TeleportTableUpdateDto dto,
            TeleportTableData updated)
        {
            updated.SpaceId = spaceId;
            updated.Id = tableId;

            repoMock.Setup(r => r.UpdateTeleportTableAsync(spaceId, tableId, dto)).ReturnsAsync(updated);

            var result = await sut.UpdateTeleportTableById(spaceId, tableId, dto);

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(updated);
        }
    }
}
