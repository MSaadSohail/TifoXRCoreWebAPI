using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using TifoXRWebApi.Models.Common;
using TifoXRWebApi.Models;
using System.Data;

[Route("api/[controller]")]
[ApiController]
public class EventController : ControllerBase
{
    private readonly string _connectionString;

    public EventController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    /// <summary>
    /// GET /api/event/{event_id}
    /// Fetches all event fields except creation_time, modified_time, modified_by,
    /// plus localized name and description.
    /// </summary>
    [HttpGet("{event_id}")]
    public async Task<ActionResult<EventData>> GetEventDataByID(
    [FromRoute(Name = "event_id")] int eventId
)
    {
        const string sql = @"
        SELECT
            e.id,
            e.platform_type_id,
            e.event_type_id,
            e.personality_id,
            e.name_key,
            e.event_url,
            e.is_live,
            e.subtitle_enabled,
            e.scheduled_length,
            e.description_key,
            e.scheduled_start_datetime,
            e.scheduled_end_datetime,
            e.actual_start_datetime,
            e.actual_end_datetime,
            e.rating,
            ln.locale_id       AS ln_locale_id,
            ln.value           AS ln_value,
            d.value            AS desc_value
        FROM `event` AS e
        LEFT JOIN i18n AS ln
            ON ln.`key` = e.name_key
        LEFT JOIN i18n AS d
            ON d.`key` = e.description_key AND d.locale_id = ln.locale_id
        WHERE e.id = @EventId
        ORDER BY ln.locale_id;
    ";

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@EventId", eventId);

        await using var reader = await cmd.ExecuteReaderAsync();
        EventData evt = null;
        var nameValues = new List<LocalizedValue>();
        var descValues = new List<LocalizedValue>();
        string nameKey = null;
        string descKey = null;

        while (await reader.ReadAsync())
        {
            if (evt == null)
            {
                nameKey = reader.GetString("name_key");
                descKey = reader.GetString("description_key");

                evt = new EventData
                {
                    Id = reader.GetInt32("id"),
                    PlatformTypeId = reader.GetInt32("platform_type_id"),
                    EventTypeId = reader.GetInt32("event_type_id"),
                    PersonalityId = reader.IsDBNull("personality_id") ? null : reader.GetInt32("personality_id"),
                    EventUrl = reader.IsDBNull("event_url") ? null : reader.GetString("event_url"),
                    IsLive = reader.GetBoolean("is_live"),
                    SubtitleEnabled = reader.GetBoolean("subtitle_enabled"),
                    ScheduledLength = reader.GetInt32("scheduled_length"),
                    ScheduledStart = reader.GetDateTime("scheduled_start_datetime"),
                    ScheduledEnd = reader.GetDateTime("scheduled_end_datetime"),
                    ActualStart = reader.IsDBNull("actual_start_datetime") ? null : reader.GetDateTime("actual_start_datetime"),
                    ActualEnd = reader.IsDBNull("actual_end_datetime") ? null : reader.GetDateTime("actual_end_datetime"),
                    Rating = reader.IsDBNull("rating") ? null : reader.GetDecimal("rating")
                };
            }

            var localeId = reader.GetString("ln_locale_id");

            if (!reader.IsDBNull("ln_value"))
            {
                nameValues.Add(new LocalizedValue
                {
                    LocaleId = localeId,
                    Value = reader.GetString("ln_value")
                });
            }

            if (!reader.IsDBNull("desc_value"))
            {
                descValues.Add(new LocalizedValue
                {
                    LocaleId = localeId,
                    Value = reader.GetString("desc_value")
                });
            }
        }

        if (evt == null)
            return NotFound();

        evt.LocalizedName = new LocalizedName
        {
            Key = nameKey,
            Values = nameValues
        };

        evt.LocalizedDescription = new LocalizedName
        {
            Key = descKey,
            Values = descValues
        };

        return Ok(evt);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EventData), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EventData>> CreateEvent([FromBody] Event eventDto)
    {
        if (eventDto == null)
            return BadRequest();

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            // 1) Insert into event table
            const string insertEventSql = @"
            INSERT INTO `event`
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
            SELECT LAST_INSERT_ID();
        ";

            int newId;
            await using (var cmd = new MySqlCommand(insertEventSql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@PlatformTypeId", eventDto.PlatformTypeId);
                cmd.Parameters.AddWithValue("@EventTypeId", eventDto.EventTypeId);
                cmd.Parameters.AddWithValue("@PersonalityId", (object?)eventDto.PersonalityId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@NameKey", eventDto.LocalizedName.Key);
                cmd.Parameters.AddWithValue("@EventUrl", (object?)eventDto.EventUrl ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsLive", eventDto.IsLive);
                cmd.Parameters.AddWithValue("@SubtitleEnabled", eventDto.SubtitleEnabled);
                cmd.Parameters.AddWithValue("@ScheduledLength", eventDto.ScheduledLength);
                cmd.Parameters.AddWithValue("@DescKey", eventDto.LocalizedDescription.Key);
                cmd.Parameters.AddWithValue("@ScheduledStart", eventDto.ScheduledStart);
                cmd.Parameters.AddWithValue("@ScheduledEnd", eventDto.ScheduledEnd);
                cmd.Parameters.AddWithValue("@ActualStart", (object?)eventDto.ActualStart ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ActualEnd", (object?)eventDto.ActualEnd ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Rating", (object?)eventDto.Rating ?? DBNull.Value);

                newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // 2) Insert localized name
            const string insertI18nSql = @"
            INSERT INTO i18n (`key`, locale_id, value, space_id)
            VALUES (@Key, @LocaleId, @Value, @SpaceId);
        ";

            foreach (var loc in eventDto.LocalizedName.Values)
            {
                await using var cmd = new MySqlCommand(insertI18nSql, conn, tx);
                cmd.Parameters.AddWithValue("@Key", eventDto.LocalizedName.Key);
                cmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                cmd.Parameters.AddWithValue("@Value", loc.Value);
                cmd.Parameters.AddWithValue("@SpaceId", eventDto.SpaceId);
                await cmd.ExecuteNonQueryAsync();
            }

            // 3) Insert localized description
            foreach (var loc in eventDto.LocalizedDescription.Values)
            {
                await using var cmd = new MySqlCommand(insertI18nSql, conn, tx);
                cmd.Parameters.AddWithValue("@Key", eventDto.LocalizedDescription.Key);
                cmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                cmd.Parameters.AddWithValue("@Value", loc.Value);
                cmd.Parameters.AddWithValue("@SpaceId", eventDto.SpaceId);
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();

            // 4) Return the created event
            var created = await LoadEventById(conn, newId);
            return CreatedAtAction(nameof(GetEventDataByID), new { event_id = newId }, created);
        }
        catch (MySqlException ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { error = ex.Message });
        }
    }


    // Assume this GET exists for CreatedAtAction
    [HttpGet("{event_id}")]

    // Helper to SELECT an event + translations
    private static async Task<EventData> LoadEventById(MySqlConnection conn, int eventId)
    {
        const string sql = @"
        SELECT
            e.id,
            e.platform_type_id,
            e.event_type_id,
            e.personality_id,
            e.name_key,
            e.event_url,
            e.is_live,
            e.subtitle_enabled,
            e.scheduled_length,
            e.description_key,
            e.scheduled_start_datetime,
            e.scheduled_end_datetime,
            e.actual_start_datetime,
            e.actual_end_datetime,
            e.rating,
            ln.locale_id AS ln_locale_id,
            ln.value AS ln_value,
            d.locale_id AS desc_locale_id,
            d.value AS desc_value
        FROM `event` AS e
        LEFT JOIN i18n AS ln ON ln.`key` = e.name_key
        LEFT JOIN i18n AS d ON d.`key` = e.description_key AND d.locale_id = ln.locale_id
        WHERE e.id = @EventId
        ORDER BY ln.locale_id;
    ";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@EventId", eventId);

        await using var reader = await cmd.ExecuteReaderAsync();

        EventData evt = null;
        List<LocalizedValue> nameTranslations = new();
        List<LocalizedValue> descriptionTranslations = new();
        string nameKey = null;
        string descKey = null;

        while (await reader.ReadAsync())
        {
            if (evt == null)
            {
                nameKey = reader.GetString("name_key");
                descKey = reader.GetString("description_key");

                evt = new EventData
                {
                    Id = reader.GetInt32("id"),
                    PlatformTypeId = reader.GetInt32("platform_type_id"),
                    EventTypeId = reader.GetInt32("event_type_id"),
                    PersonalityId = reader.IsDBNull(reader.GetOrdinal("personality_id")) ? null : reader.GetInt32("personality_id"),
                    EventUrl = reader.IsDBNull(reader.GetOrdinal("event_url")) ? null : reader.GetString("event_url"),
                    IsLive = reader.GetBoolean("is_live"),
                    SubtitleEnabled = reader.GetBoolean("subtitle_enabled"),
                    ScheduledLength = reader.GetInt32("scheduled_length"),
                    ScheduledStart = reader.GetDateTime("scheduled_start_datetime"),
                    ScheduledEnd = reader.GetDateTime("scheduled_end_datetime"),
                    ActualStart = reader.IsDBNull(reader.GetOrdinal("actual_start_datetime")) ? null : reader.GetDateTime("actual_start_datetime"),
                    ActualEnd = reader.IsDBNull(reader.GetOrdinal("actual_end_datetime")) ? null : reader.GetDateTime("actual_end_datetime"),
                    Rating = reader.IsDBNull(reader.GetOrdinal("rating")) ? null : reader.GetDecimal("rating")
                };
            }

            // Name translation
            if (!reader.IsDBNull(reader.GetOrdinal("ln_locale_id")))
            {
                nameTranslations.Add(new LocalizedValue
                {
                    LocaleId = reader.GetString("ln_locale_id"),
                    Value = reader.IsDBNull(reader.GetOrdinal("ln_value")) ? null : reader.GetString("ln_value")
                });
            }

            // Description translation
            if (!reader.IsDBNull(reader.GetOrdinal("desc_locale_id")))
            {
                descriptionTranslations.Add(new LocalizedValue
                {
                    LocaleId = reader.GetString("desc_locale_id"),
                    Value = reader.IsDBNull(reader.GetOrdinal("desc_value")) ? null : reader.GetString("desc_value")
                });
            }
        }

        if (evt != null)
        {
            evt.LocalizedName = new LocalizedName
            {
                Key = nameKey,
                Values = nameTranslations
            };

            evt.LocalizedDescription = new LocalizedName
            {
                Key = descKey,
                Values = descriptionTranslations
            };
        }

        return evt;
    }


    //Update API
    [HttpPut("/api/event/{eventId}")]
    [ProducesResponseType(typeof(EventData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EventData>> UpdateEvent(
    [FromRoute] int eventId,
    [FromBody] Event dto
)
    {
        if (dto == null) return BadRequest();

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            int spaceId = dto.SpaceId;

            // 1) Update event row
            const string updateSql = @"
            UPDATE `event`
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
            WHERE id = @EventId;
        ";

            await using (var updateCmd = new MySqlCommand(updateSql, conn, tx))
            {
                updateCmd.Parameters.AddWithValue("@PlatformTypeId", dto.PlatformTypeId);
                updateCmd.Parameters.AddWithValue("@EventTypeId", dto.EventTypeId);
                updateCmd.Parameters.AddWithValue("@PersonalityId", (object?)dto.PersonalityId ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@LocalizedNameKey", dto.LocalizedName.Key);
                updateCmd.Parameters.AddWithValue("@EventUrl", (object?)dto.EventUrl ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@IsLive", dto.IsLive);
                updateCmd.Parameters.AddWithValue("@SubtitleEnabled", dto.SubtitleEnabled);
                updateCmd.Parameters.AddWithValue("@ScheduledLength", dto.ScheduledLength);
                updateCmd.Parameters.AddWithValue("@DescriptionKey", dto.LocalizedDescription.Key);
                updateCmd.Parameters.AddWithValue("@ScheduledStart", dto.ScheduledStart);
                updateCmd.Parameters.AddWithValue("@ScheduledEnd", dto.ScheduledEnd);
                updateCmd.Parameters.AddWithValue("@ActualStart", (object?)dto.ActualStart ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@ActualEnd", (object?)dto.ActualEnd ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@Rating", (object?)dto.Rating ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@EventId", eventId);

                await updateCmd.ExecuteNonQueryAsync();
            }

            // 2) Upsert i18n values
            const string updI18n = @"
            UPDATE i18n SET value = @Value
            WHERE `key` = @Key AND locale_id = @LocaleId AND space_id = @SpaceId;
        ";

            const string insI18n = @"
            INSERT INTO i18n (`key`, locale_id, value, space_id)
            VALUES (@Key, @LocaleId, @Value, @SpaceId);
        ";

            // Update name i18n
            foreach (var loc in dto.LocalizedName.Values)
            {
                await using var uCmd = new MySqlCommand(updI18n, conn, tx);
                uCmd.Parameters.AddWithValue("@Key", dto.LocalizedName.Key);
                uCmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                uCmd.Parameters.AddWithValue("@Value", loc.Value);
                uCmd.Parameters.AddWithValue("@SpaceId", spaceId);

                if (await uCmd.ExecuteNonQueryAsync() == 0)
                {
                    await using var iCmd = new MySqlCommand(insI18n, conn, tx);
                    iCmd.Parameters.AddWithValue("@Key", dto.LocalizedName.Key);
                    iCmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                    iCmd.Parameters.AddWithValue("@Value", loc.Value);
                    iCmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    await iCmd.ExecuteNonQueryAsync();
                }
            }

            // Update description i18n
            foreach (var loc in dto.LocalizedDescription.Values)
            {
                await using var uCmd = new MySqlCommand(updI18n, conn, tx);
                uCmd.Parameters.AddWithValue("@Key", dto.LocalizedDescription.Key);
                uCmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                uCmd.Parameters.AddWithValue("@Value", loc.Value);
                uCmd.Parameters.AddWithValue("@SpaceId", spaceId);

                if (await uCmd.ExecuteNonQueryAsync() == 0)
                {
                    await using var iCmd = new MySqlCommand(insI18n, conn, tx);
                    iCmd.Parameters.AddWithValue("@Key", dto.LocalizedDescription.Key);
                    iCmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                    iCmd.Parameters.AddWithValue("@Value", loc.Value);
                    iCmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    await iCmd.ExecuteNonQueryAsync();
                }
            }

            await tx.CommitAsync();

            var updated = await LoadEventById(conn, eventId);
            return Ok(updated);
        }
        catch (MySqlException ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { error = ex.Message });
        }
    }



}


