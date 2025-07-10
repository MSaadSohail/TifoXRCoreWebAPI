using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using TifoXRWebApi.Models;

namespace TifoXRWebApi.Controllers
{
    [Route("api/metrics")]
    [ApiController]
    public class MetricsController : ControllerBase
    {
        private readonly string _connectionString;

        public MetricsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// POST /api/metrics/boothActivity
        /// Inserts a batch of user booth activity metrics.
        /// </summary>
        [HttpPost("boothActivity")]
        [ProducesResponseType(typeof(List<BoothActivity>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<BoothActivity>>> AddUserBoothActivities(
            [FromBody] List<BoothActivity> boothActivityList
        )
        {
            if (boothActivityList == null || boothActivityList.Count == 0)
                return BadRequest();

            var createdRecords = new List<BoothActivity>();

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                const string insertSql = @"
                    INSERT INTO user_booth_activity_mtx
                      (booth_id, user_id, exit_code, booth_name_key,
                       session_id, entry_datetime, session_duration, exit_datetime)
                    VALUES
                      (@BoothId, @UserId, @ExitCode, @BoothNameKey,
                       @SessionId, @EntryDatetime, @SessionDuration, @ExitDatetime);";

                foreach (var act in boothActivityList)
                {

                    await using var cmd = new MySqlCommand(insertSql, conn, tx);
                    cmd.Parameters.AddWithValue("@BoothId",         act.BoothId);
                    cmd.Parameters.AddWithValue("@UserId",          act.UserId);
                    cmd.Parameters.AddWithValue("@ExitCode",        act.ExitCode);
                    cmd.Parameters.AddWithValue("@BoothNameKey",    act.BoothNameKey);
                    cmd.Parameters.AddWithValue("@SessionId",       act.SessionId);
                    cmd.Parameters.AddWithValue("@EntryDatetime",   act.EntryDatetime);
                    cmd.Parameters.AddWithValue("@SessionDuration", act.SessionDuration);
                    cmd.Parameters.AddWithValue("@ExitDatetime",    (object?)act.ExitDatetime ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();

                    // Use inheritance: BoothActivityData extends BoothActivity
                    var record = new BoothActivity
                    {
                        BoothId         = act.BoothId,
                        UserId          = act.UserId,
                        ExitCode        = act.ExitCode,
                        BoothNameKey    = act.BoothNameKey,
                        SessionId       = act.SessionId,
                        EntryDatetime   = act.EntryDatetime,
                        SessionDuration = act.SessionDuration,
                        ExitDatetime    = act.ExitDatetime
                    };
                    createdRecords.Add(record);
                }

                await tx.CommitAsync();
                return Ok(createdRecords);
            }
            catch (MySqlException ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
