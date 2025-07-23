using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using TifoXRCoreWebAPI.Models;
using TifoXRCoreWebAPI.Models.Common;

namespace TifoXRCoreWebAPI.Controllers
{
    public class PersonalityController : Controller
    {
        private readonly string _connectionString;

        public PersonalityController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }


        [HttpGet("api/personality/{id}")]
        [ProducesResponseType(typeof(PersonalityData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PersonalityData>> GetPersonalityById(int id)
        {
            const string sql = @"
        SELECT 
    p.id,
    p.name,
    p.sport_id,
    p.entity_id,
    p.country_name_key,
    p.bio_data_key,
    p.media_id,
    p.creation_time,
    p.modified_time,
    p.modified_by,

    i.locale_id          AS locale_id,
    i18n_country.value   AS country_value,
    i18n_bio.value       AS bio_value,
    ml.media_link        AS media_link

FROM personality p
-- First join i18n i to provide locale_id reference
LEFT JOIN i18n i 
    ON i.`key` IN (p.country_name_key, p.bio_data_key)
-- Then use i.locale_id safely below
LEFT JOIN i18n i18n_country 
    ON i18n_country.`key` = p.country_name_key AND i18n_country.locale_id = i.locale_id
LEFT JOIN i18n i18n_bio 
    ON i18n_bio.`key` = p.bio_data_key AND i18n_bio.locale_id = i.locale_id
LEFT JOIN media_localization ml 
    ON ml.media_id = p.media_id AND ml.locale_id = i.locale_id

WHERE p.id = @PersonalityId
ORDER BY i.locale_id;
";

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PersonalityId", id);

            await using var reader = await cmd.ExecuteReaderAsync();

            PersonalityData personality = null;
            var countryValues = new List<LocalizedValue>();
            var bioValues = new List<LocalizedValue>();
            //var mediaLocalizations = new List<MediaLocalization>();

            while (await reader.ReadAsync())
            {
                if (personality == null)
                {
                    personality = new PersonalityData
                    {
                        Id = reader.GetInt32("id"),
                        Name = reader.GetString("name"),
                        SportId = reader.IsDBNull("sport_id") ? (int?)null : reader.GetInt32("sport_id"),
                        EntityId = reader.IsDBNull("entity_id") ? (int?)null : reader.GetInt32("entity_id"),
                        CreationTime = reader.GetDateTime("creation_time"),
                        ModifiedTime = reader.GetDateTime("modified_time"),
                        ModifiedBy = reader.GetString("modified_by"),
                        LocalizedCountry = new LocalizedName
                        {
                            Key = reader.IsDBNull("country_name_key") ? null : reader.GetString("country_name_key"),
                            Values = countryValues
                        },
                        LocalizedBio = new LocalizedName
                        {
                            Key = reader.IsDBNull("bio_data_key") ? null : reader.GetString("bio_data_key"),
                            Values = bioValues
                        },
                        Media = reader.IsDBNull("media_id")
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString("media_id"),
                                LinkLocalizations = []
                            }
                    };
                }

                if (!reader.IsDBNull("locale_id"))
                {
                    var locale = reader.GetString("locale_id");

                    if (!reader.IsDBNull("country_value"))
                    {
                        countryValues.Add(new LocalizedValue
                        {
                            LocaleId = locale,
                            Value = reader.GetString("country_value")
                        });
                    }

                    if (!reader.IsDBNull("bio_value"))
                    {
                        bioValues.Add(new LocalizedValue
                        {
                            LocaleId = locale,
                            Value = reader.GetString("bio_value")
                        });
                    }

                    if (!reader.IsDBNull("media_link"))
                    {
                        var MediaLink = reader.GetString("media_link");
                        if (MediaLink != null)

                            personality.Media.LinkLocalizations[locale] = MediaLink;
                    }
                }
            }

            return personality == null ? NotFound() : Ok(personality);
        }

    }
}
