// <copyright file="LocalizationControllerTests.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>08/14/2025</date>
// <summary>Unit tests for LocalizationController covering endpoint behavior.</summary>

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
    /// <summary>
    /// Tests for LocalizationController (GET /api/space/{spaceId}/localizations).
    /// </summary>
    public class LocalizationControllerTests
    {
        private readonly Mock<ILocalizationRepository> repo;
        private readonly LocalizationController sut;

        public LocalizationControllerTests()
        {
            repo = new Mock<ILocalizationRepository>();
            sut = new LocalizationController(repo.Object);
        }

        private static LocalizedPairs BuildDefaultDto(string key = "bth_nm_101")
        {
            return new LocalizedPairs
            {
                Key = key,
                Values = new List<LocalizedValue>
                {
                    new() { LocaleId = "en_us", Value = "Main Hall Booth" }
                }
            };
        }


        //
        #region GET /api/space/{spaceId}/localizations
        ///// <summary>
        /// Happy path for listing all localizations in a space. 
        /// Verifies the controller returns 200 OK with the exact list (order preserved) and calls the repository once with the given spaceId.
        /// This ensures the read path and payload shape are stable for consumers.
        /// </summary>
        [Fact]
        public async Task GetAll_Ok()
        {
            // ARRANGE
            var list = new List<LocalizedPairs>
            {
                new LocalizedPairsBuilder()
                    .WithKey("bth_nm_101")
                    .Add("en_us", "Main Hall Booth")
                    .Add("es_es", "Puesto del Salón Principal")
                    .Build(),
                new LocalizedPairsBuilder()
                    .WithKey("prt_lbl_news")
                    .Add("en_us", "News")
                    .Build()
            };

            repo.Setup(r => r.GetAllLocalizationsBySpaceAsync(100))
                .ReturnsAsync(list);

            // ACT
            var result = await sut.GetAllBySpace(100);

            // ASSERT
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(list, opts => opts.WithStrictOrdering());
            repo.Verify(r => r.GetAllLocalizationsBySpaceAsync(100), Times.Once);
        }

        /// <summary>
        /// Not-found path for listing localizations: covers both repository returning null and an empty list. 
        /// Asserts the controller throws ResourceNotFoundException and that the repository is invoked exactly once.
        /// This guarantees 404 semantics when a space has no localization data.
        /// </summary>
        [Theory]
        [InlineData(true)]   // repo returns null
        [InlineData(false)]  // repo returns empty list
        public async Task GetAll_NotFound(bool repoReturnsNull)
        {
            // ARRANGE
            if (repoReturnsNull)
                repo.Setup(r => r.GetAllLocalizationsBySpaceAsync(200))
                    .ReturnsAsync((List<LocalizedPairs>?)null);
            else
                repo.Setup(r => r.GetAllLocalizationsBySpaceAsync(200))
                    .ReturnsAsync(new List<LocalizedPairs>());

            // ACT & ASSERT
            await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.GetAllBySpace(200));
            repo.Verify(r => r.GetAllLocalizationsBySpaceAsync(200), Times.Once);
        }

        /// <summary>
        /// Input validation for the spaceId route parameter when listing localizations. 
        /// Ensures zero/negative ids raise ArgumentException and prevents any repository call, 
        /// protecting downstream layers from invalid input early.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-42)]
        public async Task GetAll_BadSpaceId(int spaceId)
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() => sut.GetAllBySpace(spaceId));

            // Ensure repository is not called when input is invalid
            repo.Verify(r => r.GetAllLocalizationsBySpaceAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Repository failure propagation for listing localizations. 
        /// Verifies that unexpected exceptions from the repository bubble up so global middleware can format the 5xx response.
        /// </summary>
        [Fact]
        public async Task GetAll_RepoThrows()
        {
            // ARRANGE
            repo.Setup(r => r.GetAllLocalizationsBySpaceAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Simulated repo failure"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.GetAllBySpace(321));
            ex.Message.Should().Be("Simulated repo failure");
        }

        #endregion


        #region GET /{spaceId}/localizations/{key}

        /// <summary>
        /// Happy path for fetching a single localization by key. 
        /// Verifies a 200 OK with the expected LocalizedPairs body and a single repository invocation with matching arguments.
        /// Confirms stable contract for consumers retrieving a specific key.
        /// </summary>
        [Fact]
        public async Task GetByKey_Ok()
        {
            // ARRANGE
            var expected = BuildDefaultDto();

            repo.Setup(r => r.GetLocalizationByKeyAsync(100, "bth_nm_101"))
                .ReturnsAsync(expected);

            // ACT
            var result = await sut.GetLocalization(100, "bth_nm_101");

            // ASSERT
            var ok = result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(expected);
            repo.Verify(r => r.GetLocalizationByKeyAsync(100, "bth_nm_101"), Times.Once);
        }

        /// <summary>
        /// Input validation for the spaceId route parameter when fetching by key. 
        /// Ensures invalid ids cause ArgumentException and no repository call, enforcing a clear guardrail at the controller boundary.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-42)]
        public async Task GetByKey_BadSpaceId(int spaceId)
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() => sut.GetLocalization(spaceId, "any_key"));

            // repo not called
            repo.Verify(r => r.GetLocalizationByKeyAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Input validation for the key route parameter when fetching by key. 
        /// Ensures null/empty/whitespace keys raise ArgumentException and the repository is never invoked,
        /// preventing ambiguous lookups and maintaining API contract integrity.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetByKey_BadKey(string key)
        {
            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() => sut.GetLocalization(10, key));

            // repo not called
            repo.Verify(r => r.GetLocalizationByKeyAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Not-found path for fetching by key. 
        /// When the repository returns null (key absent in the given space), verifies the controller throws ResourceNotFoundException 
        /// and records exactly one repository call with the provided arguments.
        /// </summary>
        [Fact]
        public async Task GetByKey_NotFound()
        {
            // ARRANGE
            repo.Setup(r => r.GetLocalizationByKeyAsync(5, "missing_key"))
                .ReturnsAsync((LocalizedPairs?)null);

            // ACT & ASSERT
            await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.GetLocalization(5, "missing_key"));
            repo.Verify(r => r.GetLocalizationByKeyAsync(5, "missing_key"), Times.Once);
        }

        /// <summary>
        /// Repository failure propagation for fetching by key. 
        /// Confirms that unexpected exceptions are not swallowed by the controller and can be handled by global middleware uniformly.
        /// </summary>
        [Fact]
        public async Task GetByKey_RepoThrows()
        {
            // ARRANGE
            repo.Setup(r => r.GetLocalizationByKeyAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Simulated repo failure"));

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.GetLocalization(77, "bth_nm_777"));
            ex.Message.Should().Be("Simulated repo failure");
        }

        #endregion


        #region  POST /api/space/{spaceId}/localizations

        /// <summary>
        /// Ensures CreateLocalization throws ArgumentException when spaceId is zero/negative
        /// and never calls the repository.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-42)]
        public async Task Post_InvalidSpaceId_Throws(int spaceId)
        {
            var dto = BuildDefaultDto();

            await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateLocalization(spaceId, dto));

            repo.Verify(r => r.CreateLocalizationAsync(It.IsAny<int>(), It.IsAny<LocalizedPairs>()), Times.Never);
        }

        /// <summary>
        /// Ensures CreateLocalization throws an ArgumentNullException when the DTO is null.  
        /// This safeguards against null request bodies being processed by the controller.
        /// </summary>
        [Fact]
        public async Task Post_NullDto_Throws()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => sut.CreateLocalization(10, null!));

            repo.Verify(r => r.CreateLocalizationAsync(It.IsAny<int>(), It.IsAny<LocalizedPairs>()), Times.Never);
        }

        /// <summary>
        /// Ensures CreateLocalization throws an ArgumentException when the DTO's Key 
        /// is null, empty, or whitespace.  
        /// This protects against invalid i18n keys being stored.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Post_InvalidKey_Throws(string key)
        {
            var dto = BuildDefaultDto(key);

            await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateLocalization(10, dto));

            repo.Verify(r => r.CreateLocalizationAsync(It.IsAny<int>(), It.IsAny<LocalizedPairs>()), Times.Never);
        }

        /// <summary>
        /// Ensures CreateLocalization throws an ArgumentException when the Values list 
        /// is null or empty.  
        /// This ensures every created localization has at least one locale/value pair.
        /// </summary>
        [Fact]
        public async Task Post_EmptyOrNullValues_Throws()
        {
            var dtoNull = new LocalizedPairs { Key = "bth_nm_101", Values = null! };
            await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateLocalization(10, dtoNull));

            var dtoEmpty = new LocalizedPairs { Key = "bth_nm_101", Values = new List<LocalizedValue>() };
            await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateLocalization(10, dtoEmpty));

            repo.Verify(r => r.CreateLocalizationAsync(It.IsAny<int>(), It.IsAny<LocalizedPairs>()), Times.Never);
        }

        /// <summary>
        /// Ensures CreateLocalization throws an InvalidOperationException when the repository 
        /// returns null after attempting creation.  
        /// This simulates a persistence failure and verifies the controller responds correctly.
        /// </summary>
        [Fact]
        public async Task Post_CreateFails_Throws()
        {
            var dto = BuildDefaultDto();

            repo.Setup(r => r.CreateLocalizationAsync(100, dto))
                .ReturnsAsync((LocalizedPairs?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateLocalization(100, dto));
            repo.Verify(r => r.CreateLocalizationAsync(100, dto), Times.Once);
        }

        /// <summary>
        /// Ensures CreateLocalization returns a CreatedAtActionResult with the correct 
        /// route values and response body when creation is successful.  
        /// This verifies that the happy path returns proper HTTP 201 status and payload.
        /// </summary>
        [Fact]
        public async Task Post_Success_ReturnsCreated()
        {
            var dto = BuildDefaultDto();

            var created = BuildDefaultDto();

            repo.Setup(r => r.CreateLocalizationAsync(5, dto))
                .ReturnsAsync(created);

            var result = await sut.CreateLocalization(5, dto);

            var createdAt = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdAt.ActionName.Should().Be(nameof(LocalizationController.GetLocalization));
            createdAt.RouteValues.Should().ContainKey("spaceId").WhoseValue.Should().Be(5);
            createdAt.RouteValues.Should().ContainKey("key").WhoseValue.Should().Be("bth_nm_101");
            createdAt.Value.Should().BeEquivalentTo(created);
            repo.Verify(r => r.CreateLocalizationAsync(5, dto), Times.Once);
        }

        /// <summary>
        /// Ensures CreateLocalization propagates exceptions thrown by the repository.  
        /// This confirms that unexpected data store errors bubble up for global handling.
        /// </summary>
        [Fact]
        public async Task Post_RepoThrows_Propagates()
        {
            var dto = new LocalizedPairs
            {
                Key = "bth_nm_101",
                Values = new List<LocalizedValue> { new() { LocaleId = "en_us", Value = "Main Hall Booth" } }
            };

            repo.Setup(r => r.CreateLocalizationAsync(It.IsAny<int>(), It.IsAny<LocalizedPairs>()))
                .ThrowsAsync(new Exception("Simulated repo failure"));

            var ex = await Assert.ThrowsAsync<Exception>(() => sut.CreateLocalization(7, dto));
            ex.Message.Should().Be("Simulated repo failure");
        }

        #endregion


        #region PUT /api/space/{spaceId}/localizations/{key}

        /// <summary>
        /// Validates that UpdateLocalization rejects invalid spaceId values (zero or negative),
        /// throws ArgumentException, and never calls the repository.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-42)]
        public async Task Put_InvalidSpaceId_Throws(int spaceId)
        {
            var dto = BuildDefaultDto();

            await Assert.ThrowsAsync<ArgumentException>(() => sut.UpdateLocalization(spaceId, "bth_nm_101", dto));

            repo.Verify(r => r.UpdateLocalizationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<LocalizedPairs>()), Times.Never);
        }

        /// <summary>
        /// Ensures that a null/empty/whitespace key causes ArgumentException
        /// and prevents repository interaction.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Put_InvalidKey_Throws(string key)
        {
            var dto = BuildDefaultDto();

            await Assert.ThrowsAsync<ArgumentException>(() => sut.UpdateLocalization(10, key, dto));

            repo.Verify(r => r.UpdateLocalizationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<LocalizedPairs>()), Times.Never);
        }

        /// <summary>
        /// Ensures the controller throws ArgumentNullException when the DTO is null
        /// and does not call the repository.
        /// </summary>
        [Fact]
        public async Task Put_NullDto_Throws()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => sut.UpdateLocalization(10, "bth_nm_101", null!));

            repo.Verify(r => r.UpdateLocalizationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<LocalizedPairs>()), Times.Never);
        }

        /// <summary>
        /// Ensures an ArgumentException is thrown when Values is null or empty
        /// and the repository is never called.
        /// </summary>
        [Fact]
        public async Task Put_EmptyOrNullValues_Throws()
        {
            var dtoNull = new LocalizedPairs { Key = "bth_nm_101", Values = null! };
            await Assert.ThrowsAsync<ArgumentException>(() => sut.UpdateLocalization(10, "bth_nm_101", dtoNull));

            var dtoEmpty = new LocalizedPairs { Key = "bth_nm_101", Values = new List<LocalizedValue>() };
            await Assert.ThrowsAsync<ArgumentException>(() => sut.UpdateLocalization(10, "bth_nm_101", dtoEmpty));

            repo.Verify(r => r.UpdateLocalizationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<LocalizedPairs>()), Times.Never);
        }

        /// <summary>
        /// Ensures ResourceNotFoundException is thrown when repository returns null,
        /// indicating no localization found for the given key.
        /// </summary>
        [Fact]
        public async Task Put_NotFound_Throws()
        {
            var dto = BuildDefaultDto("bth_nm_404");

            repo.Setup(r => r.UpdateLocalizationAsync(3, "bth_nm_404", dto))
                .ReturnsAsync((LocalizedPairs?)null);

            await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.UpdateLocalization(3, "bth_nm_404", dto));
            repo.Verify(r => r.UpdateLocalizationAsync(3, "bth_nm_404", dto), Times.Once);
        }

        /// <summary>
        /// Verifies UpdateLocalization returns 200 OK with the updated payload
        /// when the operation is successful.
        /// </summary>
        [Fact]
        public async Task Put_Success_ReturnsOk()
        {
            var dto = BuildDefaultDto();
            var updated = BuildDefaultDto();

            repo.Setup(r => r.UpdateLocalizationAsync(5, "bth_nm_101", dto))
                .ReturnsAsync(updated);

            var result = await sut.UpdateLocalization(5, "bth_nm_101", dto);

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(StatusCodes.Status200OK);
            ok.Value.Should().BeEquivalentTo(updated);
            repo.Verify(r => r.UpdateLocalizationAsync(5, "bth_nm_101", dto), Times.Once);
        }

        /// <summary>
        /// Ensures exceptions thrown by the repository are propagated
        /// so they can be handled by global error middleware.
        /// </summary>
        [Fact]
        public async Task Put_RepoThrows_Propagates()
        {
            var dto = BuildDefaultDto();

            repo.Setup(r => r.UpdateLocalizationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<LocalizedPairs>()))
                .ThrowsAsync(new Exception("Simulated repo failure"));

            var ex = await Assert.ThrowsAsync<Exception>(() => sut.UpdateLocalization(9, "bth_nm_101", dto));
            ex.Message.Should().Be("Simulated repo failure");
        }

        #endregion


        #region DELETE /api/space/{spaceId}/localizations/{key}

        /// <summary>
        /// Rejects zero/negative spaceId with ArgumentException and guarantees
        /// the repository is never invoked. Protects downstream layers from bad route input.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-42)]
        public async Task Delete_InvalidSpaceId_Throws(int spaceId)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => sut.DeleteLocalization(spaceId, "bth_nm_101"));

            repo.Verify(r => r.DeleteLocalizationAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Rejects a null/empty/whitespace key with ArgumentException and ensures
        /// no repository interaction occurs. Enforces API contract for the key parameter.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Delete_InvalidKey_Throws(string key)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => sut.DeleteLocalization(10, key));

            repo.Verify(r => r.DeleteLocalizationAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Returns 204 NoContent on successful deletion and verifies the repository
        /// is called exactly once with the expected arguments.
        /// </summary>
        [Fact]
        public async Task Delete_Success_NoContent()
        {
            repo.Setup(r => r.DeleteLocalizationAsync(5, "bth_nm_101"))
                .ReturnsAsync(true);

            var result = await sut.DeleteLocalization(5, "bth_nm_101");

            result.Should().BeOfType<NoContentResult>();
            repo.Verify(r => r.DeleteLocalizationAsync(5, "bth_nm_101"), Times.Once);
        }

        /// <summary>
        /// Throws ResourceNotFoundException when the repository reports no rows affected
        /// (i.e., the localization key does not exist for the given space).
        /// </summary>
        [Fact]
        public async Task Delete_NotFound_Throws()
        {
            repo.Setup(r => r.DeleteLocalizationAsync(7, "missing_key"))
                .ReturnsAsync(false);

            await Assert.ThrowsAsync<ResourceNotFoundException>(() => sut.DeleteLocalization(7, "missing_key"));
            repo.Verify(r => r.DeleteLocalizationAsync(7, "missing_key"), Times.Once);
        }

        /// <summary>
        /// Propagates unexpected repository failures so global exception middleware
        /// can translate them into a consistent 5xx response with diagnostics.
        /// </summary>
        [Fact]
        public async Task Delete_RepoThrows_Propagates()
        {
            repo.Setup(r => r.DeleteLocalizationAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Simulated repo failure"));

            var ex = await Assert.ThrowsAsync<Exception>(() => sut.DeleteLocalization(9, "bth_nm_101"));
            ex.Message.Should().Be("Simulated repo failure");
        }


        #endregion

    }
}
