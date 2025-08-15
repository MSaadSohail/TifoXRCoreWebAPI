// <copyright file="PortalControllerTests.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra (updated)</author>
// <date>08/08/2025</date>
// <summary>
// Unit tests for PortalController aligned with GlobalException middleware.
// Short test names; success = Ok/CreatedAt/NoContent; errors = thrown exceptions.
// </summary>

using FluentAssertions;
using GMS.TifoXRCoreWebAPI.Controllers;
using GMS.TifoXRCoreWebAPI.Middleware.Exceptions;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using GMS.TifoXRCoreWebAPI.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

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

        private static PortalModel SamplePortal(int spaceId = 1, int portalId = 10, int? boothId = null) =>
            new()
            {
                PortalId = portalId,
                SpaceId = spaceId,
                BoothId = boothId,
                PortalTypeId = 2,
                ExternalLink = "https://example.com",
                LocalizedPairs = new LocalizedPairs
                {
                    Key = "prt_key",
                    Values = new List<LocalizedValue>
                    {
                        new() { LocaleId = "en_us", Value = "Portal EN" }
                    }
                },

                CorrespondingMedia = null!,
                ThumbnailMedia = null!
            };

        #region GET — /api/space/{spaceId}/portal

        [Fact]
        public async Task GetBySpace_ReturnsOk()
        {
            var list = new List<PortalModel> { SamplePortal(100, 1), SamplePortal(100, 2) };
            mockRepo.Setup(r => r.GetPortalsBySpaceAsync(100)).ReturnsAsync(list);

            var result = await sut.GetPortalsBySpace(100);

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(list, o => o.WithStrictOrdering());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetBySpace_ThrowsArgEx_OnInvalidId(int badSpaceId)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => sut.GetPortalsBySpace(badSpaceId));
        }

        [Fact]
        public async Task GetBySpace_ThrowsNotFound_WhenEmptyOrNull()
        {
            mockRepo.Setup(r => r.GetPortalsBySpaceAsync(5)).ReturnsAsync(new List<PortalModel>());
            await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.GetPortalsBySpace(5));

            mockRepo.Reset();
            mockRepo.Setup(r => r.GetPortalsBySpaceAsync(5)).ReturnsAsync((List<PortalModel>?)null);
            await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.GetPortalsBySpace(5));
        }

        [Fact]
        public async Task GetBySpace_PropagatesRepoEx()
        {
            mockRepo.Setup(r => r.GetPortalsBySpaceAsync(It.IsAny<int>()))
                    .ThrowsAsync(new Exception("Unable to find portal."));
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.GetPortalsBySpace(1));
            ex.Message.Should().Be("Unable to find portal.");
        }

        #endregion

        #region GET — /api/space/{spaceId}/booth/{boothId}/portals

        [Fact]
        public async Task GetByBooth_ReturnsOk()
        {
            var list = new List<PortalModel> { SamplePortal(1, 10, boothId: 22) };
            mockRepo.Setup(r => r.GetPortalsByBoothAsync(1, 22)).ReturnsAsync(list);

            var result = await sut.GetPortalsByBooth(1, 22);

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(list);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(-1, 5)]
        [InlineData(5, -1)]
        public async Task GetByBooth_ThrowsArgEx_OnInvalidIds(int spaceId, int boothId)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => sut.GetPortalsByBooth(spaceId, boothId));
        }

        [Fact]
        public async Task GetByBooth_ThrowsNotFound_WhenEmptyOrNull()
        {
            mockRepo.Setup(r => r.GetPortalsByBoothAsync(2, 3)).ReturnsAsync(new List<PortalModel>());
            await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.GetPortalsByBooth(2, 3));

            mockRepo.Reset();
            mockRepo.Setup(r => r.GetPortalsByBoothAsync(2, 3)).ReturnsAsync((List<PortalModel>?)null);
            await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.GetPortalsByBooth(2, 3));
        }

        #endregion

        #region GET — /api/space/{spaceId}/portal/{portalId}

        [Fact]
        public async Task GetById_ReturnsOk()
        {
            var p = SamplePortal(1, 9);
            mockRepo.Setup(r => r.GetPortalByIdAsync(1, 9)).ReturnsAsync(p);

            var result = await sut.GetPortalById(1, 9);

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(p);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(-1, 1)]
        [InlineData(1, -1)]
        public async Task GetById_ThrowsArgEx_OnInvalidIds(int spaceId, int portalId)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => sut.GetPortalById(spaceId, portalId));
        }

        [Fact]
        public async Task GetById_ThrowsNotFound_WhenNull()
        {
            mockRepo.Setup(r => r.GetPortalByIdAsync(1, 77)).ReturnsAsync((PortalModel?)null);
            await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.GetPortalById(1, 77));
        }

        #endregion

        #region POST — /api/space/{spaceId}/portal

        [Fact]
        public async Task Create_ReturnsCreatedAt()
        {
            var created = SamplePortal(5, 123);
            mockRepo.Setup(r => r.CreatePortalAsync(5, defaultCreateDto)).ReturnsAsync(created);

            var result = await sut.CreatePortal(5, defaultCreateDto);

            var createdAt = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdAt.ActionName.Should().Be(nameof(PortalController.GetPortalById));
            createdAt.RouteValues["spaceId"].Should().Be(5);
            createdAt.RouteValues["portalId"].Should().Be(123);
            createdAt.Value.Should().BeEquivalentTo(created);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Create_ThrowsArgEx_OnInvalidSpaceId(int spaceId)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => sut.CreatePortal(spaceId, defaultCreateDto));
        }

        [Fact]
        public async Task Create_ThrowsArgNullEx_OnNullDto()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => sut.CreatePortal(1, null!));
        }

        [Theory]
        [InlineData(true, false, false)] // null LocalizedPairs
        [InlineData(false, true, false)] // empty values
        [InlineData(false, false, true)] // null values
        public async Task Create_ThrowsArgEx_OnInvalidLocalizedPairs(bool nullPairs, bool emptyValues, bool nullValues)
        {
            var dto = new PortalCreateDtoBuilder().Build();
            if (nullPairs) dto.LocalizedPairs = null!;
            if (emptyValues) dto.LocalizedPairs!.Values = new List<LocalizedValue>();
            if (nullValues) dto.LocalizedPairs!.Values = null!;

            await Assert.ThrowsAsync<ArgumentException>(() => sut.CreatePortal(1, dto));
        }

        [Fact]
        public async Task Create_ThrowsInvalidOpEx_OnRepoNull()
        {
            mockRepo.Setup(r => r.CreatePortalAsync(1, defaultCreateDto)).ReturnsAsync((PortalModel?)null);
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreatePortal(1, defaultCreateDto));
        }

        #endregion

        #region PUT — /api/space/{spaceId}/portal/{portalId}

        [Fact]
        public async Task Update_ReturnsOk()
        {
            var updated = SamplePortal(1, 7);
            mockRepo.Setup(r => r.UpdatePortalAsync(1, 7, defaultUpdateDto)).ReturnsAsync(updated);

            var result = await sut.UpdatePortal(1, 7, defaultUpdateDto);

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(updated);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(-1, 1)]
        [InlineData(1, -1)]
        public async Task Update_ThrowsArgEx_OnInvalidIds(int spaceId, int portalId)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => sut.UpdatePortal(spaceId, portalId, defaultUpdateDto));
        }

        [Fact]
        public async Task Update_ThrowsArgNullEx_OnNullDto()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => sut.UpdatePortal(1, 1, null!));
        }

        [Theory]
        [InlineData(true, false, false)] // null pairs
        [InlineData(false, true, false)] // empty key
        [InlineData(false, false, true)] // empty values
        public async Task Update_ThrowsArgEx_OnInvalidLocalizedPairs(bool nullPairs, bool emptyKey, bool emptyValues)
        {
            var dto = new PortalUpdateDtoBuilder().Build();
            if (nullPairs) dto.LocalizedPairs = null!;
            if (emptyKey) dto.LocalizedPairs!.Key = "   ";
            if (emptyValues) dto.LocalizedPairs!.Values = new List<LocalizedValue>();

            await Assert.ThrowsAsync<ArgumentException>(() => sut.UpdatePortal(1, 1, dto));
        }

        [Fact]
        public async Task Update_ThrowsNotFound_OnRepoNull()
        {
            mockRepo.Setup(r => r.UpdatePortalAsync(1, 99, defaultUpdateDto)).ReturnsAsync((PortalModel?)null);
            await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.UpdatePortal(1, 99, defaultUpdateDto));
        }

        #endregion

        #region PUT — /api/space/{spaceId}/booth/{boothId}/portal/{portalId}

        [Fact]
        public async Task UpdateForBooth_ReturnsOk()
        {
            var updatedPortal  = new PortalModelBuilder()
                .WithSpaceId(2)
                .WithBoothId(44)
                .WithId(12)
                .Build();

            // Force the 4-arg overload and return PortalModel (NOT PortalResponse)
            mockRepo
                .Setup(r => r.UpdatePortalAsync(
                    It.IsAny<int>(), 
                    It.IsAny<int>(), 
                    It.IsAny<int>(), 
                    It.IsAny<PortalUpdateDto>()))
                .ReturnsAsync(updatedPortal);

            var result = await sut.UpdatePortalData(2, 44, 12, defaultUpdateDto);

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(updatedPortal);
        }

        [Theory]
        [InlineData(0, 1, 1)]
        [InlineData(1, 0, 1)]
        [InlineData(1, 1, 0)]
        [InlineData(-1, 2, 3)]
        [InlineData(1, -2, 3)]
        [InlineData(1, 2, -3)]
        public async Task UpdateForBooth_ThrowsArgEx_OnInvalidIds(int spaceId, int boothId, int portalId)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => sut.UpdatePortalData(spaceId, boothId, portalId, defaultUpdateDto));
        }

        [Fact]
        public async Task UpdateForBooth_ThrowsArgNullEx_OnNullDto()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => sut.UpdatePortalData(1, 1, 1, null!));
        }

        [Fact]
        public async Task UpdateForBooth_ThrowsNotFound_OnRepoNull()
        {
            // NOT FOUND path
            mockRepo
            .Setup(r => r.UpdatePortalAsync(
                It.IsAny<int>(), 
                It.IsAny<int>(), 
                It.IsAny<int>(), 
                It.IsAny<PortalUpdateDto>()))
                .Returns((
                    int _s, 
                    int _b, 
                    int _p, 
                    PortalUpdateDto _d
                    ) => Task.FromResult<PortalModel?>(null));

            await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.UpdatePortalData(3, 33, 333, defaultUpdateDto));
        }

        #endregion

        #region DELETE — /api/space/{spaceId}/portal/{portalId}

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(-1, 1)]
        [InlineData(1, -1)]
        public async Task Delete_ThrowsArgEx_OnInvalidIds(int spaceId, int portalId)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => sut.DeletePortal(spaceId, portalId));
        }

        [Fact]
        public async Task Delete_ThrowsNotFound_WhenRepoFalse()
        {
            mockRepo.Setup(r => r.DeletePortalAsync(1, 2)).ReturnsAsync(false);
            await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.DeletePortal(1, 2));
        }

        [Fact]
        public async Task Delete_ReturnsOkMessage()
        {
            mockRepo.Setup(r => r.DeletePortalAsync(1, 2)).ReturnsAsync(true);

            var result = await sut.DeletePortal(1, 2);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(new { message = "Portal deleted successfully." });
        }

        #endregion

        #region DELETE — /api/space/{spaceId}/booth/{boothId}/portal/{portalId}

        [Theory]
        [InlineData(0, 1, 1)]
        [InlineData(1, 0, 1)]
        [InlineData(1, 1, 0)]
        [InlineData(-1, 1, 1)]
        [InlineData(1, -1, 1)]
        [InlineData(1, 1, -1)]
        public async Task DeleteForBooth_ThrowsArgEx_OnInvalidIds(int spaceId, int boothId, int portalId)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => sut.DeletePortalForBooth(spaceId, boothId, portalId));
        }

        [Fact]
        public async Task DeleteForBooth_ThrowsNotFound_WhenRepoFalse()
        {
            mockRepo.Setup(r => r.DeletePortalAsync(1, 2, 3)).ReturnsAsync(false);
            await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.DeletePortalForBooth(1, 2, 3));
        }

        [Fact]
        public async Task DeleteForBooth_ReturnsOkMessage()
        {
            mockRepo.Setup(r => r.DeletePortalAsync(1, 2, 3)).ReturnsAsync(true);

            var result = await sut.DeletePortalForBooth(1, 2, 3);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(new { message = "Portal deleted successfully." });
        }

        #endregion
    }
}
