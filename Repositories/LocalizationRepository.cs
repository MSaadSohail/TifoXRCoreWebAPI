// <copyright file="LocalizationRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/31/2025</date>
// <summary>Class to handle localization SQL side</summary>

using MySqlConnector;
using System.Data;
//
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public class LocalizationRepository : ILocalizationRepository
    {
        private readonly string _connectionString;

        public LocalizationRepository(IConfiguration cfg)
        {
            _connectionString = cfg.GetConnectionString("DefaultConnection");
        }

        #region GET

        /// <summary>
        /// Gets all i18n keys and their localizations for a given space.
        /// </summary>
        public async Task<List<LocalizedPairs>> GetAllLocalizationsBySpaceAsync(int spaceId)
        {
            const string sql = @"
            SELECT `key`, locale_id, value
            FROM i18n
            WHERE space_id = @SpaceId
            ORDER BY `key`, locale_id;
        ";

            var result = new Dictionary<string, LocalizedPairs>();

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SpaceId", spaceId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string key = reader.GetString("key");
                string localeId = reader.GetString("locale_id");
                string value = reader.IsDBNull("value") ? "" : reader.GetString("value");

                if (!result.TryGetValue(key, out var locPairs) || locPairs == null)
                {
                    locPairs = new LocalizedPairs { Key = key, Values = new List<LocalizedValue>() };
                    result[key] = locPairs;
                }

                locPairs.Values.Add(new LocalizedValue { LocaleId = localeId, Value = value });
            }

            return result.Values.ToList();
        }

        /// <summary>
        /// Gets all localizations for a key in the given space.
        /// </summary>
        public async Task<LocalizedPairs?> GetLocalizationByKeyAsync(int spaceId, string key)
        {
            const string sql = @"
            SELECT locale_id, value
            FROM i18n
            WHERE space_id = @SpaceId AND `key` = @Key
            ORDER BY locale_id;
        ";

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SpaceId", spaceId);
            cmd.Parameters.AddWithValue("@Key", key);

            var values = new List<LocalizedValue>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                values.Add(new LocalizedValue
                {
                    LocaleId = reader.GetString("locale_id"),
                    Value = reader.IsDBNull("value") ? null : reader.GetString("value")
                });
            }

            return values.Count == 0 ? null : new LocalizedPairs { Key = key, Values = values };
        }

        #endregion

        #region POST
        /// <summary>
        /// Inserts a new localization key with values for all given locales (bulk insert).
        /// </summary>
        public async Task<LocalizedPairs> CreateLocalizationAsync(int spaceId, LocalizedPairs dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Key) || dto.Values == null || dto.Values.Count == 0)
                throw new ArgumentException("Invalid localization data");

            const string insertSql = @"
            INSERT INTO i18n (`key`, locale_id, value, space_id)
            VALUES (@Key, @LocaleId, @Value, @SpaceId);
        ";

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                foreach (var val in dto.Values)
                {
                    await using var cmd = new MySqlCommand(insertSql, conn, tx);
                    cmd.Parameters.AddWithValue("@Key", dto.Key);
                    cmd.Parameters.AddWithValue("@LocaleId", val.LocaleId);
                    cmd.Parameters.AddWithValue("@Value", val.Value);
                    cmd.Parameters.AddWithValue("@SpaceId", spaceId);
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

        /// <summary>
        /// Updates all localizations for a key. Upserts all values (updates if exists, inserts if not).
        /// </summary>
        public async Task<LocalizedPairs?> UpdateLocalizationAsync(int spaceId, string key, LocalizedPairs dto)
        {
            if (dto == null || string.IsNullOrEmpty(key) || dto.Values == null || dto.Values.Count == 0)
                return null;

            // We'll use upsert pattern (update or insert)
            const string updateSql = @"
            UPDATE i18n
            SET value = @Value
            WHERE `key` = @Key AND locale_id = @LocaleId AND space_id = @SpaceId;
        ";
            const string insertSql = @"
            INSERT INTO i18n (`key`, locale_id, value, space_id)
            VALUES (@Key, @LocaleId, @Value, @SpaceId);
        ";

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                foreach (var val in dto.Values)
                {
                    await using var updateCmd = new MySqlCommand(updateSql, conn, tx);
                    updateCmd.Parameters.AddWithValue("@Key", key);
                    updateCmd.Parameters.AddWithValue("@LocaleId", val.LocaleId);
                    updateCmd.Parameters.AddWithValue("@Value", val.Value);
                    updateCmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    int affected = await updateCmd.ExecuteNonQueryAsync();
                    if (affected == 0)
                    {
                        await using var insertCmd = new MySqlCommand(insertSql, conn, tx);
                        insertCmd.Parameters.AddWithValue("@Key", key);
                        insertCmd.Parameters.AddWithValue("@LocaleId", val.LocaleId);
                        insertCmd.Parameters.AddWithValue("@Value", val.Value);
                        insertCmd.Parameters.AddWithValue("@SpaceId", spaceId);
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

        /// <summary>
        /// Deletes all localizations for a key in the space.
        /// </summary>
        public async Task<bool> DeleteLocalizationAsync(int spaceId, string key)
        {
            const string sql = @"
            DELETE FROM i18n WHERE space_id = @SpaceId AND `key` = @Key;
        ";
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SpaceId", spaceId);
            cmd.Parameters.AddWithValue("@Key", key);
            var affected = await cmd.ExecuteNonQueryAsync();
            return affected > 0;
        }

        #endregion
    }
}