// <copyright file="LocalizationRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/31/2025</date>
// <summary>Class to handle localization SQL side</summary>

using System.Data.Common;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public sealed class LocalizationRepository : ILocalizationRepository
    {
        private readonly IDbProvider _db;

        public LocalizationRepository(IConfiguration cfg, IDbProvider db)
        {
            _db = db;
        }

        #region GET

        public async Task<List<LocalizedPairs>> GetAllLocalizationsBySpaceAsync(int spaceId)
        {
            const string sql = @"
SELECT
    [key] AS i18n_key,
    locale_id,
    value
FROM i18n
WHERE space_id = @SpaceId
ORDER BY i18n_key, locale_id;";



            var result = new Dictionary<string, LocalizedPairs>();

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                // Read aliased column to avoid reserved-word headaches
                string key = reader.GetString(reader.GetOrdinal("i18n_key"));
                string localeId = reader.GetString(reader.GetOrdinal("locale_id"));
                string value = reader.IsDBNull(reader.GetOrdinal("value")) ? "" : reader.GetString(reader.GetOrdinal("value"));

                if (!result.TryGetValue(key, out var locPairs) || locPairs == null)
                {
                    locPairs = new LocalizedPairs { Key = key, Values = new List<LocalizedValue>() };
                    result[key] = locPairs;
                }

                locPairs.Values.Add(new LocalizedValue { LocaleId = localeId, Value = value });
            }

            return result.Values.ToList();
        }

        public async Task<LocalizedPairs?> GetLocalizationByKeyAsync(int spaceId, string key)
        {
            const string sql = @"
SELECT
    locale_id,
    value
FROM i18n
WHERE space_id = @SpaceId AND [key] = @Key
ORDER BY locale_id;";


            // Provider-safe version (recommended):
            // WHERE space_id = @SpaceId AND key = @Key

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@Key", key));

            var values = new List<LocalizedValue>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                values.Add(new LocalizedValue
                {
                    LocaleId = reader.GetString(reader.GetOrdinal("locale_id")),
                    Value = reader.IsDBNull(reader.GetOrdinal("value")) ? null : reader.GetString(reader.GetOrdinal("value"))
                });
            }

            return values.Count == 0 ? null : new LocalizedPairs { Key = key, Values = values };
        }

        #endregion

        #region POST

        public async Task<LocalizedPairs> CreateLocalizationAsync(int spaceId, LocalizedPairs dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Key) || dto.Values == null || dto.Values.Count == 0)
                throw new ArgumentException("Invalid localization data");

            const string insertSql = @"
INSERT INTO i18n ([key], locale_id, value, space_id)
VALUES (@Key, @LocaleId, @Value, @SpaceId);";


            // Provider-safe version (recommended):
            // INSERT INTO i18n (key, locale_id, value, space_id) ...

            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                foreach (var val in dto.Values)
                {
                    await using var cmd = _db.CreateCommand(conn, insertSql, tx);
                    cmd.Parameters.Add(_db.CreateParameter("@Key", dto.Key));
                    cmd.Parameters.Add(_db.CreateParameter("@LocaleId", val.LocaleId));
                    cmd.Parameters.Add(_db.CreateParameter("@Value", (object?)val.Value ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return dto;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region PUT

        public async Task<LocalizedPairs?> UpdateLocalizationAsync(int spaceId, string key, LocalizedPairs dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(key) || dto.Values == null || dto.Values.Count == 0)
                return null;

            const string updateSql = @"
UPDATE i18n
   SET value = @Value
 WHERE [key] = @Key AND locale_id = @LocaleId AND space_id = @SpaceId;";

            const string insertSql = @"
INSERT INTO i18n ([key], locale_id, value, space_id)
VALUES (@Key, @LocaleId, @Value, @SpaceId);";


            // Provider-safe version (recommended):
            // WHERE key = @Key ...

            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                foreach (var val in dto.Values)
                {
                    await using var updateCmd = _db.CreateCommand(conn, updateSql, tx);
                    updateCmd.Parameters.Add(_db.CreateParameter("@Key", key));
                    updateCmd.Parameters.Add(_db.CreateParameter("@LocaleId", val.LocaleId));
                    updateCmd.Parameters.Add(_db.CreateParameter("@Value", (object?)val.Value ?? DBNull.Value));
                    updateCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                    int affected = await updateCmd.ExecuteNonQueryAsync();

                    if (affected == 0)
                    {
                        await using var insertCmd = _db.CreateCommand(conn, insertSql, tx);
                        insertCmd.Parameters.Add(_db.CreateParameter("@Key", key));
                        insertCmd.Parameters.Add(_db.CreateParameter("@LocaleId", val.LocaleId));
                        insertCmd.Parameters.Add(_db.CreateParameter("@Value", (object?)val.Value ?? DBNull.Value));
                        insertCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        await insertCmd.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();
                return dto;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region DELETE

        public async Task<bool> DeleteLocalizationAsync(int spaceId, string key)
        {
            const string sql = @"
DELETE FROM i18n
WHERE space_id = @SpaceId AND [key] = @Key;";


            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@Key", key));

            var affected = await cmd.ExecuteNonQueryAsync();
            return affected > 0;
        }

        #endregion
    }
}
