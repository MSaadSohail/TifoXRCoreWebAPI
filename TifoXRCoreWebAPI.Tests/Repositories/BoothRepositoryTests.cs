// <copyright file="BoothRepositoryTests.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>08/18/2025</date>
// <summary>Unit tests for Booth Repository</summary>

using FluentAssertions;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Tests.TestDoubles.Fakes;
using GMS.TifoXRCoreWebAPI.Tests.TestDoubles.Schemas;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.Common;
using TifoXRCoreWebAPI.Utilities.Infrastructure.Interface;
using Xunit;

namespace GMS.TifoXRCoreWebAPI.Tests.Repositories
{
    public class BoothRepositoryTests
    {

        private static BoothUpdateDto Dto(int spotId, string key, params (string loc, string val)[] pairs)
            => new BoothUpdateDto
            {
                MapSpotId = spotId,
                LocalizedPairs = new LocalizedPairs
                {
                    Key = key,
                    Values = pairs.Select(p => new LocalizedValue { LocaleId = p.loc, Value = p.val }).ToList()
                }
            };

        private static DbDataReader Langs(params string[] locales)
        {
            var t = new DataTable();
            t.Columns.Add("locale_id", typeof(string));
            foreach (var loc in locales) t.Rows.Add(loc);
            return t.CreateDataReader();
        }

        /// <summary>Build reader for LoadBoothByIdAsync: id, space_id, name_key, map_spot_id, x, y, z, locale_id, value.</summary>
        private static DbDataReader ReloadRows(
            int id, int spaceId, string key, int? mapSpotId,
            decimal? x, decimal? y, decimal? z,
            params (string loc, string val)[] i18n)
        {
            var t = new DataTable();
            t.Columns.Add("id", typeof(int));
            t.Columns.Add("space_id", typeof(int));
            t.Columns.Add("name_key", typeof(string));
            t.Columns.Add("map_spot_id", typeof(int));
            t.Columns.Add("x", typeof(decimal));
            t.Columns.Add("y", typeof(decimal));
            t.Columns.Add("z", typeof(decimal));
            t.Columns.Add("locale_id", typeof(string));
            t.Columns.Add("value", typeof(string));

            foreach (var (loc, val) in i18n)
            {
                t.Rows.Add(
                    id, spaceId, key,
                    mapSpotId.HasValue ? mapSpotId.Value : (object)DBNull.Value,
                    x.HasValue ? x.Value : (object)DBNull.Value,
                    y.HasValue ? y.Value : (object)DBNull.Value,
                    z.HasValue ? z.Value : (object)DBNull.Value,
                    loc, val
                );
            }
            return t.CreateDataReader();
        }

        private static IConfiguration MakeConfig() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=unused;Uid=unused;Pwd=unused;Database=unused;"
                })
                .Build();

        private static BoothRepository MakeSut(DataTable dt)
        {
            IDbProvider fakeDb = new FakeDbProvider(() => dt.CreateDataReader());
            return new BoothRepository(MakeConfig(), fakeDb);
        }
        private static BoothCreateDto CreateDto(int spotId, string key, params (string loc, string val)[] pairs)
        => new BoothCreateDto
        {
            MapSpotId = spotId,
            LocalizedPairs = new LocalizedPairs
            {
                Key = key,
                Values = pairs.Select(p => new LocalizedValue { LocaleId = p.loc, Value = p.val }).ToList()
            }
        };

        private static DbDataReader MapSpotRow(decimal? x, decimal? y, decimal? z)
        {
            var t = new DataTable();
            t.Columns.Add("x", typeof(decimal));
            t.Columns.Add("y", typeof(decimal));
            t.Columns.Add("z", typeof(decimal));
            t.Rows.Add(
                x.HasValue ? x.Value : (object)DBNull.Value,
                y.HasValue ? y.Value : (object)DBNull.Value,
                z.HasValue ? z.Value : (object)DBNull.Value
            );
            return t.CreateDataReader();
        }

        private static DbDataReader MapSpotNone()
        {
            var t = new DataTable();
            t.Columns.Add("x", typeof(decimal));
            t.Columns.Add("y", typeof(decimal));
            t.Columns.Add("z", typeof(decimal));
            // no rows
            return t.CreateDataReader();
        }

        private static DbDataReader PortalRows(params (int id, string key, string? corr, string? thumb)[] rows)
        {
            var t = new DataTable();
            t.Columns.Add("id", typeof(int));
            t.Columns.Add("text_field_key", typeof(string));
            t.Columns.Add("corresponding_media_id", typeof(string));
            t.Columns.Add("thumbnail_media_id", typeof(string));
            foreach (var (id, key, corr, thumb) in rows)
            {
                t.Rows.Add(
                    id,
                    key,
                    corr is null ? (object)DBNull.Value : corr,
                    thumb is null ? (object)DBNull.Value : thumb
                );
            }
            return t.CreateDataReader();
        }



        // ---------- tests ----------


        // <summary>
        // Ensures GetAllBoothsBySpaceAsync returns an empty list (not null)
        // when no booth records exist for the given spaceId.
        // </summary>
        [Fact]
        public async Task ReturnsEmptyList_WhenNoRows()
        {
            var dt = BoothGetSchema.CreateEmptySchema();
            var sut = MakeSut(dt);

            var result = await sut.GetAllBoothsBySpaceAsync(123);

            result.Should().NotBeNull().And.BeEmpty();
        }

        // <summary>
        // Ensures GetAllBoothsBySpaceAsync returns a single booth with aggregated,
        // de-duplicated locales and no map spot when map_spot_id is null.
        // </summary>
        [Fact]
        public async Task AggregatesLocales_DeDupes()
        {
            var dt = BoothGetSchema.CreateEmptySchema();

            // en_us
            dt.Rows.Add(
                10,        // id
                123,       // space_id
                "bth_key", // name_key
                DBNull.Value, // map_spot_id
                DBNull.Value, DBNull.Value, DBNull.Value, // x,y,z
                "en_us",   // locale_id
                "Booth EN" // value
            );
            // es_es
            dt.Rows.Add(10, 123, "bth_key", DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, "es_es", "Booth ES");
            // duplicate en_us (should be ignored)
            dt.Rows.Add(10, 123, "bth_key", DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, "en_us", "Booth EN");

            var sut = MakeSut(dt);

            var result = await sut.GetAllBoothsBySpaceAsync(123);

            result.Should().HaveCount(1);
            var booth = result[0];
            booth.Id.Should().Be(10);
            booth.SpaceId.Should().Be(123);
            booth.MapSpotId.Should().Be(0); // default(int) because column is null
            booth.MapSpot.Should().BeNull(); // no x/y/z
            booth.LocalizedPairs.Key.Should().Be("bth_key");
            booth.LocalizedPairs.Values.Should().HaveCount(2);
            booth.LocalizedPairs.Values.Should().Contain(v => v.LocaleId == "en_us" && v.Value == "Booth EN");
            booth.LocalizedPairs.Values.Should().Contain(v => v.LocaleId == "es_es" && v.Value == "Booth ES");
        }


        // <summary>
        // Ensures GetAllBoothsBySpaceAsync correctly populates map spot ID and coordinates (x,y,z).
        // </summary>
        [Fact]
        public async Task PopulatesCoords()
        {
            var dt = BoothGetSchema.CreateEmptySchema();

            dt.Rows.Add(
                11, 123, "bth_key",
                5,        // map_spot_id
                1.1m, 2.2m, 3.3m, // x,y,z
                "en_us", "Booth"
            );

            var sut = MakeSut(dt);

            var result = await sut.GetAllBoothsBySpaceAsync(123);

            result.Should().HaveCount(1);
            var booth = result[0];
            booth.MapSpotId.Should().Be(5);
            booth.MapSpot.Should().NotBeNull();
            booth.MapSpot!.X.Should().Be(1.1m);
            booth.MapSpot.Y.Should().Be(2.2m);
            booth.MapSpot.Z.Should().Be(3.3m);
        }


        // <summary>
        // Ensures GetAllBoothsBySpaceAsync aggregates multiple booths correctly,
        // each with its own map spot and localized values.
        // </summary>
        [Fact]
        public async Task MultipleBoothsAggregatePerBooth()
        {
            var dt = BoothGetSchema.CreateEmptySchema();

            // Booth 10 (no map spot)
            dt.Rows.Add(10, 123, "bth_a", DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, "en_us", "A EN");
            // Booth 20 (with map spot)
            dt.Rows.Add(20, 123, "bth_b", 7, 9m, 8m, 7m, "en_us", "B EN");
            // Another locale for booth 20
            dt.Rows.Add(20, 123, "bth_b", 7, 9m, 8m, 7m, "es_es", "B ES");

            var sut = MakeSut(dt);

            var result = await sut.GetAllBoothsBySpaceAsync(123);

            result.Should().HaveCount(2);
            var b10 = result.First(b => b.Id == 10);
            var b20 = result.First(b => b.Id == 20);

            b10.MapSpot.Should().BeNull();
            b10.LocalizedPairs.Values.Should().ContainSingle(v => v.LocaleId == "en_us" && v.Value == "A EN");

            b20.MapSpot.Should().NotBeNull();
            b20.LocalizedPairs.Values.Should().HaveCount(2);
            b20.LocalizedPairs.Values.Select(v => v.LocaleId).Should().BeEquivalentTo(new[] { "en_us", "es_es" });
        }


        // <summary>
        // Ensures GetAllBoothsBySpaceAsync sets MapSpotId but leaves MapSpot null
        // when map_spot_id exists but no coordinate values (x,y,z) are returned.
        // </summary>
        [Fact]
        public async Task LeavesMapSpotNull()
        {
            var dt = BoothGetSchema.CreateEmptySchema();

            // map_spot_id set but left join produced no x/y/z (NULL) -> MapSpot must be null
            dt.Rows.Add(
                12, 123, "bth_key",
                42,       // map_spot_id present
                DBNull.Value, DBNull.Value, DBNull.Value, // x,y,z null
                "en_us", "Booth"
            );

            var sut = MakeSut(dt);

            var result = await sut.GetAllBoothsBySpaceAsync(123);

            result.Should().HaveCount(1);
            var booth = result[0];
            booth.MapSpotId.Should().Be(42);
            booth.MapSpot.Should().BeNull(); // because x is null => hasCoords == false
        }



        // <summary>
        // Ensures GetAllBoothsBySpaceAsync removes duplicate locales for a booth
        // while retaining unique translations per locale.
        // </summary>
        [Fact]
        public async Task DeDupesLocalesPerBooth()
        {
            var dt = BoothGetSchema.CreateEmptySchema();

            dt.Rows.Add(15, 123, "bth_key", DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, "en_us", "Text");
            dt.Rows.Add(15, 123, "bth_key", DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, "en_us", "Text"); // duplicate
            dt.Rows.Add(15, 123, "bth_key", DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, "fr_fr", "Texte");

            var sut = MakeSut(dt);

            var result = await sut.GetAllBoothsBySpaceAsync(123);

            result.Should().HaveCount(1);
            var booth = result[0];
            booth.LocalizedPairs.Values.Should().HaveCount(2);
            booth.LocalizedPairs.Values.Should().Contain(v => v.LocaleId == "en_us" && v.Value == "Text");
            booth.LocalizedPairs.Values.Should().Contain(v => v.LocaleId == "fr_fr" && v.Value == "Texte");
        }

        /// <summary>COUNT(map_spot)=0 -> throws.</summary>
        [Fact]
        public async Task MapSpotMissingThrows()
        {
            var fake = new FakeDbProvider(() => throw new InvalidOperationException("no reader expected"));
            fake.EnqueueScalar(0); // validate spot

            var sut = new BoothRepository(MakeConfig(), fake);
            var act = () => sut.UpdateBoothAsync(1, 99, Dto(999, "bth_key", ("en_us", "X")));

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("MapSpot*does not exist*");
        }

        /// <summary>UPDATE booth affects 0 rows -> null.</summary>
        [Fact]
        public async Task UpdateNoRowsNull()
        {
            var fake = new FakeDbProvider(() => throw new InvalidOperationException("no reader expected"));
            fake.EnqueueScalar(1); // spot ok
            fake.EnqueueNonQuery(0); // update 0 rows

            var sut = new BoothRepository(MakeConfig(), fake);
            var res = await sut.UpdateBoothAsync(1, 10, Dto(5, "bth_key", ("en_us", "Booth")));

            res.Should().BeNull();
        }

        /// <summary>Supported langs omit 'fr_fr' -> throws.</summary>
        [Fact]
        public async Task UnsupportedLocalesThrows()
        {
            var fake = new FakeDbProvider(() => throw new InvalidOperationException("unexpected fallback"));
            fake.EnqueueScalar(1);          // spot ok
            fake.EnqueueNonQuery(1);        // update ok
            fake.EnqueueReader(() => Langs("en_us")); // supported langs

            var sut = new BoothRepository(MakeConfig(), fake);
            var act = () => sut.UpdateBoothAsync(1, 10, Dto(5, "bth_key", ("en_us", "Hello"), ("fr_fr", "Bonjour")));

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*Unsupported locales:*fr_fr*");
        }

        /// <summary>Upsert i18n (en_us insert, es_es update), then reload merged booth.</summary>
        [Fact]
        public async Task UpdateOkUpsertAndReload()
        {
            var fake = new FakeDbProvider(() => throw new InvalidOperationException("unexpected fallback"));
            fake.EnqueueScalar(1);                 // spot ok
            fake.EnqueueNonQuery(1);               // update booth -> 1
            fake.EnqueueReader(() => Langs("en_us", "es_es")); // both supported
            fake.EnqueueNonQuery(0);               // upd en_us -> 0
            fake.EnqueueNonQuery(1);               // ins en_us -> 1
            fake.EnqueueNonQuery(1);               // upd es_es -> 1
            fake.EnqueueReader(() => ReloadRows(   // reload
                id: 10, spaceId: 1, key: "bth_key", mapSpotId: 5,
                x: 1.1m, y: 2.2m, z: 3.3m,
                ("en_us", "Hello"), ("es_es", "Hola")
            ));

            var sut = new BoothRepository(MakeConfig(), fake);
            var dto = Dto(5, "bth_key", ("en_us", "Hello"), ("es_es", "Hola"));

            var res = await sut.UpdateBoothAsync(1, 10, dto);

            res.Should().NotBeNull();
            res!.Id.Should().Be(10);
            res.SpaceId.Should().Be(1);
            res.MapSpotId.Should().Be(5);
            res.MapSpot!.X.Should().Be(1.1m);
            res.MapSpot!.Y.Should().Be(2.2m);
            res.MapSpot!.Z.Should().Be(3.3m);
            res.LocalizedPairs.Key.Should().Be("bth_key");
            res.LocalizedPairs.Values.Select(v => v.LocaleId)
               .Should().BeEquivalentTo(new[] { "en_us", "es_es" });
        }

        /// <summary>No locales in DTO: skip lang check/upserts; reload only.</summary>
        [Fact]
        public async Task UpdateOkNoLocalesReloadOnly()
        {
            var fake = new FakeDbProvider(() => throw new InvalidOperationException("unexpected fallback"));
            var dto = new BoothUpdateDto { MapSpotId = 7, LocalizedPairs = new LocalizedPairs { Key = "bth_key", Values = null } };

            fake.EnqueueScalar(1); // spot ok
            fake.EnqueueNonQuery(1); // update booth
            fake.EnqueueReader(() => ReloadRows(
                id: 22, spaceId: 3, key: "bth_key", mapSpotId: 7,
                x: 9.9m, y: 8.8m, z: 7.7m,
                ("en_us", "Existing From DB")
            ));

            var sut = new BoothRepository(MakeConfig(), fake);
            var res = await sut.UpdateBoothAsync(3, 22, dto);

            res.Should().NotBeNull();
            res!.Id.Should().Be(22);
            res.SpaceId.Should().Be(3);
            res.MapSpotId.Should().Be(7);
            res.MapSpot!.X.Should().Be(9.9m);
            res.LocalizedPairs.Values.Should().ContainSingle(v => v.LocaleId == "en_us" && v.Value == "Existing From DB");
        }


        /// <summary>SELECT x,y,z returns no rows -> throws.</summary>
        [Fact]
        public async Task MapSpotMissingThrowsCreate()
        {
            var fake = new FakeDbProvider(() => throw new InvalidOperationException("no fallback reader expected"));
            // 1) validate map_spot: empty reader
            fake.EnqueueReader(MapSpotNone);

            var sut = new BoothRepository(MakeConfig(), fake);

            var act = () => sut.CreateBoothAsync(1, CreateDto(999, "bth_key", ("en_us", "Booth")));

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("MapSpot*does not exist*");
        }

        /// <summary>Happy path: insert booth, insert supported i18n, return full model.</summary>
        [Fact]
        public async Task CreateOkInsertsAllSupported()
        {
            var fake = new FakeDbProvider(() => throw new InvalidOperationException("no fallback reader expected"));

            // 1) map_spot row (coords)
            fake.EnqueueReader(() => MapSpotRow(1.1m, 2.2m, 3.3m));
            // 2) INSERT booth
            fake.EnqueueNonQuery(1);
            // 3) LAST_INSERT_ID
            fake.EnqueueScalar(42);
            // 4) supported_languages -> en_us, es_es
            fake.EnqueueReader(() => Langs("en_us", "es_es"));
            // 5) INSERT i18n en_us
            fake.EnqueueNonQuery(1);
            // 6) INSERT i18n es_es
            fake.EnqueueNonQuery(1);

            var sut = new BoothRepository(MakeConfig(), fake);
            var dto = CreateDto(5, "bth_key", ("en_us", "Hello"), ("es_es", "Hola"));

            var res = await sut.CreateBoothAsync(1, dto);

            res.Should().NotBeNull();
            res.Id.Should().Be(42);
            res.SpaceId.Should().Be(1);
            res.MapSpotId.Should().Be(5);
            res.MapSpot!.X.Should().Be(1.1m);
            res.MapSpot.Y.Should().Be(2.2m);
            res.MapSpot.Z.Should().Be(3.3m);
            res.LocalizedPairs.Key.Should().Be("bth_key");
            res.LocalizedPairs.Values.Select(v => v.LocaleId)
                .Should().BeEquivalentTo(new[] { "en_us", "es_es" });
        }

        /// <summary>Only supported locales are inserted; unsupported are ignored.</summary>
        [Fact]
        public async Task CreateOkFiltersUnsupported()
        {
            var fake = new FakeDbProvider(() => throw new InvalidOperationException("no fallback reader expected"));

            // 1) map_spot
            fake.EnqueueReader(() => MapSpotRow(0m, 0m, 0m));
            // 2) INSERT booth
            fake.EnqueueNonQuery(1);
            // 3) LAST_INSERT_ID
            fake.EnqueueScalar(100);
            // 4) supported_languages -> only en_us
            fake.EnqueueReader(() => Langs("en_us"));
            // 5) INSERT i18n en_us
            fake.EnqueueNonQuery(1);

            var sut = new BoothRepository(MakeConfig(), fake);
            var dto = CreateDto(7, "bth_key", ("en_us", "Hello"), ("fr_fr", "Bonjour"));

            var res = await sut.CreateBoothAsync(3, dto);

            res.Id.Should().Be(100);
            res.SpaceId.Should().Be(3);
            res.LocalizedPairs.Values.Should().ContainSingle(v => v.LocaleId == "en_us" && v.Value == "Hello");
        }

        /// <summary>No locales in DTO -> skip lang check and i18n inserts; return model.</summary>
        [Fact]
        public async Task CreateOkNoLocalesSkipsI18n()
        {
            var fake = new FakeDbProvider(() => throw new InvalidOperationException("no fallback reader expected"));

            var dto = new BoothCreateDto
            {
                MapSpotId = 8,
                LocalizedPairs = new LocalizedPairs { Key = "bth_key", Values = null }
            };

            // 1) map_spot
            fake.EnqueueReader(() => MapSpotRow(9.9m, 8.8m, 7.7m));
            // 2) INSERT booth
            fake.EnqueueNonQuery(1);
            // 3) LAST_INSERT_ID
            fake.EnqueueScalar(11);
            // (no supported_languages reader, no i18n inserts)

            var sut = new BoothRepository(MakeConfig(), fake);
            var res = await sut.CreateBoothAsync(2, dto);

            res.Id.Should().Be(11);
            res.SpaceId.Should().Be(2);
            res.MapSpotId.Should().Be(8);
            res.MapSpot!.X.Should().Be(9.9m);
            res.LocalizedPairs.Key.Should().Be("bth_key");
            res.LocalizedPairs.Values.Should().BeEmpty();
        }

        /// <summary>Blank/null locale IDs are ignored; only real locales are validated/inserted.</summary>
        [Fact]
        public async Task BlankAndNullLocalesIgnored()
        {
            var fake = new FakeDbProvider(() => throw new InvalidOperationException("no fallback reader expected"));

            // 1) map_spot
            fake.EnqueueReader(() => MapSpotRow(4.4m, 5.5m, 6.6m));
            // 2) INSERT booth
            fake.EnqueueNonQuery(1);
            // 3) LAST_INSERT_ID
            fake.EnqueueScalar(88);
            // 4) supported_languages -> only en_us (whitespace/null locales are filtered before this)
            fake.EnqueueReader(() => Langs("en_us"));
            // 5) INSERT i18n en_us
            fake.EnqueueNonQuery(1);

            var sut = new BoothRepository(MakeConfig(), fake);
            var dto = CreateDto(12, "bth_key",
                ("", "X"), ("  ", "Y"), (null!, "Z"), // ignored
                ("en_us", "Hello")                    // inserted
            );

            var res = await sut.CreateBoothAsync(9, dto);

            res.Id.Should().Be(88);
            res.SpaceId.Should().Be(9);
            res.LocalizedPairs.Values.Should().ContainSingle(v => v.LocaleId == "en_us" && v.Value == "Hello");
        }

        /// <summary>map_spot coords NULL -> MapSpot defaults to 0s.</summary>
        [Fact]
        public async Task MapSpotNullDefToZero()
        {
            var fake = new FakeDbProvider(() => throw new InvalidOperationException("no fallback reader expected"));

            // 1) map_spot returns one row with NULL x,y,z
            fake.EnqueueReader(() => MapSpotRow(null, null, null));
            // 2) INSERT booth
            fake.EnqueueNonQuery(1);
            // 3) LAST_INSERT_ID
            fake.EnqueueScalar(77);
            // 4) supported_languages -> en_us (so we insert one i18n)
            fake.EnqueueReader(() => Langs("en_us"));
            // 5) INSERT i18n en_us
            fake.EnqueueNonQuery(1);

            var sut = new BoothRepository(MakeConfig(), fake);
            var dto = CreateDto(4, "bth_key", ("en_us", "Hello"));

            var res = await sut.CreateBoothAsync(2, dto);

            res.Should().NotBeNull();
            res.Id.Should().Be(77);
            res.SpaceId.Should().Be(2);
            res.MapSpotId.Should().Be(4);
            res.MapSpot!.X.Should().Be(0m);
            res.MapSpot.Y.Should().Be(0m);
            res.MapSpot.Z.Should().Be(0m);
            res.LocalizedPairs.Values.Should().ContainSingle(v => v.LocaleId == "en_us" && v.Value == "Hello");
        }


        // ---------- DeleteBoothCascadeAsync tests ----------

        /// <summary>Booth not found (name_key scalar is null) → returns false.</summary>
        [Fact]
        public async Task DeleteNotFoundReturnsFalse()
        {
            var fake = new FakeDbProvider(() => throw new InvalidOperationException("no reader expected"));
            // 1) fetch booth name_key -> null -> early false
            fake.EnqueueScalar(null);

            var sut = new BoothRepository(MakeConfig(), fake);

            var ok = await sut.DeleteBoothCascadeAsync(spaceId: 1, boothId: 999);

            ok.Should().BeFalse();
        }

        /// <summary>No portals: delete booth i18n and booth only → true.</summary>
        [Fact]
        public async Task DeleteNoPortalsJustBooth()
        {
            var fake = new FakeDbProvider(() => throw new InvalidOperationException("unexpected fallback"));
            // 1) fetch booth name_key
            fake.EnqueueScalar("bth_key");
            // 2) fetch portals -> none
            fake.EnqueueReader(() => PortalRows());
            // 5) delete booth i18n + booth
            fake.EnqueueNonQuery(1); // del i18n
            fake.EnqueueNonQuery(1); // del booth

            var sut = new BoothRepository(MakeConfig(), fake);
            var ok = await sut.DeleteBoothCascadeAsync(1, 10);

            ok.Should().BeTrue();
        }

        /// <summary>Portals with various media: cascades i18n/portal/media deletes → true.</summary>
        [Fact]
        public async Task DeleteWithPortalsCascadesAll()
        {
            var fake = new FakeDbProvider(() => throw new InvalidOperationException("unexpected fallback"));
            // 1) booth key
            fake.EnqueueScalar("bth_key");
            // 2) portals: p1(c1, null), p2(null, t2), p3(c3, t3)
            fake.EnqueueReader(() => PortalRows(
                (1, "p1_key", "c1", null),
                (2, "p2_key", null, "t2"),
                (3, "p3_key", "c3", "t3")
            ));
            // 3) delete each portal's i18n + portal row (3 * 2 = 6)
            fake.EnqueueNonQuery(1); // p1 i18n
            fake.EnqueueNonQuery(1); // p1 portal
            fake.EnqueueNonQuery(1); // p2 i18n
            fake.EnqueueNonQuery(1); // p2 portal
            fake.EnqueueNonQuery(1); // p3 i18n
            fake.EnqueueNonQuery(1); // p3 portal
                                     // 4) media deletions (p1: c1 only = 2)
            fake.EnqueueNonQuery(1); // del media_localization c1
            fake.EnqueueNonQuery(1); // del media c1
                                     //    (p2: t2 only = 2)
            fake.EnqueueNonQuery(1); // del media_localization t2
            fake.EnqueueNonQuery(1); // del media t2
                                     //    (p3: c3 + t3 = 4)
            fake.EnqueueNonQuery(1); // del media_localization c3
            fake.EnqueueNonQuery(1); // del media c3
            fake.EnqueueNonQuery(1); // del media_localization t3
            fake.EnqueueNonQuery(1); // del media t3
                                     // 5) delete booth i18n + booth
            fake.EnqueueNonQuery(1);
            fake.EnqueueNonQuery(1);

            var sut = new BoothRepository(MakeConfig(), fake);
            var ok = await sut.DeleteBoothCascadeAsync(1, 10);

            ok.Should().BeTrue();
        }

        /// <summary>Whitespace media ids are ignored (no media deletions).</summary>
        [Fact]
        public async Task DeleteWhitespaceMedia()
        {
            var fake = new FakeDbProvider(() => throw new InvalidOperationException("unexpected fallback"));
            // 1) booth key
            fake.EnqueueScalar("bth_key");
            // 2) one portal with whitespace corr/thumb → should NOT trigger media deletes
            fake.EnqueueReader(() => PortalRows(
                (5, "p5_key", "   ", " \t ")
            ));
            // 3) delete portal i18n + portal row (2)
            fake.EnqueueNonQuery(1);
            fake.EnqueueNonQuery(1);
            // 4) (no media deletes expected)
            // 5) delete booth i18n + booth (2)
            fake.EnqueueNonQuery(1);
            fake.EnqueueNonQuery(1);

            var sut = new BoothRepository(MakeConfig(), fake);
            var ok = await sut.DeleteBoothCascadeAsync(2, 5);

            ok.Should().BeTrue();
        }

    }
}
