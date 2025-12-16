// <copyright file="EventRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Class to handle event SQL side</summary>

using System.Data.Common;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public sealed class EventRepository : IEventRepository
    {
        private readonly IDbProvider _db;

        public EventRepository(IConfiguration configuration, IDbProvider db)
        {
            _db = db;
        }

        public async Task<EventData?> GetEventByIdAsync(int eventId)
        {
            // SQL Server-safe: [event], [key]
            const string sql = @"
SELECT
    e.id,
    e.platform_type_id,
    e.event_type_id,
    e.personality_id,
    e.localized_name_id,
    e.event_url,
    e.is_live,
    e.subtitle_enabled,
    e.scheduled_length,
    e.description_id,
    e.scheduled_start_datetime,
    e.scheduled_end_datetime,
    e.actual_start_datetime,
    e.actual_end_datetime,
    e.rating,
    ln.locale_id AS ln_locale_id,
    ln.value     AS ln_value,
    d.value      AS desc_value
FROM [event] AS e
LEFT JOIN i18n AS ln ON ln.[key] = e.localized_name_id
LEFT JOIN i18n AS d  ON d.[key]  = e.description_id AND d.locale_id = ln.locale_id
WHERE e.id = @EventId
ORDER BY ln.locale_id;";

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@EventId", eventId));

            await using var reader = await cmd.ExecuteReaderAsync();

            EventData? evt = null;
            var nameValues = new List<LocalizedValue>();
            var descValues = new List<LocalizedValue>();
            string? nameKey = null;
            string? descKey = null;

            bool ordReady = false;
            int o_id = -1, o_platform_type_id = -1, o_event_type_id = -1, o_personality_id = -1, o_name_key = -1,
                o_event_url = -1, o_is_live = -1, o_subtitle_enabled = -1, o_scheduled_length = -1, o_desc_key = -1,
                o_sched_start = -1, o_sched_end = -1, o_actual_start = -1, o_actual_end = -1, o_rating = -1,
                o_ln_locale_id = -1, o_ln_value = -1, o_desc_value = -1;

            while (await reader.ReadAsync())
            {
                if (!ordReady)
                {
                    o_id = reader.GetOrdinal("id");
                    o_platform_type_id = reader.GetOrdinal("platform_type_id");
                    o_event_type_id = reader.GetOrdinal("event_type_id");
                    o_personality_id = reader.GetOrdinal("personality_id");
                    o_name_key = reader.GetOrdinal("localized_name_id");
                    o_event_url = reader.GetOrdinal("event_url");
                    o_is_live = reader.GetOrdinal("is_live");
                    o_subtitle_enabled = reader.GetOrdinal("subtitle_enabled");
                    o_scheduled_length = reader.GetOrdinal("scheduled_length");
                    o_desc_key = reader.GetOrdinal("description_id");
                    o_sched_start = reader.GetOrdinal("scheduled_start_datetime");
                    o_sched_end = reader.GetOrdinal("scheduled_end_datetime");
                    o_actual_start = reader.GetOrdinal("actual_start_datetime");
                    o_actual_end = reader.GetOrdinal("actual_end_datetime");
                    o_rating = reader.GetOrdinal("rating");
                    o_ln_locale_id = reader.GetOrdinal("ln_locale_id");
                    o_ln_value = reader.GetOrdinal("ln_value");
                    o_desc_value = reader.GetOrdinal("desc_value");
                    ordReady = true;
                }

                if (evt == null)
                {
                    nameKey = reader.IsDBNull(o_name_key) ? null : reader.GetString(o_name_key);
                    descKey = reader.IsDBNull(o_desc_key) ? null : reader.GetString(o_desc_key);

                    evt = new EventData
                    {
                        Id = reader.GetInt32(o_id),
                        PlatformTypeId = reader.GetInt32(o_platform_type_id),
                        EventTypeId = reader.GetInt32(o_event_type_id),
                        PersonalityId = reader.IsDBNull(o_personality_id) ? null : reader.GetInt32(o_personality_id),
                        EventUrl = reader.IsDBNull(o_event_url) ? null : reader.GetString(o_event_url),
                        IsLive = reader.GetBoolean(o_is_live),
                        SubtitleEnabled = reader.GetBoolean(o_subtitle_enabled),
                        ScheduledLength = reader.GetInt32(o_scheduled_length),
                        ScheduledStart = reader.GetDateTime(o_sched_start),
                        ScheduledEnd = reader.GetDateTime(o_sched_end),
                        ActualStart = reader.IsDBNull(o_actual_start) ? null : reader.GetDateTime(o_actual_start),
                        ActualEnd = reader.IsDBNull(o_actual_end) ? null : reader.GetDateTime(o_actual_end),
                        Rating = reader.IsDBNull(o_rating) ? null : reader.GetDecimal(o_rating)
                    };
                }

                if (!reader.IsDBNull(o_ln_locale_id))
                {
                    var localeId = reader.GetString(o_ln_locale_id);

                    if (!reader.IsDBNull(o_ln_value))
                    {
                        nameValues.Add(new LocalizedValue
                        {
                            LocaleId = localeId,
                            Value = reader.GetString(o_ln_value)
                        });
                    }

                    if (!reader.IsDBNull(o_desc_value))
                    {
                        descValues.Add(new LocalizedValue
                        {
                            LocaleId = localeId,
                            Value = reader.GetString(o_desc_value)
                        });
                    }
                }
            }

            if (evt == null) return null;

            evt.LocalizedPairs = new LocalizedPairs { Key = nameKey, Values = nameValues };
            evt.LocalizedDescription = new LocalizedPairs { Key = descKey, Values = descValues };

            return evt;
        }

        public async Task<EventData> CreateEventAsync(Event eventDto)
        {
            ArgumentNullException.ThrowIfNull(eventDto);

            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Insert into [event] table (SQL Server)
                const string insertEventSql = @"
INSERT INTO [event]
    (platform_type_id, event_type_id, personality_id,
     name_key, event_url, is_live, subtitle_enabled,
     scheduled_length, description_key,
     scheduled_start_datetime, scheduled_end_datetime,
     actual_start_datetime, actual_end_datetime, rating)
VALUES
    (@PlatformTypeId, @EventTypeId, @PersonalityId,
     @NameKey, @EventUrl, @IsLive, @SubtitleEnabled,
     @ScheduledLength, @DescKey,
     @ScheduledStart, @ScheduledEnd,
     @ActualStart, @ActualEnd, @Rating);

SELECT CAST(SCOPE_IDENTITY() AS int);";

                int newId;
                await using (var cmd = _db.CreateCommand(conn, insertEventSql, tx))
                {
                    cmd.Parameters.Add(_db.CreateParameter("@PlatformTypeId", eventDto.PlatformTypeId));
                    cmd.Parameters.Add(_db.CreateParameter("@EventTypeId", eventDto.EventTypeId));
                    cmd.Parameters.Add(_db.CreateParameter("@PersonalityId", (object?)eventDto.PersonalityId ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@NameKey", eventDto.LocalizedPairs?.Key ?? throw new ArgumentException("LocalizedPairs.Key required")));
                    cmd.Parameters.Add(_db.CreateParameter("@EventUrl", (object?)eventDto.EventUrl ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@IsLive", eventDto.IsLive));
                    cmd.Parameters.Add(_db.CreateParameter("@SubtitleEnabled", eventDto.SubtitleEnabled));
                    cmd.Parameters.Add(_db.CreateParameter("@ScheduledLength", eventDto.ScheduledLength));
                    cmd.Parameters.Add(_db.CreateParameter("@DescKey", eventDto.LocalizedDescription?.Key ?? throw new ArgumentException("LocalizedDescription.Key required")));
                    cmd.Parameters.Add(_db.CreateParameter("@ScheduledStart", eventDto.ScheduledStart));
                    cmd.Parameters.Add(_db.CreateParameter("@ScheduledEnd", eventDto.ScheduledEnd));
                    cmd.Parameters.Add(_db.CreateParameter("@ActualStart", (object?)eventDto.ActualStart ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@ActualEnd", (object?)eventDto.ActualEnd ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@Rating", (object?)eventDto.Rating ?? DBNull.Value));

                    newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // 2) Insert i18n name + description (space-scoped)
                const string insertI18nSql = @"
INSERT INTO i18n ([key], locale_id, value, space_id)
VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                var spaceId = eventDto.SpaceId;

                // Name i18n
                if (eventDto.LocalizedPairs?.Values != null)
                {
                    foreach (var loc in eventDto.LocalizedPairs.Values)
                    {
                        await using var i18nCmd = _db.CreateCommand(conn, insertI18nSql, tx);
                        i18nCmd.Parameters.Add(_db.CreateParameter("@Key", eventDto.LocalizedPairs.Key));
                        i18nCmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        i18nCmd.Parameters.Add(_db.CreateParameter("@Value", (object?)loc.Value ?? DBNull.Value));
                        i18nCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        await i18nCmd.ExecuteNonQueryAsync();
                    }
                }

                // Description i18n
                if (eventDto.LocalizedDescription?.Values != null)
                {
                    foreach (var loc in eventDto.LocalizedDescription.Values)
                    {
                        await using var i18nCmd = _db.CreateCommand(conn, insertI18nSql, tx);
                        i18nCmd.Parameters.Add(_db.CreateParameter("@Key", eventDto.LocalizedDescription.Key));
                        i18nCmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        i18nCmd.Parameters.Add(_db.CreateParameter("@Value", (object?)loc.Value ?? DBNull.Value));
                        i18nCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        await i18nCmd.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();

                // 3) Load & return
                return (await GetEventByIdAsync(newId))!;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<EventData> UpdateEventAsync(int eventId, Event dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                int spaceId = dto.SpaceId;

                const string updateSql = @"
UPDATE [event]
SET platform_type_id = @PlatformTypeId,
    event_type_id = @EventTypeId,
    personality_id = @PersonalityId,
    name_key = @LocalizedNameKey,
    event_url = @EventUrl,
    is_live = @IsLive,
    subtitle_enabled = @SubtitleEnabled,
    scheduled_length = @ScheduledLength,
    description_key = @DescriptionKey,
    scheduled_start_datetime = @ScheduledStart,
    scheduled_end_datetime = @ScheduledEnd,
    actual_start_datetime = @ActualStart,
    actual_end_datetime = @ActualEnd,
    rating = @Rating
WHERE id = @EventId;";

                await using (var updateCmd = _db.CreateCommand(conn, updateSql, tx))
                {
                    updateCmd.Parameters.Add(_db.CreateParameter("@PlatformTypeId", dto.PlatformTypeId));
                    updateCmd.Parameters.Add(_db.CreateParameter("@EventTypeId", dto.EventTypeId));
                    updateCmd.Parameters.Add(_db.CreateParameter("@PersonalityId", (object?)dto.PersonalityId ?? DBNull.Value));
                    updateCmd.Parameters.Add(_db.CreateParameter("@LocalizedNameKey", dto.LocalizedPairs?.Key ?? throw new ArgumentException("LocalizedPairs.Key required")));
                    updateCmd.Parameters.Add(_db.CreateParameter("@EventUrl", (object?)dto.EventUrl ?? DBNull.Value));
                    updateCmd.Parameters.Add(_db.CreateParameter("@IsLive", dto.IsLive));
                    updateCmd.Parameters.Add(_db.CreateParameter("@SubtitleEnabled", dto.SubtitleEnabled));
                    updateCmd.Parameters.Add(_db.CreateParameter("@ScheduledLength", dto.ScheduledLength));
                    updateCmd.Parameters.Add(_db.CreateParameter("@DescriptionKey", dto.LocalizedDescription?.Key ?? throw new ArgumentException("LocalizedDescription.Key required")));
                    updateCmd.Parameters.Add(_db.CreateParameter("@ScheduledStart", dto.ScheduledStart));
                    updateCmd.Parameters.Add(_db.CreateParameter("@ScheduledEnd", dto.ScheduledEnd));
                    updateCmd.Parameters.Add(_db.CreateParameter("@ActualStart", (object?)dto.ActualStart ?? DBNull.Value));
                    updateCmd.Parameters.Add(_db.CreateParameter("@ActualEnd", (object?)dto.ActualEnd ?? DBNull.Value));
                    updateCmd.Parameters.Add(_db.CreateParameter("@Rating", (object?)dto.Rating ?? DBNull.Value));
                    updateCmd.Parameters.Add(_db.CreateParameter("@EventId", eventId));

                    await updateCmd.ExecuteNonQueryAsync();
                }

                // i18n upsert (SQL Server-safe: [key])
                const string updI18n = @"
UPDATE i18n
   SET value = @Value
 WHERE [key] = @Key AND locale_id = @LocaleId AND space_id = @SpaceId;";

                const string insI18n = @"
INSERT INTO i18n ([key], locale_id, value, space_id)
VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                // Name i18n
                if (dto.LocalizedPairs?.Values != null)
                {
                    foreach (var loc in dto.LocalizedPairs.Values)
                    {
                        await using var uCmd = _db.CreateCommand(conn, updI18n, tx);
                        uCmd.Parameters.Add(_db.CreateParameter("@Key", dto.LocalizedPairs.Key));
                        uCmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        uCmd.Parameters.Add(_db.CreateParameter("@Value", (object?)loc.Value ?? DBNull.Value));
                        uCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                        if (await uCmd.ExecuteNonQueryAsync() == 0)
                        {
                            await using var iCmd = _db.CreateCommand(conn, insI18n, tx);
                            iCmd.Parameters.Add(_db.CreateParameter("@Key", dto.LocalizedPairs.Key));
                            iCmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                            iCmd.Parameters.Add(_db.CreateParameter("@Value", (object?)loc.Value ?? DBNull.Value));
                            iCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                            await iCmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                // Description i18n
                if (dto.LocalizedDescription?.Values != null)
                {
                    foreach (var loc in dto.LocalizedDescription.Values)
                    {
                        await using var uCmd = _db.CreateCommand(conn, updI18n, tx);
                        uCmd.Parameters.Add(_db.CreateParameter("@Key", dto.LocalizedDescription.Key));
                        uCmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        uCmd.Parameters.Add(_db.CreateParameter("@Value", (object?)loc.Value ?? DBNull.Value));
                        uCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                        if (await uCmd.ExecuteNonQueryAsync() == 0)
                        {
                            await using var iCmd = _db.CreateCommand(conn, insI18n, tx);
                            iCmd.Parameters.Add(_db.CreateParameter("@Key", dto.LocalizedDescription.Key));
                            iCmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                            iCmd.Parameters.Add(_db.CreateParameter("@Value", (object?)loc.Value ?? DBNull.Value));
                            iCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                            await iCmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                await tx.CommitAsync();
                return (await GetEventByIdAsync(eventId))!;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
