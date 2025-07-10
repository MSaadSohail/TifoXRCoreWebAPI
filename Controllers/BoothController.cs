//using Microsoft.AspNetCore.Mvc;
//using MySqlConnector;
//using TifoXRWebApi.Models;
//using TifoXRWebApi.Models.Common;

//namespace TifoXRWebApi.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class BoothController : ControllerBase
//    {
//        private readonly string _connectionString;

//        public BoothController(IConfiguration configuration)
//        {
//            _connectionString = configuration.GetConnectionString("DefaultConnection");
//        }

//        // GET /api/booth/3
//        [HttpGet("{id}")]
//        public async Task<ActionResult<BoothModel>> GetBoothDataByID([FromRoute] int id)
//        {
//            await using var conn = new MySqlConnection(_connectionString);
//            await conn.OpenAsync();

//            const string sql = @"
//                SELECT
//                    b.id,
//                    b.space_id,
//                    b.name_key,
//                    i.locale_id,
//                    i.value
//                FROM
//                    booth b
//                JOIN
//                    i18n i ON i.key = b.name_key
//                WHERE
//                    b.id = @BoothId
//            ";

//            await using var cmd = new MySqlCommand(sql, conn);
//            cmd.Parameters.Add("@BoothId", MySqlDbType.Int32).Value = id;

//            await using var reader = await cmd.ExecuteReaderAsync();
//            BoothModel? booth = null;

//            while (await reader.ReadAsync())
//            {
//                if (booth == null)
//                {
//                    booth = new BoothModel
//                    {
//                        Id = reader.GetInt32("id"),
//                        SpaceId = reader.GetInt32("space_id"),
//                        LocalizedName = new LocalizedName
//                        {
//                            Key = reader.GetString("name_key"),
//                            Values = new List<LocalizedValue>()
//                        }
//                    };
//                }

//                booth.LocalizedName.Values.Add(new LocalizedValue
//                {
//                    LocaleId = reader.GetString("locale_id"),
//                    Value = reader.GetString("value")
//                });
//            }

//            if (booth == null)
//                return NotFound();

//            return Ok(booth);
//        }

//        // PUT /api/booth/123
//        [HttpPut("{id}")]
//        [ProducesResponseType(typeof(BoothWrapper), StatusCodes.Status200OK)]
//        [ProducesResponseType(StatusCodes.Status400BadRequest)]
//        [ProducesResponseType(StatusCodes.Status404NotFound)]
//        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//        public async Task<ActionResult<BoothWrapper>> UpdateBoothById( 
//            [FromRoute(Name = "id")] int id, 
//            [FromBody] BoothUpdateDto update)
//        {
//            if (update == null)
//                return BadRequest();

//            await using var conn = new MySqlConnection(_connectionString);
//            await conn.OpenAsync();
//            await using var tx = await conn.BeginTransactionAsync();

//            try
//            {
//                // 1) Update booth table
//                const string updateBoothSql = @"
//                    UPDATE booth
//                    SET name_key = @nameKey
//                    WHERE id = @Id;
//                ";

//                await using (var cmd = new MySqlCommand(updateBoothSql, conn, tx))
//                {
//                    cmd.Parameters.AddWithValue("@nameKey", update.LocalizedName.Key);
//                    cmd.Parameters.AddWithValue("@Id", id);
//                    var rows = await cmd.ExecuteNonQueryAsync();
//                    if (rows == 0)
//                        return NotFound();    // no booth to update
//                }

//                // 2) Upsert each i18n entry for this name_key
//                const string updateI18nSql = @"
//                    UPDATE i18n
//                    SET value = @value
//                    WHERE `key`     = @nameKey
//                    AND locale_id = @locale
//                    AND space_id  = @spaceId;
//                ";

//                const string insertI18nSql = @"
//                    INSERT INTO i18n (`key`, locale_id, value, space_id)
//                    VALUES (@nameKey, @locale, @value, @spaceId);
//                ";

//                foreach (var val in update.LocalizedName.Values)
//                {
//                    // 2a) try UPDATE
//                    await using var cmdUp = new MySqlCommand(updateI18nSql, conn, tx);
//                    cmdUp.Parameters.AddWithValue("@nameKey", update.LocalizedName.Key);
//                    cmdUp.Parameters.AddWithValue("@locale", val.LocaleId);
//                    cmdUp.Parameters.AddWithValue("@value", val.Value);
//                    var updated = await cmdUp.ExecuteNonQueryAsync();

//                    if (updated == 0)
//                    {
//                        // 2b) nothing to update → INSERT new row
//                        await using var cmdIn = new MySqlCommand(insertI18nSql, conn, tx);
//                        cmdIn.Parameters.AddWithValue("@nameKey", update.LocalizedName.Key);
//                        cmdIn.Parameters.AddWithValue("@locale", val.LocaleId);
//                        cmdIn.Parameters.AddWithValue("@value", val.Value);
//                        await cmdIn.ExecuteNonQueryAsync();
//                    }
//                }

//                await tx.CommitAsync();

//                // 3) Re-fetch the updated record
//                var refreshed = await LoadBoothById(conn, id);
//                return Ok(new BoothWrapper { booth = refreshed });
//            }
//            catch (MySqlException)
//            {
//                await tx.RollbackAsync();
//                return StatusCode(500);
//            }
//        }

//        [HttpPost]
//        [ProducesResponseType(typeof(BoothWrapper), StatusCodes.Status201Created)]
//        [ProducesResponseType(StatusCodes.Status400BadRequest)]
//        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//        public async Task<ActionResult<BoothWrapper>> CreateBooth( [FromBody] BoothCreateDto create)
//        {
//            if (create == null)
//                return BadRequest();

//            await using var conn = new MySqlConnection(_connectionString);
//            await conn.OpenAsync();
//            await using var tx = await conn.BeginTransactionAsync();

//            try
//            {
//                // 1) Insert into booth, no multi‐statement
//                const string insertBoothSql = @"
//                    INSERT INTO booth (space_id, name_key)
//                    VALUES (@spaceId, @nameKey);
//                ";

//                int newId;
//                await using (var cmd = new MySqlCommand(insertBoothSql, conn, tx))
//                {
//                    cmd.Parameters.AddWithValue("@spaceId", create.SpaceId);
//                    cmd.Parameters.AddWithValue("@nameKey", create.LocalizedName.Key);

//                    // Run the INSERT
//                    await cmd.ExecuteNonQueryAsync();

//                    // Grab the auto‐incremented ID
//                    newId = Convert.ToInt32(cmd.LastInsertedId);
//                }

//                // 2) Insert your i18n rows as before
//                const string insertI18nSql = @"
//                    INSERT INTO i18n (`key`, locale_id, value, space_id)
//                    VALUES (@nameKey, @locale, @value, @spaceId);
//                ";

//                foreach (var val in create.LocalizedName.Values)
//                {
//                    await using var cmdI18n = new MySqlCommand(insertI18nSql, conn, tx);
//                    cmdI18n.Parameters.AddWithValue("@nameKey", create.LocalizedName.Key);
//                    cmdI18n.Parameters.AddWithValue("@locale", val.LocaleId);
//                    cmdI18n.Parameters.AddWithValue("@value", val.Value);
//                    cmdI18n.Parameters.AddWithValue("@spaceId", create.SpaceId);
//                    await cmdI18n.ExecuteNonQueryAsync();
//                }

//                await tx.CommitAsync();

//                // 3) Reload and return
//                var created = await LoadBoothById(conn, newId);
//                return CreatedAtAction(
//                    nameof(GetBoothDataByID),
//                    new { id = newId },
//                    new BoothWrapper { booth = created }
//                );
//            }
//            catch (Exception ex)
//            {
//                await tx.RollbackAsync();
//                return StatusCode(500, new { error = ex.Message });
//            }
//        }

//        // Helper to SELECT the booth + localized_name JSON from the DB
//        private static async Task<BoothModel> LoadBoothById(MySqlConnection conn, int id)
//        {
//            const string sql = @"
//                SELECT 
//                  b.id,
//                  b.space_id,
//                  b.name_key,
//                  i.locale_id,
//                  i.value
//                FROM booth AS b
//                JOIN i18n  AS i  ON i.`key` = b.name_key
//                WHERE b.id = @BoothId;
//            ";

//            await using var cmd = new MySqlCommand(sql, conn);
//            cmd.Parameters.AddWithValue("@BoothId", id);
//            await using var reader = await cmd.ExecuteReaderAsync();

//            BoothModel? booth = null;

//            while (await reader.ReadAsync())
//            {
//                booth ??= new BoothModel
//                    {
//                        Id = reader.GetInt32("id"),
//                        SpaceId = reader.GetInt32("space_id"),
//                        LocalizedName = new LocalizedName
//                        {
//                            Key = reader.GetString("name_key"),
//                            Values = new List<LocalizedValue>()
//                        }
//                    };

//                booth.LocalizedName.Values.Add(new LocalizedValue
//                {
//                    LocaleId = reader.GetString("locale_id"),
//                    Value = reader.GetString("value")
//                });
//            }

//            return booth;
//        }
//    }
//}



