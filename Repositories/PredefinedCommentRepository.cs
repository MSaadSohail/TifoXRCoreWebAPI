// Repositories/PredefinedCommentRepository.cs
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using System.Data.Common;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure; // IDbProvider

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public sealed class PredefinedCommentRepository : IPredefinedCommentRepository
    {
        private readonly IDbProvider _db;

        public PredefinedCommentRepository(IConfiguration configuration, IDbProvider db)
        {
            _db = db;
        }

        public async Task<List<PredefinedCommentModel>> GetPredefinedCommentsBySpaceAsync(int spaceId, bool includeInactive = false)
        {
            const string sqlBase = @"
SELECT
    pc.id,
    pc.comment_key,
    pc.is_active,
    pc.creation_time,
    i.locale_id,
    i.value
FROM predefined_comments pc
LEFT JOIN i18n i
       ON i.`key` = pc.comment_key
      AND i.space_id = @SpaceId
{0}
ORDER BY pc.id, i.locale_id;";

            var where = includeInactive ? "" : "WHERE pc.is_active = TRUE";
            var sql = string.Format(sqlBase, where);

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            var byId = new Dictionary<int, PredefinedCommentModel>();

            await using var r = await cmd.ExecuteReaderAsync();

            bool ordReady = false;
            int o_id = -1, o_key = -1, o_active = -1, o_ct = -1, o_loc = -1, o_val = -1;

            while (await r.ReadAsync())
            {
                if (!ordReady)
                {
                    o_id = r.GetOrdinal("id");
                    o_key = r.GetOrdinal("comment_key");
                    o_active = r.GetOrdinal("is_active");
                    o_ct = r.GetOrdinal("creation_time");
                    o_loc = r.GetOrdinal("locale_id");
                    o_val = r.GetOrdinal("value");
                    ordReady = true;
                }

                var id = r.GetInt32(o_id);

                if (!byId.TryGetValue(id, out var model))
                {
                    // is_active comes as bool directly
                    bool isActive = r.GetBoolean(o_active);

                    model = new PredefinedCommentModel
                    {
                        Id = id,
                        IsActive = isActive,
                        CreationTime = r.GetDateTime(o_ct),
                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = r.GetString(o_key),
                            Values = new List<LocalizedValue>()
                        }
                    };
                    byId[id] = model;
                }

                if (!r.IsDBNull(o_loc))
                {
                    var loc = r.GetString(o_loc);
                    if (!model.LocalizedPairs.Values.Any(x => x.LocaleId == loc))
                    {
                        model.LocalizedPairs.Values.Add(new LocalizedValue
                        {
                            LocaleId = loc,
                            Value = r.IsDBNull(o_val) ? string.Empty : r.GetString(o_val)
                        });
                    }
                }
            }

            return byId.Values.ToList();
        }

        public async Task<PredefinedCommentModel> CreatePredefinedCommentAsync(int spaceId, PredefinedCommentCreateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            var commentKey = Guid.NewGuid().ToString(); // backend-generated key
            var values = dto.Localizations ?? new List<LocalizedValue>();

            // De-dupe locales in request
            values = values
                .GroupBy(v => v.LocaleId, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Insert predefined_comments
                const string sqlIns = @"
INSERT INTO predefined_comments (comment_key, is_active, creation_time, modified_by)
VALUES (@K, @A, NOW(6), 'system');";
                await using (var insCmd = _db.CreateCommand(conn, sqlIns))
                {
                    insCmd.Transaction = tx;
                    insCmd.Parameters.Add(_db.CreateParameter("@K", commentKey));
                    insCmd.Parameters.Add(_db.CreateParameter("@A", dto.IsActive));
                    await insCmd.ExecuteNonQueryAsync();
                }

                int newId;
                await using (var idCmd = _db.CreateCommand(conn, "SELECT LAST_INSERT_ID();"))
                {
                    idCmd.Transaction = tx;
                    newId = Convert.ToInt32(await idCmd.ExecuteScalarAsync());
                }

                // 2) Validate supported locales and insert i18n (if any)
                var allLocales = values
                    .Select(v => v.LocaleId)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (allLocales.Count > 0)
                {
                    var paramNames = allLocales.Select((_, i) => $"@loc{i}").ToList();
                    var checkSql = $@"
SELECT locale_id
  FROM supported_languages
 WHERE locale_id IN ({string.Join(", ", paramNames)})
   AND space_id = @S;";

                    await using (var checkCmd = _db.CreateCommand(conn, checkSql))
                    {
                        checkCmd.Transaction = tx;
                        for (int i = 0; i < allLocales.Count; i++)
                            checkCmd.Parameters.Add(_db.CreateParameter(paramNames[i], allLocales[i]));
                        checkCmd.Parameters.Add(_db.CreateParameter("@S", spaceId));

                        await using var rdr = await checkCmd.ExecuteReaderAsync();
                        while (await rdr.ReadAsync())
                            supported.Add(rdr.IsDBNull(0) ? "" : rdr.GetString(0));
                    }

                    var unsupported = allLocales.Where(l => !supported.Contains(l)).ToList();
                    if (unsupported.Count > 0)
                        throw new InvalidOperationException($"Unsupported locales: {string.Join(", ", unsupported)}");
                }

                if (values.Count > 0)
                {
                    const string sqlInsI18n = @"
INSERT INTO i18n (`key`, locale_id, value, space_id)
VALUES (@K, @L, @V, @S);";

                    foreach (var v in values)
                    {
                        if (!supported.Contains(v.LocaleId)) continue;

                        await using var cmdI18n = _db.CreateCommand(conn, sqlInsI18n);
                        cmdI18n.Transaction = tx;
                        cmdI18n.Parameters.Add(_db.CreateParameter("@K", commentKey));
                        cmdI18n.Parameters.Add(_db.CreateParameter("@L", v.LocaleId));
                        cmdI18n.Parameters.Add(_db.CreateParameter("@V", v.Value ?? string.Empty));
                        cmdI18n.Parameters.Add(_db.CreateParameter("@S", spaceId));
                        await cmdI18n.ExecuteNonQueryAsync();
                    }
                }

                // 3) Reload creation_time & is_active for accuracy
                DateTime creation;
                bool isActive;
                const string sqlReload = @"SELECT creation_time, is_active FROM predefined_comments WHERE id = @Id;";
                await using (var rel = _db.CreateCommand(conn, sqlReload))
                {
                    rel.Transaction = tx;
                    rel.Parameters.Add(_db.CreateParameter("@Id", newId));
                    await using var rr = await rel.ExecuteReaderAsync();
                    await rr.ReadAsync();
                    creation = rr.GetDateTime(rr.GetOrdinal("creation_time"));
                    isActive = rr.GetBoolean(rr.GetOrdinal("is_active"));
                }

                await tx.CommitAsync();

                return new PredefinedCommentModel
                {
                    Id = newId,
                    IsActive = isActive,
                    CreationTime = creation,
                    LocalizedPairs = new LocalizedPairs
                    {
                        Key = commentKey,
                        Values = values
                            .Where(v => supported.Contains(v.LocaleId))
                            .Select(v => new LocalizedValue { LocaleId = v.LocaleId, Value = v.Value })
                            .ToList()
                    }
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<PredefinedCommentModel?> UpdatePredefinedCommentByIdAsync(int spaceId, int id, PredefinedCommentUpdateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            // De-dupe incoming locales (keep first occurrence)
            var incoming = dto.LocalizedPairs?.Values?
                .Where(v => !string.IsNullOrWhiteSpace(v.LocaleId))
                .GroupBy(v => v.LocaleId!, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList() ?? new List<LocalizedValue>();

            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 0) Fetch comment_key and current fields by id
                const string sqlFind = @"
SELECT comment_key, is_active, creation_time
FROM predefined_comments
WHERE id = @Id
LIMIT 1;";
                string? commentKey = null;
                DateTime creation = default;
                bool isActive = true;

                await using (var find = _db.CreateCommand(conn, sqlFind))
                {
                    find.Transaction = tx;
                    find.Parameters.Add(_db.CreateParameter("@Id", id));
                    await using var r = await find.ExecuteReaderAsync();
                    if (!await r.ReadAsync())
                    {
                        await tx.RollbackAsync();
                        return null; // not found
                    }

                    var o_key = r.GetOrdinal("comment_key");
                    var o_act = r.GetOrdinal("is_active");
                    var o_ct = r.GetOrdinal("creation_time");

                    commentKey = r.GetString(o_key);
                    isActive = r.GetBoolean(o_act);
                    creation = r.GetDateTime(o_ct);
                }

                // 1) Update is_active if provided
                if (dto.IsActive.HasValue)
                {
                    const string sqlUpd = @"UPDATE predefined_comments SET is_active = @A WHERE id = @Id;";
                    await using var upd = _db.CreateCommand(conn, sqlUpd);
                    upd.Transaction = tx;
                    upd.Parameters.Add(_db.CreateParameter("@A", dto.IsActive.Value));
                    upd.Parameters.Add(_db.CreateParameter("@Id", id));
                    await upd.ExecuteNonQueryAsync();
                    isActive = dto.IsActive.Value; // reflect in the response
                }

                // 2) Validate locales against supported_languages and UPSERT i18n
                if (incoming.Count > 0)
                {
                    var locales = incoming.Select(v => v.LocaleId!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                    // Validate
                    var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var paramNames = locales.Select((_, i) => $"@loc{i}").ToList();
                    var checkSql = $@"
SELECT locale_id
  FROM supported_languages
 WHERE locale_id IN ({string.Join(", ", paramNames)})
   AND space_id = @S;";
                    await using (var chk = _db.CreateCommand(conn, checkSql))
                    {
                        chk.Transaction = tx;
                        for (int i = 0; i < locales.Count; i++)
                            chk.Parameters.Add(_db.CreateParameter(paramNames[i], locales[i]));
                        chk.Parameters.Add(_db.CreateParameter("@S", spaceId));

                        await using var rdr = await chk.ExecuteReaderAsync();
                        while (await rdr.ReadAsync())
                            supported.Add(rdr.IsDBNull(0) ? "" : rdr.GetString(0));
                    }

                    var unsupported = locales.Where(l => !supported.Contains(l)).ToList();
                    if (unsupported.Count > 0)
                        throw new InvalidOperationException($"Unsupported locales: {string.Join(", ", unsupported)}");

                    // Upsert
                    const string sqlUpdateI18n = @"
UPDATE i18n
   SET value = @V
 WHERE `key` = @K AND locale_id = @L AND space_id = @S;";
                    const string sqlInsertI18n = @"
INSERT INTO i18n (`key`, locale_id, value, space_id)
VALUES (@K, @L, @V, @S);";

                    foreach (var v in incoming)
                    {
                        await using var up = _db.CreateCommand(conn, sqlUpdateI18n);
                        up.Transaction = tx;
                        up.Parameters.Add(_db.CreateParameter("@K", commentKey!));
                        up.Parameters.Add(_db.CreateParameter("@L", v.LocaleId!));
                        up.Parameters.Add(_db.CreateParameter("@V", v.Value ?? string.Empty));
                        up.Parameters.Add(_db.CreateParameter("@S", spaceId));

                        var affected = await up.ExecuteNonQueryAsync();
                        if (affected == 0)
                        {
                            await using var ins = _db.CreateCommand(conn, sqlInsertI18n);
                            ins.Transaction = tx;
                            ins.Parameters.Add(_db.CreateParameter("@K", commentKey!));
                            ins.Parameters.Add(_db.CreateParameter("@L", v.LocaleId!));
                            ins.Parameters.Add(_db.CreateParameter("@V", v.Value ?? string.Empty));
                            ins.Parameters.Add(_db.CreateParameter("@S", spaceId));
                            await ins.ExecuteNonQueryAsync();
                        }
                    }
                }

                await tx.CommitAsync();

                // 3) Reload and return the updated model (all locales for this id/key/space)
                return await LoadByKeyAsync(conn, spaceId, commentKey!);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private async Task<PredefinedCommentModel?> LoadByKeyAsync(DbConnection conn, int spaceId, string commentKey)
        {
            const string sql = @"
SELECT
    pc.id,
    pc.comment_key,
    pc.is_active,
    pc.creation_time,
    i.locale_id,
    i.value
FROM predefined_comments pc
LEFT JOIN i18n i
       ON i.`key` = pc.comment_key
      AND i.space_id = @SpaceId
WHERE pc.comment_key = @K
ORDER BY i.locale_id;";

            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@K", commentKey));

            await using var r = await cmd.ExecuteReaderAsync();

            PredefinedCommentModel? model = null;
            bool ordReady = false;
            int o_id = -1, o_key = -1, o_active = -1, o_ct = -1, o_loc = -1, o_val = -1;

            while (await r.ReadAsync())
            {
                if (!ordReady)
                {
                    o_id = r.GetOrdinal("id");
                    o_key = r.GetOrdinal("comment_key");
                    o_active = r.GetOrdinal("is_active");
                    o_ct = r.GetOrdinal("creation_time");
                    o_loc = r.GetOrdinal("locale_id");
                    o_val = r.GetOrdinal("value");
                    ordReady = true;
                }

                model ??= new PredefinedCommentModel
                {
                    Id = r.GetInt32(o_id),
                    IsActive = r.GetBoolean(o_active),
                    CreationTime = r.GetDateTime(o_ct),
                    LocalizedPairs = new LocalizedPairs
                    {
                        Key = r.GetString(o_key),
                        Values = new List<LocalizedValue>()
                    }
                };

                if (!r.IsDBNull(o_loc))
                {
                    var loc = r.GetString(o_loc);
                    if (!model.LocalizedPairs.Values.Any(v => v.LocaleId == loc))
                    {
                        model.LocalizedPairs.Values.Add(new LocalizedValue
                        {
                            LocaleId = loc,
                            Value = r.IsDBNull(o_val) ? string.Empty : r.GetString(o_val)
                        });
                    }
                }
            }

            return model;
        }

        public async Task<PredefinedCommentDeleteResult> DeletePredefinedCommentAsync(int spaceId, int id, bool hard = false, bool deleteI18nForSpace = false)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 0) Load comment_key (and existence)
                const string sqlFind = @"SELECT comment_key FROM predefined_comments WHERE id = @Id LIMIT 1;";
                string? commentKey = null;
                await using (var find = _db.CreateCommand(conn, sqlFind))
                {
                    find.Transaction = tx;
                    find.Parameters.Add(_db.CreateParameter("@Id", id));
                    var o = await find.ExecuteScalarAsync();
                    if (o is string s) commentKey = s;
                }

                if (string.IsNullOrEmpty(commentKey))
                {
                    await tx.RollbackAsync();
                    return new PredefinedCommentDeleteResult { NotFound = true };
                }

                if (hard)
                {
                    // 1) Block hard delete if referenced by booth_comment
                    const string sqlRefCount = @"SELECT COUNT(*) FROM booth_comment WHERE predefined_comment_id = @Id;";
                    long refCount;
                    await using (var rc = _db.CreateCommand(conn, sqlRefCount))
                    {
                        rc.Transaction = tx;
                        rc.Parameters.Add(_db.CreateParameter("@Id", id));
                        refCount = Convert.ToInt64(await rc.ExecuteScalarAsync());
                    }

                    if (refCount > 0)
                    {
                        await tx.RollbackAsync();
                        return new PredefinedCommentDeleteResult { ConflictInUse = true };
                    }

                    // 2) Delete i18n for ALL spaces for this key
                    const string sqlDelI18nAll = @"DELETE FROM i18n WHERE `key` = @K;";
                    await using (var di = _db.CreateCommand(conn, sqlDelI18nAll))
                    {
                        di.Transaction = tx;
                        di.Parameters.Add(_db.CreateParameter("@K", commentKey));
                        await di.ExecuteNonQueryAsync();
                    }

                    // 3) Delete predefined_comments row
                    const string sqlDelPc = @"DELETE FROM predefined_comments WHERE id = @Id;";
                    await using (var dp = _db.CreateCommand(conn, sqlDelPc))
                    {
                        dp.Transaction = tx;
                        dp.Parameters.Add(_db.CreateParameter("@Id", id));
                        await dp.ExecuteNonQueryAsync();
                    }

                    await tx.CommitAsync();
                    return new PredefinedCommentDeleteResult { HardDeleted = true };
                }
                else
                {
                    // SOFT DELETE: mark inactive
                    const string sqlSoft = @"UPDATE predefined_comments SET is_active = FALSE WHERE id = @Id;";
                    await using (var sd = _db.CreateCommand(conn, sqlSoft))
                    {
                        sd.Transaction = tx;
                        sd.Parameters.Add(_db.CreateParameter("@Id", id));
                        await sd.ExecuteNonQueryAsync();
                    }

                    if (deleteI18nForSpace)
                    {
                        // Remove i18n only for this space to "unregister" in that context
                        const string sqlDelI18nSpace = @"DELETE FROM i18n WHERE `key` = @K AND space_id = @S;";
                        await using (var ds = _db.CreateCommand(conn, sqlDelI18nSpace))
                        {
                            ds.Transaction = tx;
                            ds.Parameters.Add(_db.CreateParameter("@K", commentKey));
                            ds.Parameters.Add(_db.CreateParameter("@S", spaceId));
                            await ds.ExecuteNonQueryAsync();
                        }
                    }

                    await tx.CommitAsync();
                    return new PredefinedCommentDeleteResult { SoftDeleted = true };
                }
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

    }
}
