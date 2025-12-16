// <copyright file="BoothActivityRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author></author>
// <date>07/28/2025</date>
// <summary>Class to handle booth activity SQL side</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public class BoothActivityRepository(IDbProvider db) : IBoothActivityRepository
    {
        private readonly IDbProvider _db = db;

        public async Task<List<BoothActivity>> AddUserBoothActivitiesAsync(List<BoothActivity> boothActivityList)
        {
            if (boothActivityList == null || boothActivityList.Count == 0)
                throw new ArgumentException("Booth activity list is empty", nameof(boothActivityList));

            var createdRecords = new List<BoothActivity>();

            await using var conn = await _db.OpenConnectionAsync();
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
                    await using var cmd = _db.CreateCommand(conn, insertSql, tx);
                    cmd.Parameters.Add(_db.CreateParameter("@BoothId", act.BoothId));
                    cmd.Parameters.Add(_db.CreateParameter("@UserId", act.UserId));
                    cmd.Parameters.Add(_db.CreateParameter("@ExitCode", act.ExitCode));
                    cmd.Parameters.Add(_db.CreateParameter("@BoothNameKey", act.BoothNameKey));
                    cmd.Parameters.Add(_db.CreateParameter("@SessionId", act.SessionId));
                    cmd.Parameters.Add(_db.CreateParameter("@EntryDatetime", act.EntryDatetime));
                    cmd.Parameters.Add(_db.CreateParameter("@SessionDuration", act.SessionDuration));
                    cmd.Parameters.Add(_db.CreateParameter("@ExitDatetime", act.ExitDatetime));

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
