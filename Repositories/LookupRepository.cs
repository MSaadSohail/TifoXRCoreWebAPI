// <copyright file="LookupRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/30/2025</date>
// <summary>Fetches lookup data used for authoring rewards and rules.</summary>

using GMS.TifoXRCoreWebAPI.Repositories.Sql;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public sealed class LookupRepository : ILookupRepository
    {
        private readonly IDbProvider _db;
        public LookupRepository(IDbProvider db) => _db = db;

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetRewardCompositionTypesAsync()
            => QueryAsync(LookupSql.RewardCompositionTypes);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetRewardStatusesAsync()
            => QueryAsync(LookupSql.RewardStatuses);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetStateTypesAsync()
            => QueryAsync(LookupSql.StateTypes);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetRuleActionTypesAsync()
            => QueryAsync(LookupSql.RuleActionTypes);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetEventTypesAsync()
            => QueryAsync(LookupSql.EventTypes);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetComparatorsAsync()
            => QueryAsync(LookupSql.Comparators);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetLogicalOperatorsAsync()
            => QueryAsync(LookupSql.LogicalOperators);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetPathTypesAsync()
            => QueryAsync(LookupSql.PathTypes);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetProviderTypesAsync()
            => QueryAsync(LookupSql.ProviderTypes);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetContextParametersAsync()
            => QueryAsync(LookupSql.ContextParameters);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetEventTypeParametersAsync(int eventTypeId)
            => QueryAsync(LookupSql.EventTypeParameters, cmd =>
                cmd.Parameters.Add(_db.CreateParameter("@EventTypeId", eventTypeId)));

        private async Task<IReadOnlyList<IDictionary<string, object?>>> QueryAsync(string sql, Action<System.Data.Common.DbCommand>? configure = null)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            configure?.Invoke(cmd);

            var list = new List<IDictionary<string, object?>>();
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < rdr.FieldCount; i++)
                {
                    row[rdr.GetName(i)] = rdr.IsDBNull(i) ? null : rdr.GetValue(i);
                }

                list.Add(row);
            }

            return list;
        }
    }
}
