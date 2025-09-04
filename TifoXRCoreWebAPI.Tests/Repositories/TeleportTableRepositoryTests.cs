// <copyright file="TeleportTableRepositoryTests.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/15/2025</date>
// <summary>Smoke test for GetTeleportTableBySpaceAsync verifying null return when no rows are present.</summary>

using FluentAssertions;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Tests.TestDoubles.Fakes;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.Common;
using TifoXRCoreWebAPI.Tests.TestDoubles.Schemas;
using TifoXRCoreWebAPI.Utilities.Infrastructure.Interface;
using Xunit;

namespace GMS.TifoXRCoreWebAPI.Tests.Repositories
{
    public class TeleportTableRepositoryTests
    {
        [Fact]
        public async Task ReturnsNull_WhenNoRows()
        {
            // Arrange: empty reader (no rows) -> repository should return null
            DataTable dt = TeleportGetSchema.CreateEmptySchema();

            // Each ExecuteReader needs a FRESH reader; pass a factory that calls CreateDataReader()
            IDbProvider fakeDb = new FakeDbProvider(() => dt.CreateDataReader());

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=unused;Uid=unused;Pwd=unused;Database=unused;"
                })
                .Build();

            var sut = new TeleportTableRepository(config, fakeDb);

            // Act
            var result = await sut.GetTeleportTableBySpaceAsync(spaceId: 123);

            // Assert
            result.Should().BeNull("no rows were returned by the reader, so the repository should return null");
        }
    }
}
