using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Sql;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;
using System.Data.Common;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public sealed class ShopRepository : IShopRepository
    {
        private readonly IDbProvider _db;
        public ShopRepository(IDbProvider db) => _db = db;

        public async Task<IReadOnlyList<ShopDto>> ListBySpaceAsync(int spaceId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopSql.ListShopsBySpace);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            var list = new List<ShopDto>();
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                list.Add(MapShop(rdr));
            return list;
        }

        public async Task<ShopDto?> GetByIdAsync(int spaceId, int shopId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopSql.GetById);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@ShopId", shopId));

            await using var rdr = await cmd.ExecuteReaderAsync();
            return await rdr.ReadAsync() ? MapShop(rdr) : null;
        }

        public async Task<ShopDto> CreateAsync(int spaceId, CreateShopDto dto, string modifiedBy)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {

                // Insert shop
                int newId;
                await using (var cmd = _db.CreateCommand(conn, ShopSql.InsertShop, tx))
                {
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    cmd.Parameters.Add(_db.CreateParameter("@PlatformTypeId", dto.PlatformTypeId));
                    cmd.Parameters.Add(_db.CreateParameter("@EntityId", (object?)dto.EntityId ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@NameKey", dto.NameKey));
                    cmd.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                    var obj = await cmd.ExecuteScalarAsync();
                    newId = Convert.ToInt32(obj);
                }

                // Insert i18n if provided
                if (dto.LocalizedPairs is not null && dto.LocalizedPairs.Values?.Count > 0)
                {
                    foreach (var v in dto.LocalizedPairs.Values)
                    {
                        await using var i18n = _db.CreateCommand(conn, ShopSql.InsertI18n, tx);
                        i18n.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        i18n.Parameters.Add(_db.CreateParameter("@Key", dto.NameKey));
                        i18n.Parameters.Add(_db.CreateParameter("@LocaleId", v.LocaleId));
                        i18n.Parameters.Add(_db.CreateParameter("@Value", v.Value));
                        i18n.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                        await i18n.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();

                // Return created row
                await using var get = _db.CreateCommand(conn, ShopSql.GetById);
                get.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                get.Parameters.Add(_db.CreateParameter("@ShopId", newId));
                await using var rdr = await get.ExecuteReaderAsync();
                if (await rdr.ReadAsync())
                    return MapShop(rdr);

                throw new InvalidOperationException("Created shop not found.");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        public async Task InsertI18nAsync(int spaceId, string key, IEnumerable<LocalizedValue> values, string modifiedBy)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                foreach (var v in values)
                {
                    await using var cmd = _db.CreateCommand(conn, ShopSql.InsertI18n, tx);
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    cmd.Parameters.Add(_db.CreateParameter("@Key", key));
                    cmd.Parameters.Add(_db.CreateParameter("@LocaleId", v.LocaleId));
                    cmd.Parameters.Add(_db.CreateParameter("@Value", v.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                    await cmd.ExecuteNonQueryAsync();
                }
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> SpaceExistsAsync(int spaceId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopSql.ExistsSpace);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            var obj = await cmd.ExecuteScalarAsync();
            return obj is not null;
        }

        public async Task<bool> PlatformTypeExistsAsync(int platformTypeId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopSql.ExistsPlatformType);
            cmd.Parameters.Add(_db.CreateParameter("@Id", platformTypeId));
            var obj = await cmd.ExecuteScalarAsync();
            return obj is not null;
        }

        public async Task<bool> EntityExistsAsync(int entityId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopSql.ExistsEntity);
            cmd.Parameters.Add(_db.CreateParameter("@Id", entityId));
            var obj = await cmd.ExecuteScalarAsync();
            return obj is not null;
        }

        public async Task<HashSet<string>> GetSupportedLocalesAsync(int spaceId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopSql.GetSupportedLocales);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                set.Add(rdr.GetString(0));
            return set;
        }
        public async Task<bool> ExistsInSpaceAsync(int spaceId, int shopId)
        {
            // lightweight existence check; avoids mapping
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopSql.GetById);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
            await using var rdr = await cmd.ExecuteReaderAsync();
            return await rdr.ReadAsync();
        }

        public async Task<string?> GetCurrentNameKeyAsync(int spaceId, int shopId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopSql.GetCurrentNameKey);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
            var obj = await cmd.ExecuteScalarAsync();
            return obj is null or DBNull ? null : Convert.ToString(obj);
        }

        public async Task<bool> IsNameKeyDuplicateAsync(int spaceId, string nameKey, int excludeShopId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopSql.CheckDuplicateNameKey);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@ShopId", excludeShopId));
            cmd.Parameters.Add(_db.CreateParameter("@NameKey", nameKey));
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        public async Task UpdatePartialAsync(
            int spaceId,
            int shopId,
            int? platformTypeId,
            bool hasEntityId,
            int? entityId,
            string? nameKey,
            string modifiedBy)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopSql.UpdateShopPartial);

            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
            cmd.Parameters.Add(_db.CreateParameter("@PlatformTypeId", (object?)platformTypeId ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@HasEntityId", hasEntityId ? 1 : 0));
            cmd.Parameters.Add(_db.CreateParameter("@EntityId", (object?)entityId ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@NameKey", (object?)nameKey ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));

            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// UPDATE-first, INSERT-if-missing i18n upsert within a single transaction.
        /// </summary>
        public async Task UpsertI18nAsync(int spaceId, string key, IEnumerable<LocalizedValue> values, string modifiedBy)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                foreach (var v in values)
                {
                    // Try UPDATE
                    await using (var upd = _db.CreateCommand(conn, ShopSql.UpdateI18nValue, tx))
                    {
                        upd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        upd.Parameters.Add(_db.CreateParameter("@Key", key));
                        upd.Parameters.Add(_db.CreateParameter("@LocaleId", v.LocaleId));
                        upd.Parameters.Add(_db.CreateParameter("@Value", v.Value));
                        upd.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                        var affected = await upd.ExecuteNonQueryAsync();

                        if (affected == 0)
                        {
                            // INSERT if not present
                            await using var ins = _db.CreateCommand(conn, ShopSql.InsertI18n, tx);
                            ins.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                            ins.Parameters.Add(_db.CreateParameter("@Key", key));
                            ins.Parameters.Add(_db.CreateParameter("@LocaleId", v.LocaleId));
                            ins.Parameters.Add(_db.CreateParameter("@Value", v.Value));
                            ins.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                            await ins.ExecuteNonQueryAsync();
                        }
                    }
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<int> HardDeleteAsync(int spaceId, int shopId, string modifiedBy)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                // Guard: ensure it belongs to this space
                await using (var existsCmd = _db.CreateCommand(conn, ShopSql.ExistsShopInSpace, tx))
                {
                    existsCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    existsCmd.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
                    var exists = await existsCmd.ExecuteScalarAsync();
                    if (exists is null)
                    {
                        await tx.RollbackAsync();
                        return 0;
                    }
                }

                // delete i18n for the shop's own name_key
                string? shopNameKey = null;
                await using (var getKey = _db.CreateCommand(conn, ShopSql.GetShopNameKey, tx))
                {
                    getKey.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    getKey.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
                    var obj = await getKey.ExecuteScalarAsync();
                    if (obj is string s && !string.IsNullOrWhiteSpace(s)) shopNameKey = s;
                }
                if (!string.IsNullOrWhiteSpace(shopNameKey))
                {
                    await using var delShopKey = _db.CreateCommand(conn, ShopSql.DeleteI18nByKey, tx);
                    delShopKey.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    delShopKey.Parameters.Add(_db.CreateParameter("@Key", shopNameKey!));
                    await delShopKey.ExecuteNonQueryAsync();
                }

                // delete i18n for item keys that are exclusive to this shop in this space
                var keysToDelete = new List<(string key, bool isNull)>();
                await using (var sel = _db.CreateCommand(conn, ShopSql.SelectExclusiveItemKeysForShopInSpace, tx))
                {
                    sel.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    sel.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
                    await using var rdr = await sel.ExecuteReaderAsync();
                    var ordName = rdr.GetOrdinal("NameKey");
                    var ordDesc = rdr.GetOrdinal("DescriptionKey");
                    while (await rdr.ReadAsync())
                    {
                        if (!rdr.IsDBNull(ordName))
                            keysToDelete.Add((rdr.GetString(ordName), false));
                        if (!rdr.IsDBNull(ordDesc))
                            keysToDelete.Add((rdr.GetString(ordDesc), false));
                    }
                }
                foreach (var (key, _) in keysToDelete)
                {
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    await using var del = _db.CreateCommand(conn, ShopSql.DeleteI18nByKey, tx);
                    del.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    del.Parameters.Add(_db.CreateParameter("@Key", key));
                    await del.ExecuteNonQueryAsync();
                }

                // 1) delete children (shop_items)
                await using (var delItems = _db.CreateCommand(conn, ShopSql.DeleteShopItemsByShopInSpace, tx))
                {
                    delItems.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    delItems.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
                    await delItems.ExecuteNonQueryAsync();
                }

                // 2) delete the shop
                int affected;
                await using (var delShop = _db.CreateCommand(conn, ShopSql.DeleteShopByIdInSpace, tx))
                {
                    delShop.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    delShop.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
                    affected = await delShop.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return affected;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();

                if (ex.Message.IndexOf("FOREIGN KEY", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    ex.Message.IndexOf("constraint", StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new InvalidOperationException("FK_CONSTRAINT: shop hard delete blocked.", ex);

                if (ex.Message.StartsWith("Already committed or rolled back", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("TX_ALREADY_COMPLETED", ex);

                throw;
            }
        }


        private static ShopDto MapShop(DbDataReader rdr) => new()
        {
            Id = rdr.GetInt32(rdr.GetOrdinal("Id")),
            SpaceId = rdr.GetInt32(rdr.GetOrdinal("SpaceId")),
            PlatformTypeId = rdr.GetInt32(rdr.GetOrdinal("PlatformTypeId")),
            EntityId = rdr.IsDBNull(rdr.GetOrdinal("EntityId")) ? (int?)null : rdr.GetInt32(rdr.GetOrdinal("EntityId")),
            NameKey = rdr.GetString(rdr.GetOrdinal("NameKey")),
            CreationTime = rdr.GetDateTime(rdr.GetOrdinal("CreationTime")),
            ModifiedTime = rdr.GetDateTime(rdr.GetOrdinal("ModifiedTime")),
            ModifiedBy = rdr.GetString(rdr.GetOrdinal("ModifiedBy"))
        };
    }
}
