// <copyright file="DiscountRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>11/26/2025</date>
// <summary>SQL statements for discount definitions listing.</summary>
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Sql;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;
using System.Data.Common;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public sealed class DiscountsRepository : IDiscountsRepository
    {
        private readonly IDbProvider _db;
        public DiscountsRepository(IDbProvider db) => _db = db;

        public async Task<IReadOnlyList<DiscountDto>> ListActiveBySpaceAsync(int spaceId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, DiscountsSql.ListActiveBySpace);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            var list = new List<DiscountDto>();
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                list.Add(Map(rdr));
            return list;
        }

        private static DiscountDto Map(DbDataReader r) => new()
        {
            Id = r.GetInt32(r.GetOrdinal("id")),
            SpaceId = r.GetInt32(r.GetOrdinal("space_id")),
            DiscountTypeId = r.GetInt32(r.GetOrdinal("discount_type_id")),
            NameKey = r.GetString(r.GetOrdinal("name_key")),
            ValueMinorUnits = r.GetInt64(r.GetOrdinal("value_minor_units")),
            StartAt = r.IsDBNull(r.GetOrdinal("start_at")) ? (DateTime?)null : r.GetDateTime(r.GetOrdinal("start_at")),
            EndAt = r.IsDBNull(r.GetOrdinal("end_at")) ? (DateTime?)null : r.GetDateTime(r.GetOrdinal("end_at")),
            Metadata = r.IsDBNull(r.GetOrdinal("metadata")) ? null : r.GetString(r.GetOrdinal("metadata")),
            CreationTime = r.GetDateTime(r.GetOrdinal("creation_time")),
            ModifiedTime = r.GetDateTime(r.GetOrdinal("modified_time")),
            ModifiedBy = r.GetString(r.GetOrdinal("modified_by"))
        };

        public async Task<HashSet<string>> GetSupportedLocalesAsync(int spaceId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, DiscountsSql.GetSupportedLocales);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                set.Add(rdr.GetString(0));
            return set;
        }

        public async Task<bool> DiscountTypeExistsAsync(int discountTypeId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, DiscountsSql.ExistsDiscountType);
            cmd.Parameters.Add(_db.CreateParameter("@DiscountTypeId", discountTypeId));
            var obj = await cmd.ExecuteScalarAsync();
            return obj is not null;
        }

        public async Task<DiscountDto> CreateAsync(int spaceId, CreateDiscountDto dto, string modifiedBy)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                int newId;
                // insert discount
                await using (var cmd = _db.CreateCommand(conn, DiscountsSql.InsertDiscount, tx))
                {
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    cmd.Parameters.Add(_db.CreateParameter("@DiscountTypeId", dto.DiscountTypeId));
                    cmd.Parameters.Add(_db.CreateParameter("@NameKey", dto.NameKey));
                    cmd.Parameters.Add(_db.CreateParameter("@Value", dto.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@StartAt", (object?)dto.StartAt ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@EndAt", (object?)dto.EndAt ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@Metadata", (object?)dto.Metadata ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                    var obj = await cmd.ExecuteScalarAsync();
                    newId = Convert.ToInt32(obj);
                }

                // i18n inserts (optional)
                if (dto.LocalizedPairs?.Values is { Count: > 0 })
                {
                    foreach (var v in dto.LocalizedPairs.Values)
                    {
                        await using var i18n = _db.CreateCommand(conn, DiscountsSql.InsertI18n, tx);
                        i18n.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        i18n.Parameters.Add(_db.CreateParameter("@Key", dto.NameKey));
                        i18n.Parameters.Add(_db.CreateParameter("@LocaleId", v.LocaleId));
                        i18n.Parameters.Add(_db.CreateParameter("@Value", v.Value));
                        i18n.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                        await i18n.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();

                // fetch created
                await using var get = _db.CreateCommand(conn, DiscountsSql.GetById);
                get.Parameters.Add(_db.CreateParameter("@Id", newId));
                get.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                await using var rdr = await get.ExecuteReaderAsync();
                if (await rdr.ReadAsync())
                {
                    return new DiscountDto
                    {
                        Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                        SpaceId = rdr.GetInt32(rdr.GetOrdinal("space_id")),
                        DiscountTypeId = rdr.GetInt32(rdr.GetOrdinal("discount_type_id")),
                        NameKey = rdr.GetString(rdr.GetOrdinal("name_key")),
                        ValueMinorUnits = rdr.GetInt64(rdr.GetOrdinal("value_minor_units")),
                        StartAt = rdr.IsDBNull(rdr.GetOrdinal("start_at")) ? (DateTime?)null : rdr.GetDateTime(rdr.GetOrdinal("start_at")),
                        EndAt = rdr.IsDBNull(rdr.GetOrdinal("end_at")) ? (DateTime?)null : rdr.GetDateTime(rdr.GetOrdinal("end_at")),
                        Metadata = rdr.IsDBNull(rdr.GetOrdinal("metadata")) ? null : rdr.GetString(rdr.GetOrdinal("metadata")),
                        CreationTime = rdr.GetDateTime(rdr.GetOrdinal("creation_time")),
                        ModifiedTime = rdr.GetDateTime(rdr.GetOrdinal("modified_time")),
                        ModifiedBy = rdr.GetString(rdr.GetOrdinal("modified_by"))
                    };
                }

                throw new InvalidOperationException("Created discount not found.");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<DiscountDto?> UpdateAsync(int spaceId, int discountId, UpdateDiscountDto dto, string modifiedBy)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            string oldNameKey;

            try
            {
                // 1) Load existing (FOR UPDATE)
                await using (var get = _db.CreateCommand(conn, DiscountsSql.GetForUpdate, tx))
                {
                    get.Parameters.Add(_db.CreateParameter("@DiscountId", discountId));
                    get.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                    await using var rdr = await get.ExecuteReaderAsync();
                    if (!await rdr.ReadAsync())
                    {
                        // no row; nothing to update
                        return null;
                    }
                    oldNameKey = rdr.GetString(rdr.GetOrdinal("name_key"));
                }

                // 2) Uniqueness (if name changes)
                var finalNameKey = dto.NameKey ?? oldNameKey;
                if (!string.Equals(finalNameKey, oldNameKey, StringComparison.Ordinal))
                {
                    await using var chk = _db.CreateCommand(conn, DiscountsSql.ExistsNameKeyInSpaceExceptId, tx);
                    chk.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    chk.Parameters.Add(_db.CreateParameter("@NameKey", finalNameKey));
                    chk.Parameters.Add(_db.CreateParameter("@DiscountId", discountId));
                    var exists = await chk.ExecuteScalarAsync();
                    if (exists is not null)
                        throw new InvalidOperationException("Duplicate name_key in this space.");
                }

                // 3) Update discounts row
                await using (var upd = _db.CreateCommand(conn, DiscountsSql.UpdateDiscount, tx))
                {
                    upd.Parameters.Add(_db.CreateParameter("@DiscountId", discountId));
                    upd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    upd.Parameters.Add(_db.CreateParameter("@NewTypeId", (object?)dto.DiscountTypeId ?? DBNull.Value));
                    upd.Parameters.Add(_db.CreateParameter("@NewNameKey", (object?)dto.NameKey ?? DBNull.Value));
                    upd.Parameters.Add(_db.CreateParameter("@NewValue", (object?)dto.Value ?? DBNull.Value));
                    upd.Parameters.Add(_db.CreateParameter("@NewStartAt", (object?)dto.StartAt ?? DBNull.Value));
                    upd.Parameters.Add(_db.CreateParameter("@NewEndAt", (object?)dto.EndAt ?? DBNull.Value));
                    upd.Parameters.Add(_db.CreateParameter("@NewMetadata", (object?)dto.Metadata ?? DBNull.Value));
                    upd.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                    await upd.ExecuteNonQueryAsync();
                }

                // 4) Upsert i18n if provided
                if (dto.LocalizedPairs?.Values is { Count: > 0 })
                {
                    foreach (var v in dto.LocalizedPairs.Values)
                    {
                        // UPDATE
                        await using (var up = _db.CreateCommand(conn, DiscountsSql.UpdateI18nValue, tx))
                        {
                            up.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                            up.Parameters.Add(_db.CreateParameter("@Key", finalNameKey));
                            up.Parameters.Add(_db.CreateParameter("@LocaleId", v.LocaleId));
                            up.Parameters.Add(_db.CreateParameter("@Value", v.Value));
                            up.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                            var rows = await up.ExecuteNonQueryAsync();

                            if (rows == 0)
                            {
                                await using var ins = _db.CreateCommand(conn, DiscountsSql.InsertI18n, tx);
                                ins.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                                ins.Parameters.Add(_db.CreateParameter("@Key", finalNameKey));
                                ins.Parameters.Add(_db.CreateParameter("@LocaleId", v.LocaleId));
                                ins.Parameters.Add(_db.CreateParameter("@Value", v.Value));
                                ins.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                                await ins.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

               
                await tx.CommitAsync();
            }
            catch
            {
                // Guard rollback to avoid "Already committed or rolled back."
                if (tx?.Connection is not null)
                {
                    try { await tx.RollbackAsync(); } catch { /* swallow */ }
                }
                throw;
            }

            
            await using var sel = _db.CreateCommand(conn, DiscountsSql.GetById);
            sel.Parameters.Add(_db.CreateParameter("@DiscountId", discountId));
            sel.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            await using var rdr2 = await sel.ExecuteReaderAsync();
            if (!await rdr2.ReadAsync()) return null;

            return new DiscountDto
            {
                Id = rdr2.GetInt32(rdr2.GetOrdinal("id")),
                SpaceId = rdr2.GetInt32(rdr2.GetOrdinal("space_id")),
                DiscountTypeId = rdr2.GetInt32(rdr2.GetOrdinal("discount_type_id")),
                NameKey = rdr2.GetString(rdr2.GetOrdinal("name_key")),
                ValueMinorUnits = rdr2.GetInt64(rdr2.GetOrdinal("value_minor_units")),
                StartAt = rdr2.IsDBNull(rdr2.GetOrdinal("start_at")) ? (DateTime?)null : rdr2.GetDateTime(rdr2.GetOrdinal("start_at")),
                EndAt = rdr2.IsDBNull(rdr2.GetOrdinal("end_at")) ? (DateTime?)null : rdr2.GetDateTime(rdr2.GetOrdinal("end_at")),
                Metadata = rdr2.IsDBNull(rdr2.GetOrdinal("metadata")) ? null : rdr2.GetString(rdr2.GetOrdinal("metadata")),
                CreationTime = rdr2.GetDateTime(rdr2.GetOrdinal("creation_time")),
                ModifiedTime = rdr2.GetDateTime(rdr2.GetOrdinal("modified_time")),
                ModifiedBy = rdr2.GetString(rdr2.GetOrdinal("modified_by"))
            };
        }

        public async Task<bool> ExistsInSpaceAsync(int spaceId, int discountId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, DiscountsSql.ExistsInSpace);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@DiscountId", discountId));
            var obj = await cmd.ExecuteScalarAsync();
            return obj is not null;
        }

        public async Task<int> SoftExpireNowAsync(int spaceId, int discountId, string modifiedBy)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, DiscountsSql.SoftExpireNow);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@DiscountId", discountId));
            cmd.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> HardDeleteAsync(int spaceId, int discountId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, DiscountsSql.HardDelete);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@DiscountId", discountId));
            return await cmd.ExecuteNonQueryAsync();
        }

    }
}

