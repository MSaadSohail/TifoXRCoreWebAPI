using MySqlConnector;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public class BoothActivityRepository : IBoothActivityRepository
    {
        private readonly string _connectionString;

        public BoothActivityRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<List<BoothActivity>> AddUserBoothActivitiesAsync(List<BoothActivity> boothActivityList)
        {
            if (boothActivityList == null || boothActivityList.Count == 0)
                throw new ArgumentException("Booth activity list is empty", nameof(boothActivityList));

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
                    cmd.Parameters.AddWithValue("@BoothId", act.BoothId);
                    cmd.Parameters.AddWithValue("@UserId", act.UserId);
                    cmd.Parameters.AddWithValue("@ExitCode", act.ExitCode);
                    cmd.Parameters.AddWithValue("@BoothNameKey", act.BoothNameKey);
                    cmd.Parameters.AddWithValue("@SessionId", act.SessionId);
                    cmd.Parameters.AddWithValue("@EntryDatetime", act.EntryDatetime);
                    cmd.Parameters.AddWithValue("@SessionDuration", act.SessionDuration);
                    cmd.Parameters.AddWithValue("@ExitDatetime", (object?)act.ExitDatetime ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();

                    createdRecords.Add(new BoothActivity
                    {
                        BoothId = act.BoothId,
                        UserId = act.UserId,
                        ExitCode = act.ExitCode,
                        BoothNameKey = act.BoothNameKey,
                        SessionId = act.SessionId,
                        EntryDatetime = act.EntryDatetime,
                        SessionDuration = act.SessionDuration,
                        ExitDatetime = act.ExitDatetime
                    });
                }

                await tx.CommitAsync();
                return createdRecords;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
