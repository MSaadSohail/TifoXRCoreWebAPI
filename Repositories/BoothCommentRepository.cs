// Repositories/BoothCommentRepository.cs
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using System.Data;
using System.Data.Common;
using TifoXRCoreWebAPI.Utilities.Infrastructure.Interface; // IDbProvider

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public sealed class BoothCommentRepository : IBoothCommentRepository
    {
        private readonly IDbProvider _db;

        public BoothCommentRepository(IConfiguration configuration, IDbProvider db)
        {
            _db = db;
        }

        public async Task<BoothCommentModel> CreateBoothCommentAsync(int spaceId, BoothCommentCreateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.UserId)) throw new ArgumentException("userId is required.");
            if (dto.PredefinedCommentId <= 0) throw new ArgumentException("predefinedCommentId is required.");

            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 0) Validate user exists (optional but nicer than FK error)
                const string sqlUser = @"SELECT 1 FROM `user` WHERE id = @U LIMIT 1;";
                await using (var u = _db.CreateCommand(conn, sqlUser))
                {
                    u.Transaction = tx;
                    u.Parameters.Add(_db.CreateParameter("@U", dto.UserId));
                    var exists = await u.ExecuteScalarAsync();
                    if (exists is null)
                        throw new InvalidOperationException($"Unknown user '{dto.UserId}'.");
                }

                // 1) Fetch predefined comment (must exist and be active)
                const string sqlPc = @"SELECT comment_key, is_active FROM predefined_comments WHERE id = @Pid LIMIT 1;";
                string? commentKey = null;
                bool pcActive = false;

                await using (var pc = _db.CreateCommand(conn, sqlPc))
                {
                    pc.Transaction = tx;
                    pc.Parameters.Add(_db.CreateParameter("@Pid", dto.PredefinedCommentId));
                    await using var r = await pc.ExecuteReaderAsync();
                    if (!await r.ReadAsync())
                        throw new InvalidOperationException($"Predefined comment id {dto.PredefinedCommentId} not found.");

                    commentKey = r.GetString(r.GetOrdinal("comment_key"));
                    pcActive = r.GetBoolean(r.GetOrdinal("is_active"));
                }

                if (!pcActive)
                    throw new InvalidOperationException("Selected predefined comment is inactive.");

                // 2) Idempotency on (spaceId, userId, predefined_comment_id)
                const string sqlFindExisting = @"
SELECT id, is_active, creation_time
FROM booth_comment
WHERE space_id = @S AND user_id = @U AND predefined_comment_id = @Pid
ORDER BY id
LIMIT 1;";

                int? existingId = null;
                bool existingActive = false;
                DateTime existingCreation = default;

                await using (var f = _db.CreateCommand(conn, sqlFindExisting))
                {
                    f.Transaction = tx;
                    f.Parameters.Add(_db.CreateParameter("@S", spaceId));
                    f.Parameters.Add(_db.CreateParameter("@U", dto.UserId));
                    f.Parameters.Add(_db.CreateParameter("@Pid", dto.PredefinedCommentId));
                    await using var r = await f.ExecuteReaderAsync();
                    if (await r.ReadAsync())
                    {
                        existingId = r.GetInt32(r.GetOrdinal("id"));
                        existingActive = r.GetBoolean(r.GetOrdinal("is_active"));
                        existingCreation = r.GetDateTime(r.GetOrdinal("creation_time"));
                    }
                }

                int resultingId;
                bool resultingActive;
                DateTime resultingCreation;

                if (existingId.HasValue)
                {
                    if (!existingActive && dto.IsActive)
                    {
                        // Revive inactive entry
                        const string sqlUpd = @"UPDATE booth_comment SET is_active = TRUE WHERE id = @Id;";
                        await using var upd = _db.CreateCommand(conn, sqlUpd);
                        upd.Transaction = tx;
                        upd.Parameters.Add(_db.CreateParameter("@Id", existingId.Value));
                        await upd.ExecuteNonQueryAsync();

                        resultingId = existingId.Value;
                        resultingActive = true;
                        resultingCreation = existingCreation; // keep original created time
                    }
                    else
                    {
                        // Already active, or caller requested inactive (we keep it as-is per idempotency)
                        resultingId = existingId.Value;
                        resultingActive = existingActive;
                        resultingCreation = existingCreation;
                    }
                }
                else
                {
                    // 3) Insert new booth_comment
                    const string sqlIns = @"
INSERT INTO booth_comment (space_id, user_id, predefined_comment_id, is_active, creation_time, modified_by)
VALUES (@S, @U, @Pid, @A, NOW(6), 'system');";
                    await using (var ins = _db.CreateCommand(conn, sqlIns))
                    {
                        ins.Transaction = tx;
                        ins.Parameters.Add(_db.CreateParameter("@S", spaceId));
                        ins.Parameters.Add(_db.CreateParameter("@U", dto.UserId));
                        ins.Parameters.Add(_db.CreateParameter("@Pid", dto.PredefinedCommentId));
                        ins.Parameters.Add(_db.CreateParameter("@A", dto.IsActive));
                        await ins.ExecuteNonQueryAsync();
                    }

                    await using (var idCmd = _db.CreateCommand(conn, "SELECT LAST_INSERT_ID();"))
                    {
                        idCmd.Transaction = tx;
                        resultingId = Convert.ToInt32(await idCmd.ExecuteScalarAsync());
                    }

                    resultingActive = dto.IsActive;
                    resultingCreation = DateTime.UtcNow; // will reload next step anyway if needed
                }

                // 4) Load localized values for this comment_key in the space
                var localized = new List<LocalizedValue>();
                const string sqlI18n = @"SELECT locale_id, value FROM i18n WHERE space_id = @S AND `key` = @K ORDER BY locale_id;";
                await using (var i18n = _db.CreateCommand(conn, sqlI18n))
                {
                    i18n.Transaction = tx;
                    i18n.Parameters.Add(_db.CreateParameter("@S", spaceId));
                    i18n.Parameters.Add(_db.CreateParameter("@K", commentKey!));
                    await using var r = await i18n.ExecuteReaderAsync();

                    int o_loc = -1, o_val = -1; bool ordReady = false;
                    while (await r.ReadAsync())
                    {
                        if (!ordReady)
                        {
                            o_loc = r.GetOrdinal("locale_id");
                            o_val = r.GetOrdinal("value");
                            ordReady = true;
                        }

                        localized.Add(new LocalizedValue
                        {
                            LocaleId = r.GetString(o_loc),
                            Value = r.IsDBNull(o_val) ? string.Empty : r.GetString(o_val)
                        });
                    }
                }

                await tx.CommitAsync();

                return new BoothCommentModel
                {
                    Id = resultingId,
                    SpaceId = spaceId,
                    UserId = dto.UserId,
                    PredefinedCommentId = dto.PredefinedCommentId,
                    IsActive = resultingActive,
                    CreationTime = resultingCreation,
                    LocalizedPairs = new LocalizedPairs
                    {
                        Key = commentKey!,
                        Values = localized
                    }
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<List<BoothCommentModel>> GetBoothCommentsBySpaceAsync(int spaceId, bool includeInactive = false)
        {
            // 1) Load booth_comment + comment_key in one pass
            const string sqlBase = @"
SELECT
    bc.id,
    bc.space_id,
    bc.user_id,
    bc.predefined_comment_id,
    bc.is_active,
    bc.creation_time,
    pc.comment_key
FROM booth_comment bc
INNER JOIN predefined_comments pc
        ON pc.id = bc.predefined_comment_id
WHERE bc.space_id = @S
{0}
ORDER BY bc.id;";

            var where = includeInactive ? string.Empty : "AND bc.is_active = TRUE";
            var sql = string.Format(sqlBase, where);

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@S", spaceId));

            var rows = new List<(BoothCommentModel Model, string CommentKey)>();
            var keys = new HashSet<string>(StringComparer.Ordinal);

            await using (var r = await cmd.ExecuteReaderAsync())
            {
                bool ordReady = false;
                int o_id = -1, o_sid = -1, o_uid = -1, o_pid = -1, o_act = -1, o_ct = -1, o_ck = -1;

                while (await r.ReadAsync())
                {
                    if (!ordReady)
                    {
                        o_id = r.GetOrdinal("id");
                        o_sid = r.GetOrdinal("space_id");
                        o_uid = r.GetOrdinal("user_id");
                        o_pid = r.GetOrdinal("predefined_comment_id");
                        o_act = r.GetOrdinal("is_active");
                        o_ct = r.GetOrdinal("creation_time");
                        o_ck = r.GetOrdinal("comment_key");
                        ordReady = true;
                    }

                    var ck = r.GetString(o_ck);
                    keys.Add(ck);

                    var m = new BoothCommentModel
                    {
                        Id = r.GetInt32(o_id),
                        SpaceId = r.GetInt32(o_sid),
                        UserId = r.GetString(o_uid),
                        PredefinedCommentId = r.GetInt32(o_pid),
                        IsActive = r.GetBoolean(o_act),
                        CreationTime = r.GetDateTime(o_ct),
                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = ck,
                            Values = new List<LocalizedValue>()
                        }
                    };

                    rows.Add((m, ck));
                }
            }

            if (rows.Count == 0) return new List<BoothCommentModel>();

            // 2) Bulk load all i18n for these keys in this space
            var keyList = keys.ToList();
            var pNames = keyList.Select((_, i) => $"@k{i}").ToList();

            var sqlI18n = $@"
SELECT `key`, locale_id, value
FROM i18n
WHERE space_id = @S
  AND `key` IN ({string.Join(", ", pNames)})
ORDER BY `key`, locale_id;";

            var map = new Dictionary<string, List<LocalizedValue>>(StringComparer.Ordinal);

            await using (var i18nCmd = _db.CreateCommand(conn, sqlI18n))
            {
                i18nCmd.Parameters.Add(_db.CreateParameter("@S", spaceId));
                for (int i = 0; i < keyList.Count; i++)
                    i18nCmd.Parameters.Add(_db.CreateParameter(pNames[i], keyList[i]));

                await using var r2 = await i18nCmd.ExecuteReaderAsync();

                bool ordReady = false;
                int o_k = -1, o_loc = -1, o_val = -1;

                while (await r2.ReadAsync())
                {
                    if (!ordReady)
                    {
                        o_k = r2.GetOrdinal("key");
                        o_loc = r2.GetOrdinal("locale_id");
                        o_val = r2.GetOrdinal("value");
                        ordReady = true;
                    }

                    var k = r2.GetString(o_k);
                    if (!map.TryGetValue(k, out var list))
                    {
                        list = new List<LocalizedValue>();
                        map[k] = list;
                    }

                    list.Add(new LocalizedValue
                    {
                        LocaleId = r2.GetString(o_loc),
                        Value = r2.IsDBNull(o_val) ? string.Empty : r2.GetString(o_val)
                    });
                }
            }

            // 3) Attach localized values
            foreach (var (model, key) in rows)
            {
                if (map.TryGetValue(key, out var vals))
                    model.LocalizedPairs.Values.AddRange(vals);
            }

            return rows.Select(x => x.Model).ToList();
        }

        public async Task<BoothCommentModel?> UpdateBoothCommentAsync(int spaceId, int id, BoothCommentUpdateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            if (!dto.PredefinedCommentId.HasValue && !dto.IsActive.HasValue)
                throw new InvalidOperationException("Nothing to update. Provide predefinedCommentId and/or isActive.");

            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 0) Load current row (scoped to space)
                const string sqlLoad = @"
SELECT bc.id, bc.space_id, bc.user_id, bc.predefined_comment_id, bc.is_active, bc.creation_time, pc.comment_key
FROM booth_comment bc
INNER JOIN predefined_comments pc ON pc.id = bc.predefined_comment_id
WHERE bc.id = @Id AND bc.space_id = @S
LIMIT 1;";

                int currentPid;
                bool currentActive;
                string userId;
                string currentKey;

                await using (var load = _db.CreateCommand(conn, sqlLoad))
                {
                    load.Transaction = tx;
                    load.Parameters.Add(_db.CreateParameter("@Id", id));
                    load.Parameters.Add(_db.CreateParameter("@S", spaceId));

                    await using var r = await load.ExecuteReaderAsync();
                    if (!await r.ReadAsync())
                    {
                        await tx.RollbackAsync();
                        return null; // not found
                    }

                    currentPid = r.GetInt32(r.GetOrdinal("predefined_comment_id"));
                    currentActive = r.GetBoolean(r.GetOrdinal("is_active"));
                    userId = r.GetString(r.GetOrdinal("user_id"));
                    currentKey = r.GetString(r.GetOrdinal("comment_key"));
                }

                var finalPid = dto.PredefinedCommentId ?? currentPid;
                var finalActive = dto.IsActive ?? currentActive;
                var finalKey = currentKey; // will refresh if pid changes

                // 1) If changing predefined_comment_id, validate new target and prevent duplicates
                if (finalPid != currentPid)
                {
                    // a) Validate target predefined comment exists; must be active if finalActive=true
                    const string sqlPc = @"SELECT comment_key, is_active FROM predefined_comments WHERE id = @Pid LIMIT 1;";
                    string? newKey = null;
                    bool targetActive;

                    await using (var pc = _db.CreateCommand(conn, sqlPc))
                    {
                        pc.Transaction = tx;
                        pc.Parameters.Add(_db.CreateParameter("@Pid", finalPid));
                        await using var r = await pc.ExecuteReaderAsync();
                        if (!await r.ReadAsync())
                            throw new InvalidOperationException($"Predefined comment id {finalPid} not found.");

                        newKey = r.GetString(r.GetOrdinal("comment_key"));
                        targetActive = r.GetBoolean(r.GetOrdinal("is_active"));
                    }

                    if (finalActive && !targetActive)
                        throw new InvalidOperationException("Cannot link to an inactive predefined comment while setting is_active = true.");

                    // b) Enforce uniqueness for (space, user, newPid) — avoid duplicate row for same selection
                    const string sqlDup = @"
SELECT id, is_active
FROM booth_comment
WHERE space_id = @S AND user_id = @U AND predefined_comment_id = @Pid AND id <> @Id
LIMIT 1;";

                    await using (var dup = _db.CreateCommand(conn, sqlDup))
                    {
                        dup.Transaction = tx;
                        dup.Parameters.Add(_db.CreateParameter("@S", spaceId));
                        dup.Parameters.Add(_db.CreateParameter("@U", userId));
                        dup.Parameters.Add(_db.CreateParameter("@Pid", finalPid));
                        dup.Parameters.Add(_db.CreateParameter("@Id", id));

                        await using var r = await dup.ExecuteReaderAsync();
                        if (await r.ReadAsync())
                            throw new DuplicateNameException("Another booth_comment already exists for this user and predefined comment in this space.");
                    }

                    // c) Apply change
                    const string sqlUpdPid = @"UPDATE booth_comment SET predefined_comment_id = @Pid WHERE id = @Id;";
                    await using (var up = _db.CreateCommand(conn, sqlUpdPid))
                    {
                        up.Transaction = tx;
                        up.Parameters.Add(_db.CreateParameter("@Pid", finalPid));
                        up.Parameters.Add(_db.CreateParameter("@Id", id));
                        await up.ExecuteNonQueryAsync();
                    }

                    finalKey = newKey!;
                }

                // 2) Update is_active if provided
                if (dto.IsActive.HasValue)
                {
                    const string sqlUpdAct = @"UPDATE booth_comment SET is_active = @A WHERE id = @Id;";
                    await using (var upa = _db.CreateCommand(conn, sqlUpdAct))
                    {
                        upa.Transaction = tx;
                        upa.Parameters.Add(_db.CreateParameter("@A", finalActive));
                        upa.Parameters.Add(_db.CreateParameter("@Id", id));
                        await upa.ExecuteNonQueryAsync();
                    }
                }

                // 3) Reload current basics (in case only is_active changed)
                const string sqlReload = @"
SELECT bc.id, bc.space_id, bc.user_id, bc.predefined_comment_id, bc.is_active, bc.creation_time
FROM booth_comment bc
WHERE bc.id = @Id
LIMIT 1;";
                int resPid;
                bool resActive;
                DateTime resCreation;
                await using (var rel = _db.CreateCommand(conn, sqlReload))
                {
                    rel.Transaction = tx;
                    rel.Parameters.Add(_db.CreateParameter("@Id", id));
                    await using var r = await rel.ExecuteReaderAsync();
                    await r.ReadAsync();
                    resPid = r.GetInt32(r.GetOrdinal("predefined_comment_id"));
                    resActive = r.GetBoolean(r.GetOrdinal("is_active"));
                    resCreation = r.GetDateTime(r.GetOrdinal("creation_time"));
                }

                // 4) Load i18n for the (possibly new) comment key in this space
                var localized = new List<LocalizedValue>();
                const string sqlI18n = @"SELECT locale_id, value FROM i18n WHERE space_id = @S AND `key` = @K ORDER BY locale_id;";
                await using (var i18n = _db.CreateCommand(conn, sqlI18n))
                {
                    i18n.Transaction = tx;
                    i18n.Parameters.Add(_db.CreateParameter("@S", spaceId));
                    i18n.Parameters.Add(_db.CreateParameter("@K", finalKey));
                    await using var r = await i18n.ExecuteReaderAsync();

                    bool ordReady = false;
                    int o_loc = -1, o_val = -1;
                    while (await r.ReadAsync())
                    {
                        if (!ordReady)
                        {
                            o_loc = r.GetOrdinal("locale_id");
                            o_val = r.GetOrdinal("value");
                            ordReady = true;
                        }
                        localized.Add(new LocalizedValue
                        {
                            LocaleId = r.GetString(o_loc),
                            Value = r.IsDBNull(o_val) ? string.Empty : r.GetString(o_val)
                        });
                    }
                }

                await tx.CommitAsync();

                return new BoothCommentModel
                {
                    Id = id,
                    SpaceId = spaceId,
                    UserId = userId,
                    PredefinedCommentId = resPid,
                    IsActive = resActive,
                    CreationTime = resCreation,
                    LocalizedPairs = new LocalizedPairs
                    {
                        Key = finalKey,
                        Values = localized
                    }
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
