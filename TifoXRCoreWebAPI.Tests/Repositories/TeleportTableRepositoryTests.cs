// © 2025 Global Mobile Software LLC. All rights reserved.
// Author: Urvashi Dhingra
// Date: 08/19/2025
// Summary:
// Single file containing theory-first unit tests for
//  - GetTeleportTableBySpaceAsync (GET)
//  - CreateTeleportTableAsync (POST)
// Uses:
//   - FakeDbProvider (scriptable; supports ExecuteReader/NonQuery/Scalar + transactions)
//   - RepositorySchemas (generic schema helper)
//   - DataRowBuilder (generic row builder; with teleport-specific helpers)
//   - Teleport DTO builders (for concise POST scenarios)

using FluentAssertions;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Tests.TestDoubles.Fakes;
using Microsoft.Extensions.Configuration;
<<<<<<< Updated upstream
using System.Data;
using System.Data.Common;
=======
using TifoXRCoreWebAPI.Tests.TestDoubles.Builders;
using TifoXRCoreWebAPI.Tests.TestDoubles.Fakes;
>>>>>>> Stashed changes
using TifoXRCoreWebAPI.Tests.TestDoubles.Schemas;
using Xunit;

// IMPORTANT: disambiguate against System.Data.DataRowBuilder
using TestDataRowBuilder = TifoXRCoreWebAPI.Tests.TestDoubles.Builders.DataRowBuilder;

namespace GMS.TifoXRCoreWebAPI.Tests.Repositories
{
    public class TeleportTableRepositoryTests
    {
        // ---------------------------- repo factories ----------------------------

        /// <summary>
        /// For GET-style tests that need a reader created from a DataTable.
        /// </summary>
        private static TeleportTableRepository BuildRepo(DataTable dt)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Server=unused;Uid=unused;Pwd=unused;Database=unused;"
                })
                .Build();

            var fakeDb = new FakeDbProvider(() => dt.CreateDataReader());
            return new TeleportTableRepository(config, fakeDb);
        }

        /// <summary>
        /// For POST-style tests that need scripted ExecuteScalar/NonQuery/Txn behavior.
        /// </summary>
        private static TeleportTableRepository BuildRepo(FakeDbProvider fakeDb)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Server=unused;Uid=unused;Pwd=unused;Database=unused;"
                })
                .Build();

            return new TeleportTableRepository(config, fakeDb);
        }

        #region GET TESTS

        // -------- data sources for parameterized tests --------

        public static IEnumerable<object[]> TableLocaleSets()
        {
            yield return new object[] { new[] { "en-US|Teleport" } };
            yield return new object[] { new[] { "en-US|Teleport", "fr-FR|Téléportation" } };
            yield return new object[] { new[] { "en-US|Tele", "de-DE|Teleportieren", "fr-FR|Téléportation" } };
        }

        public static IEnumerable<object[]> ButtonNoI18nCases()
        {
            yield return new object[] { 10, "btn.play", true };
            yield return new object[] { 11, "btn.stop", false };
        }

        public static IEnumerable<object[]> MapSpotCases()
        {
            // buttonId, nameKey, active, mapSpotId, x, y, z
            yield return new object[] { 42, "btn.map", true, 777, 1.25m, -3.5m, 10m };
            yield return new object[] { 99, "btn.loc", false, 15, 0m, 0m, 0m };
            yield return new object[] { 7, "btn.neg", true, 3, -12.345m, 999.999m, -0.001m };
        }

        [Theory]
        [InlineData(123)]
        [InlineData(1)]
        public async Task Get_ReturnsNull_WhenNoRows(int spaceId)
        {
            var dt = RepositorySchemas.CreateTeleportSelectSchema();
            var sut = BuildRepo(dt);

            var result = await sut.GetTeleportTableBySpaceAsync(spaceId);

            result.Should().BeNull("no rows were returned by the data reader");
        }

        [Theory]
        [MemberData(nameof(TableLocaleSets))]
        public async Task Get_ReturnsTable_WithTableI18n_NoButtons(string[] tableLocales)
        {
            var dt = RepositorySchemas.CreateTeleportSelectSchema();

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
            var result = await sut.GetTeleportTableBySpaceAsync(123);

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

        [Theory]
        [MemberData(nameof(ButtonNoI18nCases))]
        public async Task Get_ReturnsTable_WithSingleButton_NoI18n_NoMapSpot(int buttonId, string nameKey, bool isActive)
        {
            var dt = RepositorySchemas.CreateTeleportSelectSchema();

            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults()
                .WithButton(buttonId, nameKey, isActive)
                .AddTo(dt);

            var sut = BuildRepo(dt);
            var result = await sut.GetTeleportTableBySpaceAsync(123);

            result.Should().NotBeNull();
            result!.Buttons.Should().HaveCount(1);

            var b = result.Buttons[0];
            b.Id.Should().Be(buttonId);
            b.NameKey.Should().Be(nameKey);
            b.IsActive.Should().Be(isActive);
            b.LocalizedPairs.Values.Should().BeEmpty();
            b.MapSpot.Should().BeNull();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Get_Aggregates_MultipleButtons_And_MultiLocaleButtonI18n_DedupesLocales(bool includeDuplicateFr)
        {
            var dt = RepositorySchemas.CreateTeleportSelectSchema();

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
            var result = await sut.GetTeleportTableBySpaceAsync(123);

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

        [Theory]
        [MemberData(nameof(MapSpotCases))]
        public async Task Get_Button_WithMapSpot_PopulatesCoordinates(
            int buttonId, string nameKey, bool isActive, int mapSpotId, decimal x, decimal y, decimal z)
        {
            var dt = RepositorySchemas.CreateTeleportSelectSchema();

            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults()
                .WithButton(buttonId, nameKey, isActive)
                .WithMapSpot(mapSpotId, x, y, z)
                .AddTo(dt);

            var sut = BuildRepo(dt);
            var result = await sut.GetTeleportTableBySpaceAsync(123);

            result.Should().NotBeNull();
            var btn = result!.Buttons.Should().ContainSingle().Subject;
            btn.MapSpot.Should().NotBeNull();
            btn.MapSpot!.Id.Should().Be(mapSpotId);
            btn.MapSpot.X.Should().Be(x);
            btn.MapSpot.Y.Should().Be(y);
            btn.MapSpot.Z.Should().Be(z);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task Get_Ignores_Null_TableAndButton_I18n_Pairs(bool nullTableI18n, bool nullButtonI18n)
        {
            var dt = RepositorySchemas.CreateTeleportSelectSchema();

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
            var result = await sut.GetTeleportTableBySpaceAsync(123);

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

        [Theory]
        [InlineData("en-US", "Only Table")]
        [InlineData("fr-FR", "Seulement Table")]
        public async Task Get_NoButtons_Yields_EmptyButtonsList_NotNull(string tableLocale, string tableValue)
        {
            var dt = RepositorySchemas.CreateTeleportSelectSchema();

            TestDataRowBuilder.TeleportSelectRow()
                .WithTeleportTableDefaults()
                .WithTableLocale(tableLocale, tableValue)
                .AddTo(dt);

            var sut = BuildRepo(dt);
            var result = await sut.GetTeleportTableBySpaceAsync(123);

            result.Should().NotBeNull();
            result!.Buttons.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region POST TESTS

        // ---------- helpers (for POST) ----------

        private static int CountInsertFor(FakeDbProvider db, string table) =>
            db.ExecutedSql.Count(s =>
                s.Contains($"INSERT INTO {table} ", StringComparison.OrdinalIgnoreCase) ||
                s.Contains($"INSERT INTO {table}(", StringComparison.OrdinalIgnoreCase));

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

        [Fact]
        public async Task Create_Throws_ArgumentNull_WhenDtoIsNull()
        {
            var fakeDb = new FakeDbProvider(() => new DataTable().CreateDataReader());
            var sut = BuildRepo(fakeDb);

            Func<Task> act = async () => await sut.CreateTeleportTableAsync(123, null!);
            await act.Should().ThrowAsync<ArgumentNullException>();

            fakeDb.Commits.Should().Be(0);
            fakeDb.Rollbacks.Should().Be(0);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Create_Table_NoI18n_NoButtons_Commits(bool isActive)
        {
            // Scalar seq: [tableId]
            var fakeDb = new FakeDbProvider(
                () => new DataTable().CreateDataReader(),
                scalarResults: new Queue<object>(new object[] { 100 })
            );

            var dto = TeleportTableCreateDtoBuilder.Default()
                .WithNameKey("teleport.table")
                .WithIsActive(isActive)
                .Build();

            var sut = BuildRepo(fakeDb);
            var created = await sut.CreateTeleportTableAsync(123, dto);

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

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        public async Task Create_Table_With_I18n_Inserts_All_Locales(int localeCount)
        {
            // Scalar seq: [tableId]
            var fakeDb = new FakeDbProvider(
                () => new DataTable().CreateDataReader(),
                scalarResults: new Queue<object>(new object[] { 101 })
            );

            var locales = Enumerable.Range(1, localeCount)
                .Select(i => ($"l{i}", $"Name {i}"))
                .ToArray();

            var dto = TeleportTableCreateDtoBuilder.Default()
                .WithNameKey("teleport.name")
                .WithIsActive(true)
                .WithTableLocales(locales)
                .Build();

            var sut = BuildRepo(fakeDb);
            var created = await sut.CreateTeleportTableAsync(123, dto);

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

        [Fact]
        public async Task Create_Table_And_Button_Without_MapSpot_No_I18n()
        {
            // Scalar seq: [tableId, buttonId]
            var fakeDb = new FakeDbProvider(
                () => new DataTable().CreateDataReader(),
                scalarResults: new Queue<object>(new object[] { 200, 300 })
            );

            var dto = TeleportTableCreateDtoBuilder.Default()
                .WithNameKey("teleport.table")
                .WithIsActive(true)
                .AddButton(ButtonCreateDtoBuilder.Default()
                    .WithNameKey("btn.play")
                    .WithIsActive(true)
                    .Build())
                .Build();

            var sut = BuildRepo(fakeDb);
            var created = await sut.CreateTeleportTableAsync(123, dto);

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

        [Fact]
        public async Task Create_Table_And_Button_With_New_MapSpot_And_I18n()
        {
            // Scalar seq: [tableId, mapSpotId, buttonId]
            var fakeDb = new FakeDbProvider(
                () => new DataTable().CreateDataReader(),
                scalarResults: new Queue<object>(new object[] { 500, 600, 601 })
            );

            var dto = TeleportTableCreateDtoBuilder.Default()
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
            var created = await sut.CreateTeleportTableAsync(123, dto);

            // ----- asserts on object graph -----
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

            // ----- asserts on SQL execution -----
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

        [Fact]
        public async Task Create_Uses_Existing_MapSpotId_No_MapSpot_Insert()
        {
            // Scalar seq: [tableId, buttonId] (no map spot id)
            var fakeDb = new FakeDbProvider(
                () => new DataTable().CreateDataReader(),
                scalarResults: new Queue<object>(new object[] { 800, 900 })
            );

            var dto = TeleportTableCreateDtoBuilder.Default()
                .WithNameKey("teleport.table")
                .WithIsActive(true)
                .AddButton(ButtonCreateDtoBuilder.Default()
                    .WithNameKey("btn.existing")
                    .WithIsActive(true)
                    .WithExistingMapSpotId(777)
                    .Build())
                .Build();

            var sut = BuildRepo(fakeDb);
            var created = await sut.CreateTeleportTableAsync(123, dto);

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

        [Fact]
        public async Task Create_RollsBack_When_Button_Insert_Fails()
        {
            // Scalar seq: [tableId] — failure occurs when inserting the button
            var fakeDb = new FakeDbProvider(
                () => new DataTable().CreateDataReader(),
                scalarResults: new Queue<object>(new object[] { 1234 })
            )
            {
                ShouldThrowOnNonQuery = sql => sql.Contains("INSERT INTO teleport_table_button", StringComparison.OrdinalIgnoreCase),
                NonQueryException = new InvalidOperationException("boom during button insert")
            };

            var dto = TeleportTableCreateDtoBuilder.Default()
                .WithNameKey("teleport.table")
                .WithIsActive(true)
                .AddButton(ButtonCreateDtoBuilder.Default()
                    .WithNameKey("btn.bad")
                    .WithIsActive(true)
                    .Build())
                .Build();

            var sut = BuildRepo(fakeDb);
            var act = async () => await sut.CreateTeleportTableAsync(123, dto);

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*boom during button insert*");

            fakeDb.Commits.Should().Be(0);
            fakeDb.Rollbacks.Should().Be(1);
        }

        // ---------- additional POST coverage ----------

        public static IEnumerable<object[]> CreateVariants()
        {
            // tableI18n, buttonCount, buttonI18nEach, mapSpotMode
            // mapSpotMode: 0=none, 1=new, 2=existing
            yield return new object[] { 0, 0, 0, 0 };
            yield return new object[] { 2, 0, 0, 0 };
            yield return new object[] { 0, 1, 0, 0 };
            yield return new object[] { 1, 1, 1, 1 };
            yield return new object[] { 0, 1, 0, 2 };
            yield return new object[] { 2, 2, 1, 1 }; // richer: 2 buttons, each with 1 i18n and new map spot
        }

        // -- targeted failure-path tests: each should rollback --

        [Fact]
        public async Task Create_Rollback_When_Table_Insert_Fails()
        {
            var fakeDb = new FakeDbProvider(() => new DataTable().CreateDataReader())
            {
                ShouldThrowOnNonQuery = sql => sql.Contains("INSERT INTO teleport_table", StringComparison.OrdinalIgnoreCase),
                NonQueryException = new InvalidOperationException("boom at table")
            };

            var dto = TeleportTableCreateDtoBuilder.Default()
                .WithNameKey("teleport.table")
                .WithIsActive(true)
                .Build();

            var sut = BuildRepo(fakeDb);
            var act = async () => await sut.CreateTeleportTableAsync(123, dto);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*boom at table*");
            fakeDb.Commits.Should().Be(0);
            fakeDb.Rollbacks.Should().Be(1);
        }

        [Fact]
        public async Task Create_Rollback_When_MapSpot_Insert_Fails()
        {
            // Scalars: [tableId] (we will fail before consuming later scalars)
            var fakeDb = new FakeDbProvider(
                () => new DataTable().CreateDataReader(),
                scalarResults: new Queue<object>(new object[] { 2001 })
            )
            {
                ShouldThrowOnNonQuery = sql => sql.Contains("INSERT INTO map_spot", StringComparison.OrdinalIgnoreCase),
                NonQueryException = new InvalidOperationException("boom at map_spot")
            };

            var dto = TeleportTableCreateDtoBuilder.Default()
                .WithNameKey("teleport.table")
                .WithIsActive(true)
                .AddButton(ButtonCreateDtoBuilder.Default()
                    .WithNameKey("btn.map")
                    .WithIsActive(true)
                    .WithNewMapSpot(1, 2, 3)
                    .Build())
                .Build();

            var sut = BuildRepo(fakeDb);
            var act = async () => await sut.CreateTeleportTableAsync(123, dto);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*boom at map_spot*");
            fakeDb.Commits.Should().Be(0);
            fakeDb.Rollbacks.Should().Be(1);
        }

        [Fact]
        public async Task Create_Rollback_When_Table_I18n_Insert_Fails()
        {
            // Scalars: [tableId]
            var fakeDb = new FakeDbProvider(
                () => new DataTable().CreateDataReader(),
                scalarResults: new Queue<object>(new object[] { 2101 })
            )
            {
                ShouldThrowOnNonQuery = sql => sql.Contains("INSERT INTO i18n", StringComparison.OrdinalIgnoreCase),
                NonQueryException = new InvalidOperationException("boom at table i18n")
            };

            // DTO with table i18n and no buttons to ensure the first i18n is table-level
            var dto = TeleportTableCreateDtoBuilder.Default()
                .WithNameKey("teleport.table")
                .WithIsActive(true)
                .WithTableLocales(("en-US", "T"))
                .Build();

            var sut = BuildRepo(fakeDb);
            var act = async () => await sut.CreateTeleportTableAsync(123, dto);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*table i18n*");
            fakeDb.Commits.Should().Be(0);
            fakeDb.Rollbacks.Should().Be(1);
        }

        [Fact]
        public async Task Create_Rollback_When_Button_I18n_Insert_Fails()
        {
            // Scalars: [tableId, buttonId]
            var fakeDb = new FakeDbProvider(
                () => new DataTable().CreateDataReader(),
                scalarResults: new Queue<object>(new object[] { 2201, 2202 })
            )
            {
                ShouldThrowOnNonQuery = sql => sql.Contains("INSERT INTO i18n", StringComparison.OrdinalIgnoreCase),
                NonQueryException = new InvalidOperationException("boom at button i18n")
            };

            // DTO with button i18n and no table i18n to ensure first i18n is button-level
            var dto = TeleportTableCreateDtoBuilder.Default()
                .WithNameKey("teleport.table")
                .WithIsActive(true)
                .AddButton(ButtonCreateDtoBuilder.Default()
                    .WithNameKey("btn.i18n")
                    .WithIsActive(true)
                    .WithButtonLocales(("en-US", "Go"))
                    .Build())
                .Build();

            var sut = BuildRepo(fakeDb);
            var act = async () => await sut.CreateTeleportTableAsync(123, dto);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*button i18n*");
            fakeDb.Commits.Should().Be(0);
            fakeDb.Rollbacks.Should().Be(1);
        }
        #endregion
    }
}
