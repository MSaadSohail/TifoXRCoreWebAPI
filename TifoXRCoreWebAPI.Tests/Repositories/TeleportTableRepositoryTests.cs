// <copyright file="TeleportTableRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/19/2025</date>
// <summary>
// Test file containing unit tests for GET, POST, PUT, DELETE
// Uses:
//   - FakeDbProvider (scriptable; supports ExecuteReader/NonQuery/Scalar + transactions)
//   - RepositorySchemas (generic schema helper)
//   - DataRowBuilder (generic row builder; with teleport-specific helpers)
//   - Teleport DTO builders (for concise POST scenarios) </summary>

using FluentAssertions;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Tests.TestDoubles.Builders;
using GMS.TifoXRCoreWebAPI.Tests.TestDoubles.Fakes;
using GMS.TifoXRCoreWebAPI.Tests.TestDoubles.Schemas;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using TestDataRowBuilder = GMS.TifoXRCoreWebAPI.Tests.TestDoubles.Builders.DataRowBuilder;

namespace GMS.TifoXRCoreWebAPI.Tests.Repositories
{
    /// <summary>
    /// End-to-end unit tests for <see cref="TeleportTableRepository"/>, organized by HTTP verb.
    /// Each test follows Arrange / Act / Assert structure.
    /// </summary>
    public class TeleportTableRepositoryTests
    {
        /// <summary>
        /// Builds a repository using a <see cref="DataTable"/>-backed reader for GET scenarios.
        /// </summary>
        private static TeleportTableRepository BuildRepo(DataTable dt)
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Server=unused;Uid=unused;Pwd=unused;Database=unused;"
                })
                .Build();

            var fakeDb = new FakeDbProvider(() => dt.CreateDataReader());

            // Act
            var repo = new TeleportTableRepository(config, fakeDb);

            // Assert (none — helper)
            return repo;
        }

        /// <summary>
        /// Builds a repository using a scripted <see cref="FakeDbProvider"/> for POST/PUT/DELETE scenarios.
        /// </summary>
        private static TeleportTableRepository BuildRepo(FakeDbProvider fakeDb)
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Server=unused;Uid=unused;Pwd=unused;Database=unused;"
                })
                .Build();

            // Act
            var repo = new TeleportTableRepository(config, fakeDb);

            // Assert (none — helper)
            return repo;
        }

        #region GET TESTS

        // -------- data sources for parameterized tests --------

        /// <summary>
        /// Yields sets of (locale|value) pairs to validate table-level i18n aggregation.
        /// </summary>
        public static IEnumerable<object[]> TableLocaleSets()
        {
            yield return new object[] { new[] { "en-US|Teleport" } };
            yield return new object[] { new[] { "en-US|Teleport", "fr-FR|Téléportation" } };
            yield return new object[] { new[] { "en-US|Tele", "de-DE|Teleportieren", "fr-FR|Téléportation" } };
        }

        /// <summary>
        /// Yields button rows with/without activity to validate button projection sans i18n.
        /// </summary>
        public static IEnumerable<object[]> ButtonNoI18nCases()
        {
            yield return new object[] { 10, "btn.play", true };
            yield return new object[] { 11, "btn.stop", false };
        }

        /// <summary>
        /// Yields map spot variations to validate map coordinate population.
        /// </summary>
        public static IEnumerable<object[]> MapSpotCases()
        {
            // buttonId, nameKey, active, mapSpotId, x, y, z
            yield return new object[] { 42, "btn.map", true, 777, 1.25m, -3.5m, 10m };
            yield return new object[] { 99, "btn.loc", false, 15, 0m, 0m, 0m };
            yield return new object[] { 7, "btn.neg", true, 3, -12.345m, 999.999m, -0.001m };
        }

        /// <summary>
        /// Ensures null is returned when the query yields no rows.
        /// </summary>
        [Theory]
        [InlineData(123)]
        [InlineData(1)]
        public async Task Get_ReturnsNull_WhenNoRows(int spaceId)
        {
            // Arrange
            var dt = TeleportSchema.CreateTeleportSelectSchema();
            var sut = BuildRepo(dt);

            // Act
            var result = await sut.GetTeleportTableBySpaceAsync(spaceId);

            // Assert
            result.Should().BeNull("no rows were returned by the data reader");
        }

        /// <summary>
        /// Verifies table i18n aggregation without any buttons.
        /// </summary>
        [Theory]
        [MemberData(nameof(TableLocaleSets))]
        public async Task Get_ReturnsTable_WithTableI18n_NoButtons(string[] tableLocales)
        {
            // Arrange
            var dt = TeleportSchema.CreateTeleportSelectSchema();

            foreach (var pair in tableLocales)
            {
                var parts = pair.Split('|', 2);
                var locale = parts[0];
                var value = parts.Length > 1 ? parts[1] : "";
                TestDataRowBuilder.TeleportSelectRow()
                    .WithTeleportTableDefaults()
                    .WithTableLocale(locale, value)
                    .AddTo(dt);
            }

            var sut = BuildRepo(dt);

            // Act
            var result = await sut.GetTeleportTableBySpaceAsync(123);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.SpaceId.Should().Be(123);
            result.NameKey.Should().Be("teleport.table");
            result.IsActive.Should().BeTrue();

            result.LocalizedPairs.Values
                .Select(v => $"{v.LocaleId}|{v.Value}")
                .Should()
                .BeEquivalentTo(tableLocales, opts => opts.WithoutStrictOrdering());

            result.Buttons.Should().NotBeNull().And.BeEmpty();
        }

        /// <summary>
        /// Projects a single button without i18n or map spot.
        /// </summary>
        [Theory]
        [MemberData(nameof(ButtonNoI18nCases))]
        public async Task Get_ReturnsTable_WithSingleButton_NoI18n_NoMapSpot(int buttonId, string nameKey, bool isActive)
        {
            // Arrange
            var dt = TeleportSchema.CreateTeleportSelectSchema();

            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults()
                .WithButton(buttonId, nameKey, isActive)
                .AddTo(dt);

            var sut = BuildRepo(dt);

            // Act
            var result = await sut.GetTeleportTableBySpaceAsync(123);

            // Assert
            result.Should().NotBeNull();
            result!.Buttons.Should().HaveCount(1);

            var b = result.Buttons[0];
            b.Id.Should().Be(buttonId);
            b.NameKey.Should().Be(nameKey);
            b.IsActive.Should().Be(isActive);
            b.LocalizedPairs.Values.Should().BeEmpty();
            b.MapSpot.Should().BeNull();
        }

        /// <summary>
        /// Aggregates multiple buttons and deduplicates repeated button i18n rows.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Get_Aggregates_MultipleButtons_And_MultiLocaleButtonI18n_DedupesLocales(bool includeDuplicateFr)
        {
            // Arrange
            var dt = TeleportSchema.CreateTeleportSelectSchema();

            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults()
                .WithTableLocale("en-US", "Teleport Table")
                .AddTo(dt);

            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults()
                .WithButton(10, "btn.one", true)
                .WithButtonLocale("en-US", "One")
                .AddTo(dt);

            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults()
                .WithButton(10, "btn.one", true)
                .WithButtonLocale("fr-FR", "Un")
                .AddTo(dt);

            if (includeDuplicateFr)
            {
                TestDataRowBuilder.TeleportSelectRow()
                    .WithTeleportTableDefaults()
                    .WithButton(10, "btn.one", true)
                    .WithButtonLocale("fr-FR", "Un") // duplicate row
                    .AddTo(dt);
            }

            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults()
                .WithButton(11, "btn.two", false)
                .WithButtonLocale("en-US", "Two")
                .AddTo(dt);

            var sut = BuildRepo(dt);

            // Act
            var result = await sut.GetTeleportTableBySpaceAsync(123);

            // Assert
            result.Should().NotBeNull();
            result!.Buttons.Should().HaveCount(2);

            var b10 = result.Buttons.Single(b => b.Id == 10);
            b10.NameKey.Should().Be("btn.one");
            b10.IsActive.Should().BeTrue();
            b10.LocalizedPairs.Values.Should().BeEquivalentTo(
                new[]
                {
                    new { LocaleId = "en-US", Value = "One" },
                    new { LocaleId = "fr-FR", Value = "Un" }
                },
                opts => opts.WithoutStrictOrdering()
            );

            var b11 = result.Buttons.Single(b => b.Id == 11);
            b11.NameKey.Should().Be("btn.two");
            b11.IsActive.Should().BeFalse();
            b11.LocalizedPairs.Values.Should()
                .ContainSingle(v => v.LocaleId == "en-US" && v.Value == "Two");
        }

        /// <summary>
        /// Projects a button with an attached map spot and verifies coordinates.
        /// </summary>
        [Theory]
        [MemberData(nameof(MapSpotCases))]
        public async Task Get_Button_WithMapSpot_PopulatesCoordinates(
            int buttonId, string nameKey, bool isActive, int mapSpotId, decimal x, decimal y, decimal z)
        {
            // Arrange
            var dt = TeleportSchema.CreateTeleportSelectSchema();

            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults()
                .WithButton(buttonId, nameKey, isActive)
                .WithMapSpot(mapSpotId, x, y, z)
                .AddTo(dt);

            var sut = BuildRepo(dt);

            // Act
            var result = await sut.GetTeleportTableBySpaceAsync(123);

            // Assert
            result.Should().NotBeNull();
            var btn = result!.Buttons.Should().ContainSingle().Subject;
            btn.MapSpot.Should().NotBeNull();
            btn.MapSpot!.Id.Should().Be(mapSpotId);
            btn.MapSpot.X.Should().Be(x);
            btn.MapSpot.Y.Should().Be(y);
            btn.MapSpot.Z.Should().Be(z);
        }

        /// <summary>
        /// Ignores null table/button i18n pairs while still building the object graph.
        /// </summary>
        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task Get_Ignores_Null_TableAndButton_I18n_Pairs(bool nullTableI18n, bool nullButtonI18n)
        {
            // Arrange
            var dt = TeleportSchema.CreateTeleportSelectSchema();

            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults()
                .WithTableLocale(
                    localeId: nullTableI18n ? null : "en-US",
                    value: nullTableI18n ? null : "Teleport")
                .AddTo(dt);

            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults()
                .WithButton(5, "btn.nulls", true)
                .WithButtonLocale(
                    localeId: nullButtonI18n ? null : "en-US",
                    value: nullButtonI18n ? null : "Nulls")
                .AddTo(dt);

            var sut = BuildRepo(dt);

            // Act
            var result = await sut.GetTeleportTableBySpaceAsync(123);

            // Assert
            result.Should().NotBeNull();

            if (nullTableI18n)
                result!.LocalizedPairs.Values.Should().BeEmpty("null table i18n must be ignored");
            else
                result!.LocalizedPairs.Values.Should().ContainSingle(v => v.LocaleId == "en-US" && v.Value == "Teleport");

            result.Buttons.Should().ContainSingle();

            if (nullButtonI18n)
                result.Buttons[0].LocalizedPairs.Values.Should().BeEmpty("null button i18n must be ignored");
            else
                result.Buttons[0].LocalizedPairs.Values.Should().ContainSingle(v => v.LocaleId == "en-US" && v.Value == "Nulls");
        }

        /// <summary>
        /// When no button rows exist, verifies Buttons is an empty list (not null).
        /// </summary>
        [Theory]
        [InlineData("en-US", "Only Table")]
        [InlineData("fr-FR", "Seulement Table")]
        public async Task Get_NoButtons_Yields_EmptyButtonsList_NotNull(string tableLocale, string tableValue)
        {
            // Arrange
            var dt = TeleportSchema.CreateTeleportSelectSchema();

            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults()
                .WithTableLocale(tableLocale, tableValue)
                .AddTo(dt);

            var sut = BuildRepo(dt);

            // Act
            var result = await sut.GetTeleportTableBySpaceAsync(123);

            // Assert
            result.Should().NotBeNull();
            result!.Buttons.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region POST TESTS

        // ---------- helpers (for POST) ----------

        /// <summary>
        /// Counts INSERT statements for the given table name.
        /// </summary>
        private static int CountInsertFor(FakeDbProvider db, string table) =>
            db.ExecutedSql.Count(s =>
                s.Contains($"INSERT INTO {table} ", StringComparison.OrdinalIgnoreCase) ||
                s.Contains($"INSERT INTO {table}(", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Asserts expected INSERT counts across key tables for create flows.
        /// </summary>
        private static void AssertInsertCounts(
            FakeDbProvider db,
            int teleportTable = 0,
            int mapSpot = 0,
            int teleportButton = 0,
            int i18n = 0,
            int lastId = 0)
        {
            CountInsertFor(db, "teleport_table").Should().Be(teleportTable);
            CountInsertFor(db, "map_spot").Should().Be(mapSpot);
            CountInsertFor(db, "teleport_table_button").Should().Be(teleportButton);
            db.ExecutedSql.Count(s => s.Contains("INSERT INTO i18n", StringComparison.OrdinalIgnoreCase)).Should().Be(i18n);
            db.ExecutedSql.Count(s => s.Contains("SELECT LAST_INSERT_ID()", StringComparison.OrdinalIgnoreCase)).Should().Be(lastId);
        }

        /// <summary>
        /// Ensures null DTO throws and no transaction is attempted.
        /// </summary>
        [Fact]
        public async Task Create_Throws_ArgumentNull_WhenDtoIsNull()
        {
            // Arrange
            var fakeDb = new FakeDbProvider(() => new DataTable().CreateDataReader());
            var sut = BuildRepo(fakeDb);

            // Act
            Func<Task> act = async () => await sut.CreateTeleportTableAsync(123, null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
            fakeDb.Commits.Should().Be(0);
            fakeDb.Rollbacks.Should().Be(0);
        }

        /// <summary>
        /// Creates a table with no i18n and no buttons, validating commit.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Create_Table_NoI18n_NoButtons_Commits(bool isActive)
        {
            // Arrange
            var fakeDb = new FakeDbProvider(
                () => new DataTable().CreateDataReader(),
                scalarResults: new Queue<object>(new object[] { 100 })
            );

            var dto = TeleportDtoBuilders.Default()
                .WithNameKey("teleport.table")
                .WithIsActive(isActive)
                .Build();

            var sut = BuildRepo(fakeDb);

            // Act
            var created = await sut.CreateTeleportTableAsync(123, dto);

            // Assert
            created.Id.Should().Be(100);
            created.SpaceId.Should().Be(123);
            created.NameKey.Should().Be("teleport.table");
            created.IsActive.Should().Be(isActive);
            created.Buttons.Should().BeEmpty();
            created.LocalizedPairs.Should().BeNull();

            AssertInsertCounts(fakeDb,
                teleportTable: 1,
                mapSpot: 0,
                teleportButton: 0,
                i18n: 0,
                lastId: 1);

            fakeDb.Commits.Should().Be(1);
            fakeDb.Rollbacks.Should().Be(0);
        }

        /// <summary>
        /// Inserts all provided table locales and verifies i18n row count.
        /// </summary>
        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        public async Task Create_Table_With_I18n_Inserts_All_Locales(int localeCount)
        {
            // Arrange
            var fakeDb = new FakeDbProvider(
                () => new DataTable().CreateDataReader(),
                scalarResults: new Queue<object>(new object[] { 101 })
            );

            var locales = Enumerable.Range(1, localeCount)
                .Select(i => ($"l{i}", $"Name {i}"))
                .ToArray();

            var dto = TeleportDtoBuilders.Default()
                .WithNameKey("teleport.name")
                .WithIsActive(true)
                .WithTableLocales(locales)
                .Build();

            var sut = BuildRepo(fakeDb);

            // Act
            var created = await sut.CreateTeleportTableAsync(123, dto);

            // Assert
            created.Id.Should().Be(101);
            created.LocalizedPairs!.Key.Should().Be("teleport.name");
            created.LocalizedPairs!.Values.Should().HaveCount(localeCount);
            created.LocalizedPairs!.Values
                .Select(v => $"{v.LocaleId}|{v.Value}")
                .Should()
                .BeEquivalentTo(locales.Select(t => $"{t.Item1}|{t.Item2}"));

            AssertInsertCounts(fakeDb,
                teleportTable: 1,
                i18n: localeCount,
                lastId: 1);

            fakeDb.Commits.Should().Be(1);
            fakeDb.Rollbacks.Should().Be(0);
        }

        /// <summary>
        /// Adds a button without map spot or i18n; verifies graph + SQL.
        /// </summary>
        [Fact]
        public async Task Create_Table_And_Button_Without_MapSpot_No_I18n()
        {
            // Arrange
            var fakeDb = new FakeDbProvider(
                () => new DataTable().CreateDataReader(),
                scalarResults: new Queue<object>(new object[] { 200, 300 })
            );

            var dto = TeleportDtoBuilders.Default()
                .WithNameKey("teleport.table")
                .WithIsActive(true)
                .AddButton(ButtonCreateDtoBuilder.Default()
                    .WithNameKey("btn.play")
                    .WithIsActive(true)
                    .Build())
                .Build();

            var sut = BuildRepo(fakeDb);

            // Act
            var created = await sut.CreateTeleportTableAsync(123, dto);

            // Assert
            created.Id.Should().Be(200);
            created.Buttons.Should().ContainSingle();
            var b = created.Buttons[0];
            b.Id.Should().Be(300);
            b.NameKey.Should().Be("btn.play");
            b.IsActive.Should().BeTrue();
            b.MapSpot.Should().BeNull();
            b.LocalizedPairs.Values.Should().BeEmpty();

            AssertInsertCounts(fakeDb,
                teleportTable: 1,
                teleportButton: 1,
                i18n: 0,
                lastId: 2);

            fakeDb.Commits.Should().Be(1);
            fakeDb.Rollbacks.Should().Be(0);
        }

        /// <summary>
        /// Adds a button with a new map spot and i18n; validates object and SQL counts.
        /// </summary>
        [Fact]
        public async Task Create_Table_And_Button_With_New_MapSpot_And_I18n()
        {
            // Arrange
            var fakeDb = new FakeDbProvider(
                () => new DataTable().CreateDataReader(),
                scalarResults: new Queue<object>(new object[] { 500, 600, 601 })
            );

            var dto = TeleportDtoBuilders.Default()
                .WithNameKey("teleport.table")
                .WithIsActive(true)
                .WithTableLocales(("en-US", "Teleport"))
                .AddButton(ButtonCreateDtoBuilder.Default()
                    .WithNameKey("btn.map")
                    .WithIsActive(true)
                    .WithNewMapSpot(1.25m, -3.50m, 10m)
                    .WithButtonLocales(("en-US", "Map"))
                    .Build())
                .Build();

            var sut = BuildRepo(fakeDb);

            // Act
            var created = await sut.CreateTeleportTableAsync(123, dto);

            // Assert
            created.Id.Should().Be(500);
            created.LocalizedPairs!.Values.Should()
                .ContainSingle(l => l.LocaleId == "en-US" && l.Value == "Teleport");

            var btn = created.Buttons.Should().ContainSingle().Subject;
            btn.Id.Should().Be(601);
            btn.NameKey.Should().Be("btn.map");
            btn.IsActive.Should().BeTrue();
            btn.MapSpot!.Id.Should().Be(600);
            btn.MapSpot.X.Should().Be(1.25m);
            btn.MapSpot.Y.Should().Be(-3.50m);
            btn.MapSpot.Z.Should().Be(10m);
            btn.LocalizedPairs.Values.Should()
                .ContainSingle(v => v.LocaleId == "en-US" && v.Value == "Map");

            fakeDb.ExecutedSql.Count(s =>
                s.Contains("INSERT INTO teleport_table ", StringComparison.OrdinalIgnoreCase) ||
                s.Contains("INSERT INTO teleport_table(", StringComparison.OrdinalIgnoreCase)
            ).Should().Be(1);

            fakeDb.ExecutedSql.Count(s =>
                s.Contains("INSERT INTO map_spot", StringComparison.OrdinalIgnoreCase)
            ).Should().Be(1);

            fakeDb.ExecutedSql.Count(s =>
                s.Contains("INSERT INTO teleport_table_button", StringComparison.OrdinalIgnoreCase)
            ).Should().Be(1);

            fakeDb.ExecutedSql.Count(s =>
                s.Contains("INSERT INTO i18n", StringComparison.OrdinalIgnoreCase)
            ).Should().Be(2); // table + button

            fakeDb.ExecutedSql.Count(s =>
                s.Contains("SELECT LAST_INSERT_ID()", StringComparison.OrdinalIgnoreCase)
            ).Should().Be(3);

            fakeDb.Commits.Should().Be(1);
            fakeDb.Rollbacks.Should().Be(0);
        }

        /// <summary>
        /// Uses an existing map spot id (no INSERT into map_spot expected).
        /// </summary>
        [Fact]
        public async Task Create_Uses_Existing_MapSpotId_No_MapSpot_Insert()
        {
            // Arrange
            var fakeDb = new FakeDbProvider(
                () => new DataTable().CreateDataReader(),
                scalarResults: new Queue<object>(new object[] { 800, 900 })
            );

            var dto = TeleportDtoBuilders.Default()
                .WithNameKey("teleport.table")
                .WithIsActive(true)
                .AddButton(ButtonCreateDtoBuilder.Default()
                    .WithNameKey("btn.existing")
                    .WithIsActive(true)
                    .WithExistingMapSpotId(777)
                    .Build())
                .Build();

            var sut = BuildRepo(fakeDb);

            // Act
            var created = await sut.CreateTeleportTableAsync(123, dto);

            // Assert
            created.Id.Should().Be(800);
            var btn = created.Buttons.Should().ContainSingle().Subject;
            btn.Id.Should().Be(900);
            btn.MapSpot!.Id.Should().Be(777);
            btn.MapSpot.X.Should().Be(0);
            btn.MapSpot.Y.Should().Be(0);
            btn.MapSpot.Z.Should().Be(0);

            fakeDb.ExecutedSql.Any(s => s.Contains("INSERT INTO map_spot", StringComparison.OrdinalIgnoreCase))
                 .Should().BeFalse();
            fakeDb.ExecutedSql.Count(s => s.Contains("SELECT LAST_INSERT_ID()", StringComparison.OrdinalIgnoreCase))
                 .Should().Be(2);
            fakeDb.Commits.Should().Be(1);
            fakeDb.Rollbacks.Should().Be(0);
        }

        /// <summary>
        /// Rolls back the transaction if the button INSERT fails.
        /// </summary>
        [Fact]
        public async Task Create_RollsBack_When_Button_Insert_Fails()
        {
            // Arrange
            var fakeDb = new FakeDbProvider(
                () => new DataTable().CreateDataReader(),
                scalarResults: new Queue<object>(new object[] { 1234 })
            )
            {
                ShouldThrowOnNonQuery = sql => sql.Contains("INSERT INTO teleport_table_button", StringComparison.OrdinalIgnoreCase),
                NonQueryException = new InvalidOperationException("error during button insert")
            };

            var dto = TeleportDtoBuilders.Default()
                .WithNameKey("teleport.table")
                .WithIsActive(true)
                .AddButton(ButtonCreateDtoBuilder.Default()
                    .WithNameKey("btn.bad")
                    .WithIsActive(true)
                    .Build())
                .Build();

            var sut = BuildRepo(fakeDb);

            // Act
            var act = async () => await sut.CreateTeleportTableAsync(123, dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*error during button insert*");

            fakeDb.Commits.Should().Be(0);
            fakeDb.Rollbacks.Should().Be(1);
        }

        /// <summary>
        /// Validates guard: null button DTO throws without touching transactions.
        /// </summary>
        [Fact]
        public async Task CreateButton_Throws_ArgumentNull_WhenDtoIsNull()
        {
            // Arrange
            var fakeDb = new FakeDbProvider(() => new DataTable().CreateDataReader());
            var sut = BuildRepo(fakeDb);

            // Act
            var act = async () => await sut.CreateTeleportTableButtonAsync(123, 77, null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
            fakeDb.Commits.Should().Be(0);
            fakeDb.Rollbacks.Should().Be(0);
        }

        /// <summary>
        /// Internal transaction path: commits on success.
        /// </summary>
        [Fact]
        public async Task CreateButton_InternalTx_Commits_OnSuccess()
        {
            // Arrange
            var fakeDb = new FakeDbProvider(
                () => new DataTable().CreateDataReader(),
                scalarResults: new Queue<object?>(new object?[] { 600, 601 })
            );

            var sut = BuildRepo(fakeDb);
            var dto = ButtonCreateDtoBuilder.Default()
                .WithNameKey("btn.ok")
                .WithIsActive(true)
                .WithNewMapSpot(1.1m, 2.2m, 3.3m)
                .WithButtonLocales(("en-US", "OK"))
                .Build();

            // Act
            var created = await sut.CreateTeleportTableButtonAsync(123, 77, dto);

            // Assert
            created.Id.Should().Be(601);
            created.MapSpot!.Id.Should().Be(600);
            fakeDb.Commits.Should().Be(1);
            fakeDb.Rollbacks.Should().Be(0);
        }

        /// <summary>
        /// Internal transaction path: rolls back when button insert fails.
        /// </summary>
        [Fact]
        public async Task CreateButton_InternalTx_Rollback_OnFailure()
        {
            // Arrange
            var fakeDb = new FakeDbProvider(
                () => new DataTable().CreateDataReader(),
                scalarResults: new Queue<object?>(new object?[] { 700 })
            )
            {
                ShouldThrowOnNonQuery = sql =>
                    WS(sql).Contains("INSERT INTO teleport_table_button", StringComparison.OrdinalIgnoreCase),
                NonQueryException = new InvalidOperationException("error during button insert")
            };

            var sut = BuildRepo(fakeDb);
            var dto = ButtonCreateDtoBuilder.Default()
                .WithNameKey("btn.fail")
                .WithIsActive(true)
                .WithNewMapSpot(9m, 9m, 9m)
                .Build();

            // Act
            var act = async () => await sut.CreateTeleportTableButtonAsync(123, 77, dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*button insert*");
            fakeDb.Commits.Should().Be(0);
            fakeDb.Rollbacks.Should().Be(1);
        }

        /// <summary>
        /// External transaction path: the method must not commit/rollback.
        /// </summary>
        [Fact]
        public async Task CreateButton_ExternalTx_Skips_InternalCommitRollback()
        {
            // Arrange
            var fakeDb = new FakeDbProvider(
                () => new DataTable().CreateDataReader(),
                scalarResults: new Queue<object?>(new object?[] { 800, 801 })
            );

            var sut = BuildRepo(fakeDb);
            var extConn = await fakeDb.OpenConnectionAsync();
            var extTx = await extConn.BeginTransactionAsync();

            var dto = ButtonCreateDtoBuilder.Default()
                .WithNameKey("btn.ext")
                .WithIsActive(true)
                .WithNewMapSpot(4m, 5m, 6m)
                .Build();

            // Act
            var created = await sut.CreateTeleportTableButtonAsync(123, 77, dto, extConn, extTx);

            // Assert
            created.Id.Should().Be(801);
            created.MapSpot!.Id.Should().Be(800);
            fakeDb.Commits.Should().Be(0);
            fakeDb.Rollbacks.Should().Be(0);
        }

        /// <summary>
        /// External transaction path: even on failure, method must not rollback internally.
        /// </summary>
        [Fact]
        public async Task CreateButton_ExternalTx_Failure_DoesNotRollbackInternally()
        {
            // Arrange
            var fakeDb = new FakeDbProvider(
                () => new DataTable().CreateDataReader(),
                scalarResults: new Queue<object?>(new object?[] { 900 })
            )
            {
                ShouldThrowOnNonQuery = sql =>
                    WS(sql).Contains("INSERT INTO teleport_table_button", StringComparison.OrdinalIgnoreCase),
                NonQueryException = new InvalidOperationException("error in external transaction")
            };

            var sut = BuildRepo(fakeDb);
            var extConn = await fakeDb.OpenConnectionAsync();
            var extTx = await extConn.BeginTransactionAsync();

            var dto = ButtonCreateDtoBuilder.Default()
                .WithNameKey("btn.problem")
                .WithIsActive(true)
                .WithNewMapSpot(1m, 2m, 3m)
                .Build();

            // Act
            var act = async () => await sut.CreateTeleportTableButtonAsync(123, 77, dto, extConn, extTx);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*external transaction*");
            fakeDb.Commits.Should().Be(0);
            fakeDb.Rollbacks.Should().Be(0);
        }

        #endregion

        #region PUT TESTS

        // small helpers (PUT)

        /// <summary>
        /// Creates a reader that returns the provided supported language codes.
        /// </summary>
        private static DbDataReader SupportedLangs(params string[] locales)
        {
            var t = new DataTable();
            t.Columns.Add("locale_id", typeof(string));
            foreach (var loc in locales) t.Rows.Add(loc);
            return t.CreateDataReader();
        }

        /// <summary>
        /// Returns null when UPDATE affects zero rows and existence check fails.
        /// </summary>
        [Theory]
        [InlineData(123, 999)]
        [InlineData(5, 0)]
        public async Task Put_ReturnsNull_WhenTableNotFound(int spaceId, int tableId)
        {
            // Arrange
            var fakeDb = new FakeDbProvider(() => new DataTable().CreateDataReader());
            fakeDb.EnqueueReader(() => SupportedLangs()); // supported_languages
            fakeDb.EnqueueNonQuery(0);                    // UPDATE teleport_table -> 0
            fakeDb.EnqueueScalar(0);                      // existence check -> not found

            var sut = BuildRepo(fakeDb);
            var dto = new TeleportTableUpdateDto
            {
                NameKey = "teleport.table",
                IsActive = true,
                LocalizedPairs = null,
                Buttons = null
            };

            // Act
            var result = await sut.UpdateTeleportTableAsync(spaceId, tableId, dto);

            // Assert
            result.Should().BeNull();
            fakeDb.Commits.Should().Be(0);
            fakeDb.Rollbacks.Should().Be(1);
        }

        /// <summary>
        /// When no data changes but table exists, upserts table i18n and commits.
        /// </summary>
        [Theory]
        [InlineData("Teleport New", true)]
        [InlineData("Renamed", false)]
        public async Task Put_NoChangesButExists_Upserts_Table_I18n_And_Commits(string newNameLocalized, bool isActive)
        {
            // Arrange
            var fakeDb = new FakeDbProvider(() => new DataTable().CreateDataReader());

            fakeDb.EnqueueReader(() => SupportedLangs("en-US")); // supported_languages
            fakeDb.EnqueueNonQuery(0);                           // UPDATE teleport_table -> 0 (no change)
            fakeDb.EnqueueScalar(1);                             // existence check -> exists
            fakeDb.EnqueueNonQuery(0);                           // UPDATE i18n -> 0 then INSERT

            var dt = TeleportSchema.CreateTeleportSelectSchema(); // reload
            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults(tableId: 1, spaceId: 123, isActive: isActive, tableNameKey: "teleport.table")
                .WithTableLocale("en-US", newNameLocalized)
                .AddTo(dt);
            fakeDb.EnqueueReader(() => dt.CreateDataReader());

            var sut = BuildRepo(fakeDb);
            var dto = new TeleportTableUpdateDto
            {
                NameKey = "teleport.table",
                IsActive = isActive,
                LocalizedPairs = new LocalizedPairs
                {
                    Key = "teleport.table",
                    Values = new List<LocalizedValue> { new LocalizedValue { LocaleId = "en-US", Value = newNameLocalized } }
                },
                Buttons = null
            };

            // Act
            var updated = await sut.UpdateTeleportTableAsync(123, 1, dto);

            // Assert
            updated.Should().NotBeNull();
            updated!.Id.Should().Be(1);
            updated.IsActive.Should().Be(isActive);
            updated.LocalizedPairs.Values.Should()
                .ContainSingle(v => v.LocaleId == "en-US" && v.Value == newNameLocalized);

            fakeDb.Commits.Should().Be(1);
            fakeDb.Rollbacks.Should().Be(0);
        }

        /// <summary>
        /// Updates table and mixed button set (update existing, insert new), including map spots and i18n upserts.
        /// </summary>
        [Fact]
        public async Task Put_TableAndButtons_Mixed_Update_Insert_MapSpotAndI18n()
        {
            // Arrange
            var fakeDb = new FakeDbProvider(() => new DataTable().CreateDataReader());

            fakeDb.EnqueueReader(() => SupportedLangs("en-US", "es-ES"));
            fakeDb.EnqueueNonQuery(1); // UPDATE teleport_table

            // table i18n upserts
            fakeDb.EnqueueNonQuery(1); // en-US update
            fakeDb.EnqueueNonQuery(0); // es-ES update -> then insert

            // existing button (id=10)
            fakeDb.EnqueueNonQuery(1); // update button
            fakeDb.EnqueueNonQuery(1); // update map spot
            fakeDb.EnqueueNonQuery(0); // i18n en-US update -> insert
            fakeDb.EnqueueNonQuery(1); // i18n es-ES update

            // new button
            fakeDb.EnqueueNonQuery(1); // insert button
            fakeDb.EnqueueScalar(999); // LAST_INSERT_ID
            fakeDb.EnqueueNonQuery(1); // update map spot
            fakeDb.EnqueueNonQuery(0); // i18n en-US update -> insert

            // reload
            var dt = TeleportSchema.CreateTeleportSelectSchema();
            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults(tableId: 77, spaceId: 123, isActive: true, tableNameKey: "teleport.table")
                .WithTableLocale("en-US", "T-EN")
                .AddTo(dt);
            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults(tableId: 77, spaceId: 123, isActive: true, tableNameKey: "teleport.table")
                .WithTableLocale("es-ES", "T-ES")
                .AddTo(dt);

            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults(tableId: 77, spaceId: 123, isActive: true, tableNameKey: "teleport.table")
                .WithButton(10, "btn.play", true)
                .WithMapSpot(777, 1.1m, 2.2m, 3.3m)
                .WithButtonLocale("en-US", "Play")
                .AddTo(dt);
            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults(tableId: 77, spaceId: 123, isActive: true, tableNameKey: "teleport.table")
                .WithButton(10, "btn.play", true)
                .WithMapSpot(777, 1.1m, 2.2m, 3.3m)
                .WithButtonLocale("es-ES", "Jugar")
                .AddTo(dt);

            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults(tableId: 77, spaceId: 123, isActive: true, tableNameKey: "teleport.table")
                .WithButton(999, "btn.new", false)
                .WithMapSpot(888, 9m, 8m, 7m)
                .WithButtonLocale("en-US", "New")
                .AddTo(dt);

            fakeDb.EnqueueReader(() => dt.CreateDataReader());

            var sut = BuildRepo(fakeDb);
            var dto = new TeleportTableUpdateDto
            {
                NameKey = "teleport.table",
                IsActive = true,
                LocalizedPairs = new LocalizedPairs
                {
                    Key = "teleport.table",
                    Values = new List<LocalizedValue>
                    {
                        new LocalizedValue { LocaleId = "en-US", Value = "T-EN" },
                        new LocalizedValue { LocaleId = "es-ES", Value = "T-ES" }
                    }
                },
                Buttons = new List<ButtonUpdateDto>
                {
                    new ButtonUpdateDto
                    {
                        Id = 10,
                        NameKey = "btn.play",
                        IsActive = true,
                        MapSpot = new MapSpotData { Id = 777, X = 1.1m, Y = 2.2m, Z = 3.3m },
                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = "btn.play",
                            Values = new List<LocalizedValue>
                            {
                                new LocalizedValue { LocaleId = "en-US", Value = "Play" },
                                new LocalizedValue { LocaleId = "es-ES", Value = "Jugar" }
                            }
                        }
                    },
                    new ButtonUpdateDto
                    {
                        NameKey = "btn.new",
                        IsActive = false,
                        MapSpot = new MapSpotData { Id = 888, X = 9m, Y = 8m, Z = 7m },
                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = "btn.new",
                            Values = new List<LocalizedValue>
                            {
                                new LocalizedValue { LocaleId = "en-US", Value = "New" }
                            }
                        }
                    }
                }
            };

            // Act
            var updated = await sut.UpdateTeleportTableAsync(123, 77, dto);

            // Assert
            updated.Should().NotBeNull();
            updated!.Id.Should().Be(77);
            updated.LocalizedPairs.Values.Select(v => v.LocaleId)
                .Should().BeEquivalentTo(new[] { "en-US", "es-ES" });
            updated.Buttons.Should().HaveCount(2);

            var bPlay = updated.Buttons.Single(b => b.Id == 10);
            bPlay.MapSpot!.X.Should().Be(1.1m);
            bPlay.LocalizedPairs.Values.Select(v => v.LocaleId)
                 .Should().BeEquivalentTo(new[] { "en-US", "es-ES" });

            var bNew = updated.Buttons.Single(b => b.Id == 999);
            bNew.IsActive.Should().BeFalse();
            bNew.MapSpot!.Id.Should().Be(888);
            bNew.LocalizedPairs.Values.Should().ContainSingle(v => v.LocaleId == "en-US" && v.Value == "New");

            fakeDb.Commits.Should().Be(1);
            fakeDb.Rollbacks.Should().Be(0);
        }

        /// <summary>
        /// Skips unsupported/blank locales for table and button i18n; commits.
        /// </summary>
        [Fact]
        public async Task Put_SkipsUnsupportedAndBlankLocales_For_Table_And_Buttons()
        {
            // Arrange
            var fakeDb = new FakeDbProvider(() => new DataTable().CreateDataReader())
            {
                // Guard: ensure no accidental map_spot update occurs
                ShouldThrowOnNonQuery = sql => sql.Contains("UPDATE map_spot", StringComparison.OrdinalIgnoreCase)
            };

            fakeDb.EnqueueReader(() => SupportedLangs("en-US"));
            fakeDb.EnqueueNonQuery(1); // UPDATE teleport_table
            fakeDb.EnqueueNonQuery(0); // UPDATE i18n -> then INSERT

            fakeDb.EnqueueNonQuery(1); // UPDATE existing button
            fakeDb.EnqueueNonQuery(0); // Button i18n en-US -> update 0 then insert

            var dt = TeleportSchema.CreateTeleportSelectSchema();
            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults()
                .WithTableLocale("en-US", "Only EN")
                .AddTo(dt);
            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults()
                .WithButton(10, "btn.one", true)
                .WithButtonLocale("en-US", "One EN")
                .AddTo(dt);
            fakeDb.EnqueueReader(() => dt.CreateDataReader());

            var sut = BuildRepo(fakeDb);
            var dto = new TeleportTableUpdateDto
            {
                NameKey = "teleport.table",
                IsActive = true,
                LocalizedPairs = new LocalizedPairs
                {
                    Key = "teleport.table",
                    Values = new List<LocalizedValue>
                    {
                        new LocalizedValue { LocaleId = "",      Value = "X" },     // ignored
                        new LocalizedValue { LocaleId = "  ",    Value = "Y" },     // ignored
                        new LocalizedValue { LocaleId = null!,   Value = "Z" },     // ignored
                        new LocalizedValue { LocaleId = "fr-FR", Value = "FR" },    // unsupported
                        new LocalizedValue { LocaleId = "en-US", Value = "Only EN"} // handled
                    }
                },
                Buttons = new List<ButtonUpdateDto>
                {
                    new ButtonUpdateDto
                    {
                        Id = 10,
                        NameKey = "btn.one",
                        IsActive = true,
                        MapSpot = null,
                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = "btn.one",
                            Values = new List<LocalizedValue>
                            {
                                new LocalizedValue { LocaleId = null!,   Value = "X" }, // ignored
                                new LocalizedValue { LocaleId = "  ",    Value = "Y" }, // ignored
                                new LocalizedValue { LocaleId = "fr-FR", Value = "FR"}, // unsupported
                                new LocalizedValue { LocaleId = "en-US", Value = "One EN"} // handled
                            }
                        }
                    }
                }
            };

            // Act
            var updated = await sut.UpdateTeleportTableAsync(123, 1, dto);

            // Assert
            updated.Should().NotBeNull();
            updated!.LocalizedPairs.Values.Should().ContainSingle(v => v.LocaleId == "en-US" && v.Value == "Only EN");
            updated.Buttons.Should().ContainSingle(b => b.Id == 10);
            updated.Buttons[0].LocalizedPairs.Values.Should().ContainSingle(v => v.LocaleId == "en-US" && v.Value == "One EN");
            fakeDb.Commits.Should().Be(1);
            fakeDb.Rollbacks.Should().Be(0);
        }

        /// <summary>
        /// New button with null map spot must not trigger a map_spot UPDATE.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Put_NewButton_NullMapSpot_NoMapSpotUpdate(bool isActive)
        {
            // Arrange
            var fakeDb = new FakeDbProvider(() => new DataTable().CreateDataReader())
            {
                // Guard: fail if any UPDATE map_spot happens
                ShouldThrowOnNonQuery = sql => sql.Contains("UPDATE map_spot", StringComparison.OrdinalIgnoreCase)
            };

            fakeDb.EnqueueReader(() => SupportedLangs("en-US"));
            fakeDb.EnqueueNonQuery(1); // UPDATE teleport_table
            fakeDb.EnqueueNonQuery(1); // INSERT new button
            fakeDb.EnqueueScalar(333);

            var dt = TeleportSchema.CreateTeleportSelectSchema();
            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults(isActive: isActive)
                .AddTo(dt);
            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults(isActive: isActive)
                .WithButton(333, "btn.add", true)
                .AddTo(dt);
            fakeDb.EnqueueReader(() => dt.CreateDataReader());

            var sut = BuildRepo(fakeDb);
            var dto = new TeleportTableUpdateDto
            {
                NameKey = "teleport.table",
                IsActive = isActive,
                LocalizedPairs = null,
                Buttons = new List<ButtonUpdateDto>
                {
                    new ButtonUpdateDto
                    {
                        NameKey = "btn.add",
                        IsActive = true,
                        MapSpot = null,
                        LocalizedPairs = null
                    }
                }
            };

            // Act
            var updated = await sut.UpdateTeleportTableAsync(123, 1, dto);

            // Assert
            updated.Should().NotBeNull();
            updated!.Buttons.Should().ContainSingle(b => b.Id == 333 && b.MapSpot == null);
            fakeDb.Commits.Should().Be(1);
            fakeDb.Rollbacks.Should().Be(0);
        }

        /// <summary>
        /// Clearing map spot for an existing button must not perform a map_spot UPDATE.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Put_ExistingButton_ClearMapSpotId(bool isActive)
        {
            // Arrange
            var fakeDb = new FakeDbProvider(() => new DataTable().CreateDataReader())
            {
                // Guard: fail if any UPDATE map_spot happens (should not when MapSpot=null)
                ShouldThrowOnNonQuery = sql => sql.Contains("UPDATE map_spot", StringComparison.OrdinalIgnoreCase)
            };

            fakeDb.EnqueueReader(() => SupportedLangs());
            fakeDb.EnqueueNonQuery(1); // UPDATE teleport_table
            fakeDb.EnqueueNonQuery(1); // UPDATE existing button (map_spot_id -> NULL)

            var dt = TeleportSchema.CreateTeleportSelectSchema();
            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults(isActive: isActive)
                .AddTo(dt);
            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults(isActive: isActive)
                .WithButton(44, "btn.clear", true)
                .AddTo(dt);
            fakeDb.EnqueueReader(() => dt.CreateDataReader());

            var sut = BuildRepo(fakeDb);
            var dto = new TeleportTableUpdateDto
            {
                NameKey = "teleport.table",
                IsActive = isActive,
                LocalizedPairs = null,
                Buttons = new List<ButtonUpdateDto>
                {
                    new ButtonUpdateDto
                    {
                        Id = 44,
                        NameKey = "btn.clear",
                        IsActive = true,
                        MapSpot = null,
                        LocalizedPairs = null
                    }
                }
            };

            // Act
            var updated = await sut.UpdateTeleportTableAsync(123, 1, dto);

            // Assert
            updated.Should().NotBeNull();
            var btn = updated!.Buttons.Single(b => b.Id == 44);
            btn.MapSpot.Should().BeNull();
            fakeDb.Commits.Should().Be(1);
            fakeDb.Rollbacks.Should().Be(0);
        }

        /// <summary>
        /// With empty Buttons list, button processing is skipped entirely.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Put_EmptyButtonsList_SkipsButtonsSection(bool isActive)
        {
            // Arrange
            var fakeDb = new FakeDbProvider(() => new DataTable().CreateDataReader())
            {
                // Guard: fail if any teleport_table_button DML sneaks in
                ShouldThrowOnNonQuery = sql => sql.Contains("teleport_table_button", StringComparison.OrdinalIgnoreCase)
            };

            fakeDb.EnqueueReader(() => SupportedLangs("en-US"));
            fakeDb.EnqueueNonQuery(1); // UPDATE teleport_table

            var dt = TeleportSchema.CreateTeleportSelectSchema();
            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults(isActive: isActive)
                .AddTo(dt);
            fakeDb.EnqueueReader(() => dt.CreateDataReader());

            var sut = BuildRepo(fakeDb);
            var dto = new TeleportTableUpdateDto
            {
                NameKey = "teleport.table",
                IsActive = isActive,
                LocalizedPairs = null,
                Buttons = new List<ButtonUpdateDto>() // empty
            };

            // Act
            var updated = await sut.UpdateTeleportTableAsync(123, 1, dto);

            // Assert
            updated.Should().NotBeNull();
            updated!.Buttons.Should().BeEmpty();
            fakeDb.Commits.Should().Be(1);
            fakeDb.Rollbacks.Should().Be(0);
        }

        /// <summary>
        /// Rolls back when the button i18n INSERT throws.
        /// </summary>
        [Fact]
        public async Task Put_Rollback_When_Button_I18n_Insert_Fails()
        {
            // Arrange
            var fakeDb = new FakeDbProvider(() => new DataTable().CreateDataReader())
            {
                ShouldThrowOnNonQuery = sql => sql.Contains("INSERT INTO i18n", StringComparison.OrdinalIgnoreCase),
                NonQueryException = new InvalidOperationException("error during button i18n insert")
            };

            fakeDb.EnqueueReader(() => SupportedLangs("en-US"));
            fakeDb.EnqueueNonQuery(1); // UPDATE teleport_table
            fakeDb.EnqueueNonQuery(1); // UPDATE existing button
            fakeDb.EnqueueNonQuery(0); // Button i18n UPDATE -> INSERT (throws)

            var sut = BuildRepo(fakeDb);
            var dto = new TeleportTableUpdateDto
            {
                NameKey = "teleport.table",
                IsActive = true,
                LocalizedPairs = null,
                Buttons = new List<ButtonUpdateDto>
                {
                    new ButtonUpdateDto
                    {
                        Id = 10,
                        NameKey = "btn.bad",
                        IsActive = true,
                        MapSpot = null,
                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = "btn.bad",
                            Values = new List<LocalizedValue>
                            {
                                new LocalizedValue { LocaleId = "en-US", Value = "Bad" }
                            }
                        }
                    }
                }
            };

            // Act
            var act = async () => await sut.UpdateTeleportTableAsync(123, 1, dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*error during button i18n insert*");
            fakeDb.Commits.Should().Be(0);
            fakeDb.Rollbacks.Should().Be(1);
        }

        #endregion

        #region DELETE TESTS

        // ---------- helpers ----------

        /// <summary>
        /// Normalizes whitespace in SQL for easier assertions.
        /// </summary>
        private static string WS(string s) => Regex.Replace(s ?? "", @"\s+", " ").Trim();

        /// <summary>
        /// Returns the executed SQL bag (normalized).
        /// </summary>
        private static List<string> SqlBag(FakeDbProvider db) => db.ExecutedSql.Select(WS).ToList();

        /// <summary>
        /// Case-insensitive, whitespace-tolerant search in the SQL bag.
        /// </summary>
        private static bool HasSql(IEnumerable<string> bag, string needle)
            => bag.Any(s => s.IndexOf(WS(needle), StringComparison.OrdinalIgnoreCase) >= 0);

        /// <summary>
        /// Creates a reader yielding button name_key rows (nullable allowed).
        /// </summary>
        private static DbDataReader ButtonKeysReader(params string?[] keys)
        {
            var t = new DataTable();
            t.Columns.Add("name_key", typeof(string));
            foreach (var k in keys)
            {
                var r = t.NewRow();
                r[0] = (object?)k ?? DBNull.Value;
                t.Rows.Add(r);
            }
            return t.CreateDataReader();
        }

        /// <summary>
        /// Creates a reader yielding teleport_table ids.
        /// </summary>
        private static DbDataReader TableIdsReader(params int[] ids)
        {
            var t = new DataTable();
            t.Columns.Add("id", typeof(int));
            foreach (var id in ids)
            {
                var r = t.NewRow();
                r[0] = id;
                t.Rows.Add(r);
            }
            return t.CreateDataReader();
        }

        /// <summary>
        /// Exercises DeleteTeleportTableAsync across permutations:
        /// - presence/absence of table name_key and button keys
        /// - final table delete success/failure.
        /// </summary>
        [Theory]
        [InlineData(true, 2, 1)]
        [InlineData(true, 0, 1)]
        [InlineData(false, 2, 1)]
        [InlineData(false, 0, 1)]
        [InlineData(true, 0, 0)]
        [InlineData(false, 2, 0)]
        [InlineData(false, 0, 0)]
        public async Task DeleteTable_Theory(bool hasTableKey, int buttonKeyCount, int tableDeleteAffected)
        {
            // Arrange
            var fakeDb = new FakeDbProvider(() => new DataTable().CreateDataReader());

            fakeDb.EnqueueScalar(hasTableKey ? "teleport.table" : null); // table name_key

            var keys = Enumerable.Range(1, buttonKeyCount).Select(i => $"btn.{i}").Cast<string?>().ToArray();
            fakeDb.EnqueueReader(() => ButtonKeysReader(keys));          // button keys

            var before = (buttonKeyCount > 0 ? 1 : 0) + (hasTableKey ? 1 : 0) + 1; // btn i18n + table i18n + delete buttons
            for (int i = 0; i < before; i++) fakeDb.EnqueueNonQuery(1);
            fakeDb.EnqueueNonQuery(tableDeleteAffected); // final table delete

            var sut = BuildRepo(fakeDb);

            // Act
            var ok = await sut.DeleteTeleportTableAsync(spaceId: 123, tableId: 77);

            // Assert
            ok.Should().Be(tableDeleteAffected > 0);
            fakeDb.Commits.Should().Be(1);
            fakeDb.Rollbacks.Should().Be(0);

            var sql = SqlBag(fakeDb);

            sql.Should().Contain(s => s.Contains("SELECT name_key") && s.Contains("FROM teleport_table "));
            sql.Should().Contain(s => s.Contains("SELECT name_key") && s.Contains("FROM teleport_table_button"));

            if (buttonKeyCount > 0)
                HasSql(sql, "DELETE FROM i18n WHERE `key` IN (").Should().BeTrue();
            else
                HasSql(sql, "DELETE FROM i18n WHERE `key` IN (").Should().BeFalse();

            if (hasTableKey)
                HasSql(sql, "DELETE FROM i18n WHERE `key` = @NameKey AND space_id = @SpaceId").Should().BeTrue();
            else
                HasSql(sql, "DELETE FROM i18n WHERE `key` = @NameKey AND space_id = @SpaceId").Should().BeFalse();

            HasSql(sql, "DELETE FROM teleport_table_button WHERE table_id = @TableId").Should().BeTrue();
            HasSql(sql, "DELETE FROM teleport_table WHERE id = @TableId AND space_id = @SpaceId").Should().BeTrue();
        }

        /// <summary>
        /// Rolls back delete when the button delete step fails.
        /// </summary>
        [Fact]
        public async Task DeleteTable_Rollback_On_DeleteButtons_Failure()
        {
            // Arrange
            var fakeDb = new FakeDbProvider(() => new DataTable().CreateDataReader())
            {
                ShouldThrowOnNonQuery = sql => WS(sql).Contains("DELETE FROM teleport_table_button WHERE table_id = @TableId",
                                                               StringComparison.OrdinalIgnoreCase),
                NonQueryException = new InvalidOperationException("error deleting buttons")
            };

            fakeDb.EnqueueScalar("teleport.table");
            fakeDb.EnqueueReader(() => ButtonKeysReader("btn.one"));

            var sut = BuildRepo(fakeDb);

            // Act
            var act = async () => await sut.DeleteTeleportTableAsync(123, 7);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*error deleting buttons*");
            fakeDb.Commits.Should().Be(0);
            fakeDb.Rollbacks.Should().Be(1);
        }

        /// <summary>
        /// When a space has no tables, DeleteTeleportTablesBySpaceAsync returns 0 and does not start transactions.
        /// </summary>
        [Fact]
        public async Task DeleteTablesBySpace_NoRows_ReturnsZero()
        {
            // Arrange
            var fakeDb = new FakeDbProvider(() => new DataTable().CreateDataReader());
            fakeDb.EnqueueReader(() => TableIdsReader()); // empty
            var sut = BuildRepo(fakeDb);

            // Act
            var deleted = await sut.DeleteTeleportTablesBySpaceAsync(123);

            // Assert
            deleted.Should().Be(0);
            fakeDb.Commits.Should().Be(0);
            fakeDb.Rollbacks.Should().Be(0);
        }

        /// <summary>
        /// Mixed results across multiple table ids should return the success count and commit each successful delete.
        /// </summary>
        [Fact]
        public async Task DeleteTablesBySpace_MixedSuccess_ReturnsSuccessCount()
        {
            // Arrange
            var fakeDb = new FakeDbProvider(() => new DataTable().CreateDataReader());
            fakeDb.EnqueueReader(() => TableIdsReader(10, 20, 30));

            // id=10 success
            fakeDb.EnqueueScalar("k10");
            fakeDb.EnqueueReader(() => ButtonKeysReader("b10"));
            fakeDb.EnqueueNonQuery(1); fakeDb.EnqueueNonQuery(1); fakeDb.EnqueueNonQuery(1); fakeDb.EnqueueNonQuery(1);

            // id=20 not found on final table delete
            fakeDb.EnqueueScalar("k20");
            fakeDb.EnqueueReader(() => ButtonKeysReader("b20"));
            fakeDb.EnqueueNonQuery(1); fakeDb.EnqueueNonQuery(1); fakeDb.EnqueueNonQuery(1); fakeDb.EnqueueNonQuery(0);

            // id=30 success
            fakeDb.EnqueueScalar("k30");
            fakeDb.EnqueueReader(() => ButtonKeysReader("b30"));
            fakeDb.EnqueueNonQuery(1); fakeDb.EnqueueNonQuery(1); fakeDb.EnqueueNonQuery(1); fakeDb.EnqueueNonQuery(1);

            var sut = BuildRepo(fakeDb);

            // Act
            var deleted = await sut.DeleteTeleportTablesBySpaceAsync(123);

            // Assert
            deleted.Should().Be(2);
            fakeDb.Commits.Should().Be(3);
            fakeDb.Rollbacks.Should().Be(0);
        }

        /// <summary>
        /// Button delete theory covering presence/absence of i18n key and delete affected count.
        /// </summary>
        [Theory]
        [InlineData(true, 1)]
        [InlineData(true, 0)]
        [InlineData(false, 1)]
        [InlineData(false, 0)]
        public async Task DeleteButton_Theory(bool hasI18nKey, int buttonDeleteAffected)
        {
            // Arrange
            var fakeDb = new FakeDbProvider(() => new DataTable().CreateDataReader());

            fakeDb.EnqueueScalar(hasI18nKey ? "btn.key" : null);

            if (hasI18nKey) fakeDb.EnqueueNonQuery(1); // delete i18n (optional)
            fakeDb.EnqueueNonQuery(buttonDeleteAffected); // final delete

            var sut = BuildRepo(fakeDb);

            // Act
            var ok = await sut.DeleteTeleportTableButtonAsync(buttonId: 44, tableId: 7);

            // Assert
            ok.Should().Be(buttonDeleteAffected > 0);
            fakeDb.Commits.Should().Be(1);
            fakeDb.Rollbacks.Should().Be(0);

            var sql = SqlBag(fakeDb);

            sql.Should().Contain(s => s.Contains("SELECT name_key") && s.Contains("FROM teleport_table_button"));

            if (hasI18nKey)
                HasSql(sql, "DELETE FROM i18n WHERE `key` = @BtnKey").Should().BeTrue();
            else
                HasSql(sql, "DELETE FROM i18n WHERE `key` = @BtnKey").Should().BeFalse();

            sql.Any(s =>
                s.Contains("DELETE FROM teleport_table_button", StringComparison.OrdinalIgnoreCase) &&
                s.Contains("WHERE id = @ButtonId", StringComparison.OrdinalIgnoreCase) &&
                s.Contains("AND table_id = @TableId", StringComparison.OrdinalIgnoreCase)
            ).Should().BeTrue();
        }

        /// <summary>
        /// Rolls back when the optional i18n delete throws.
        /// </summary>
        [Fact]
        public async Task DeleteButton_Rollback_On_I18nDelete_Failure()
        {
            // Arrange
            var fakeDb = new FakeDbProvider(() => new DataTable().CreateDataReader())
            {
                ShouldThrowOnNonQuery = sql => WS(sql).Contains("DELETE FROM i18n WHERE `key` = @BtnKey",
                                                               StringComparison.OrdinalIgnoreCase),
                NonQueryException = new InvalidOperationException("error deleting button i18n")
            };

            fakeDb.EnqueueScalar("btn.problem");

            var sut = BuildRepo(fakeDb);

            // Act
            var act = async () => await sut.DeleteTeleportTableButtonAsync(1, 2);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*error deleting button i18n*");
            fakeDb.Commits.Should().Be(0);
            fakeDb.Rollbacks.Should().Be(1);
        }

        #endregion
    }
}