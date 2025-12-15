// <copyright file="RulesMetadataRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>10/01/2025</date>
// <summary>CRUD operations for rules metadata (re_*) tables.</summary>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Sql;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public sealed class RulesMetadataRepository(IDbProvider db) : IRulesMetadataRepository
    {
        private readonly IDbProvider _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<IReadOnlyList<EventTypeView>> GetEventTypesAsync()
        {
            var list = new List<EventTypeView>();

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesMetadataSql.SelectEventTypes);
            await using var rdr = await cmd.ExecuteReaderAsync();

            while (await rdr.ReadAsync())
            {
                list.Add(new EventTypeView
                {
                    Id = rdr.GetInt32(rdr.GetOrdinal("Id")),
                    Name = rdr.GetString(rdr.GetOrdinal("Name"))
                });
            }

            return list;
        }

        public async Task<int> InsertEventTypeAsync(EventTypeCreateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesMetadataSql.InsertEventType);
            cmd.Parameters.Add(_db.CreateParameter("@Name", dto.Name));

            var obj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(obj);
        }

        public async Task<bool> UpdateEventTypeAsync(EventTypeUpdateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesMetadataSql.UpdateEventType);
            cmd.Parameters.Add(_db.CreateParameter("@Id", dto.Id));
            cmd.Parameters.Add(_db.CreateParameter("@Name", dto.Name));

            var affected = await cmd.ExecuteNonQueryAsync();
            return affected > 0;
        }

        public async Task<IReadOnlyList<ContextParameterView>> GetContextParametersAsync()
        {
            var list = new List<ContextParameterView>();

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesMetadataSql.SelectContextParameters);
            await using var rdr = await cmd.ExecuteReaderAsync();

            while (await rdr.ReadAsync())
            {
                var dropdownProviderIdOrdinal = rdr.GetOrdinal("DropdownProviderId");
                var providerTypeIdOrdinal = rdr.GetOrdinal("ProviderTypeId");
                var providerTypeOrdinal = rdr.GetOrdinal("ProviderType");
                var dropdownConfigOrdinal = rdr.GetOrdinal("DropdownConfigJson");
                var exampleValueOrdinal = rdr.GetOrdinal("ExampleValue");

                list.Add(new ContextParameterView
                {
                    Id = rdr.GetInt32(rdr.GetOrdinal("Id")),
                    Key = rdr.GetString(rdr.GetOrdinal("Key")),
                    Source = rdr.GetString(rdr.GetOrdinal("Source")),
                    Path = rdr.GetString(rdr.GetOrdinal("Path")),
                    RePathTypeId = rdr.GetInt32(rdr.GetOrdinal("RePathTypeId")),
                    DataType = rdr.GetString(rdr.GetOrdinal("DataType")),
                    UiLabel = rdr.GetString(rdr.GetOrdinal("UiLabel")),
                    UiHelpKey = rdr.GetString(rdr.GetOrdinal("UiHelpKey")),
                    Unit = rdr.IsDBNull(rdr.GetOrdinal("Unit")) ? null : rdr.GetString(rdr.GetOrdinal("Unit")),
                    DropdownProviderId = rdr.IsDBNull(dropdownProviderIdOrdinal) ? null : rdr.GetInt32(dropdownProviderIdOrdinal),
                    ProviderTypeId = rdr.IsDBNull(providerTypeIdOrdinal) ? null : rdr.GetInt32(providerTypeIdOrdinal),
                    ProviderType = rdr.IsDBNull(providerTypeOrdinal) ? null : rdr.GetString(providerTypeOrdinal),
                    DropdownConfigJson = rdr.IsDBNull(dropdownConfigOrdinal) ? null : rdr.GetString(dropdownConfigOrdinal),
                    ExampleValue = rdr.IsDBNull(exampleValueOrdinal) ? null : rdr.GetString(exampleValueOrdinal)
                });
            }

            return list;
        }

        public async Task<int> InsertContextParameterAsync(ContextParameterCreateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesMetadataSql.InsertContextParameter);
            cmd.Parameters.Add(_db.CreateParameter("@Key", dto.Key));
            cmd.Parameters.Add(_db.CreateParameter("@Source", dto.Source));
            cmd.Parameters.Add(_db.CreateParameter("@Path", dto.Path));
            cmd.Parameters.Add(_db.CreateParameter("@RePathTypeId", dto.RePathTypeId));
            cmd.Parameters.Add(_db.CreateParameter("@DataType", dto.DataType));
            cmd.Parameters.Add(_db.CreateParameter("@UiLabel", dto.UiLabel));
            cmd.Parameters.Add(_db.CreateParameter("@UiHelpKey", dto.UiHelpKey));
            cmd.Parameters.Add(_db.CreateParameter("@Unit", dto.Unit));
            cmd.Parameters.Add(_db.CreateParameter("@DropdownProviderId", dto.ReDropdownValueProviderId));
            cmd.Parameters.Add(_db.CreateParameter("@ExampleValue", dto.ExampleValue));

            var obj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(obj);
        }

        public async Task<bool> UpdateContextParameterAsync(ContextParameterUpdateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesMetadataSql.UpdateContextParameter);
            cmd.Parameters.Add(_db.CreateParameter("@Id", dto.Id));
            cmd.Parameters.Add(_db.CreateParameter("@Key", dto.Key));
            cmd.Parameters.Add(_db.CreateParameter("@Source", dto.Source));
            cmd.Parameters.Add(_db.CreateParameter("@Path", dto.Path));
            cmd.Parameters.Add(_db.CreateParameter("@RePathTypeId", dto.RePathTypeId));
            cmd.Parameters.Add(_db.CreateParameter("@DataType", dto.DataType));
            cmd.Parameters.Add(_db.CreateParameter("@UiLabel", dto.UiLabel));
            cmd.Parameters.Add(_db.CreateParameter("@UiHelpKey", dto.UiHelpKey));
            cmd.Parameters.Add(_db.CreateParameter("@Unit", dto.Unit));
            cmd.Parameters.Add(_db.CreateParameter("@DropdownProviderId", dto.ReDropdownValueProviderId));
            cmd.Parameters.Add(_db.CreateParameter("@ExampleValue", dto.ExampleValue));

            var affected = await cmd.ExecuteNonQueryAsync();
            return affected > 0;
        }

        public async Task<IReadOnlyList<EventTypeParameterView>> GetEventTypeParametersAsync(int eventTypeId)
        {
            var list = new List<EventTypeParameterView>();

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesMetadataSql.SelectEventTypeParameters);
            cmd.Parameters.Add(_db.CreateParameter("@EventTypeId", eventTypeId));

            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                list.Add(new EventTypeParameterView
                {
                    Id = rdr.GetInt32(rdr.GetOrdinal("Id")),
                    EventTypeId = rdr.GetInt32(rdr.GetOrdinal("EventTypeId")),
                    ParameterId = rdr.GetInt32(rdr.GetOrdinal("ParameterId")),
                    ParameterKey = rdr.GetString(rdr.GetOrdinal("ParameterKey")),
                    IsRequired = rdr.GetInt32(rdr.GetOrdinal("IsRequired")) == 1,
                    DefaultValueJson = rdr.IsDBNull(rdr.GetOrdinal("DefaultValueJson")) ? null : rdr.GetString(rdr.GetOrdinal("DefaultValueJson"))
                });
            }

            return list;
        }

        public async Task<int> InsertEventTypeParameterAsync(EventTypeParameterCreateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesMetadataSql.InsertEventTypeParameter);
            cmd.Parameters.Add(_db.CreateParameter("@EventTypeId", dto.ReEventTypeId));
            cmd.Parameters.Add(_db.CreateParameter("@ParameterId", dto.ParameterId));
            cmd.Parameters.Add(_db.CreateParameter("@IsRequired", dto.IsRequired ? 1 : 0));
            cmd.Parameters.Add(_db.CreateParameter("@DefaultValueJson", dto.DefaultValueJson));

            var obj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(obj);
        }

        public async Task<bool> UpdateEventTypeParameterAsync(EventTypeParameterUpdateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesMetadataSql.UpdateEventTypeParameter);
            cmd.Parameters.Add(_db.CreateParameter("@Id", dto.Id));
            cmd.Parameters.Add(_db.CreateParameter("@EventTypeId", dto.ReEventTypeId));
            cmd.Parameters.Add(_db.CreateParameter("@ParameterId", dto.ParameterId));
            cmd.Parameters.Add(_db.CreateParameter("@IsRequired", dto.IsRequired ? 1 : 0));
            cmd.Parameters.Add(_db.CreateParameter("@DefaultValueJson", dto.DefaultValueJson));

            var affected = await cmd.ExecuteNonQueryAsync();
            return affected > 0;
        }
    }
}
