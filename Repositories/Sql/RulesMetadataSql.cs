// <copyright file="RulesMetadataSql.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>10/01/2025</date>
// <summary>SQL statements for rules metadata CRUD operations.</summary>

namespace GMS.TifoXRCoreWebAPI.Repositories.Sql
{
    public static class RulesMetadataSql
    {
        public const string SelectEventTypes = @"
            SELECT id AS Id, name AS Name
            FROM re_event_type
            ORDER BY id;";

        public const string InsertEventType = @"
            INSERT INTO re_event_type (name, creation_time, modified_by)
            VALUES (@Name, NOW(6), 'system');
            SELECT LAST_INSERT_ID();";

        public const string UpdateEventType = @"
            UPDATE re_event_type
            SET name = @Name,
                modified_by = 'system'
            WHERE id = @Id;";

        public const string SelectContextParameters = @"
            SELECT cp.id AS Id,
                   cp.`key` AS `Key`,
                   cp.source AS Source,
                   cp.path AS Path,
                   cp.re_path_type_id AS RePathTypeId,
                   cp.data_type AS DataType,
                   cp.ui_label AS UiLabel,
                   cp.ui_help_key AS UiHelpKey,
                   cp.unit AS Unit,
                   cp.re_dropdown_value_provider_id AS DropdownProviderId,
                   d.re_provider_type_id AS ProviderTypeId,
                   pt.type AS ProviderType,
                   d.config_json AS DropdownConfigJson,
                   cp.example_value AS ExampleValue
            FROM re_context_parameter cp
            LEFT JOIN re_dropdown_value_provider d ON d.id = cp.re_dropdown_value_provider_id
            LEFT JOIN re_provider_type pt ON pt.id = d.re_provider_type_id
            ORDER BY cp.id;";

        public const string InsertContextParameter = @"
            INSERT INTO re_context_parameter
                (`key`, source, path, re_path_type_id, data_type,
                 ui_label, ui_help_key, unit, re_dropdown_value_provider_id,
                 example_value, creation_time, modified_by)
            VALUES
                (@Key, @Source, @Path, @RePathTypeId, @DataType,
                 @UiLabel, @UiHelpKey, @Unit, @DropdownProviderId,
                 @ExampleValue, NOW(6), 'system');
            SELECT LAST_INSERT_ID();";

        public const string UpdateContextParameter = @"
            UPDATE re_context_parameter
            SET `key` = @Key,
                source = @Source,
                path = @Path,
                re_path_type_id = @RePathTypeId,
                data_type = @DataType,
                ui_label = @UiLabel,
                ui_help_key = @UiHelpKey,
                unit = @Unit,
                re_dropdown_value_provider_id = @DropdownProviderId,
                example_value = @ExampleValue,
                modified_by = 'system'
            WHERE id = @Id;";

        public const string SelectEventTypeParameters = @"
            SELECT etp.id AS Id,
                   etp.re_event_type_id AS EventTypeId,
                   etp.parameter_id AS ParameterId,
                   cp.`key` AS ParameterKey,
                   (etp.is_required + 0) AS IsRequired,
                   etp.default_value_json AS DefaultValueJson
            FROM re_event_type_parameter etp
            JOIN re_context_parameter cp ON cp.id = etp.parameter_id
            WHERE etp.re_event_type_id = @EventTypeId
            ORDER BY etp.id;";

        public const string InsertEventTypeParameter = @"
            INSERT INTO re_event_type_parameter
                (re_event_type_id, parameter_id, is_required, default_value_json, creation_time, modified_by)
            VALUES (@EventTypeId, @ParameterId, @IsRequired, @DefaultValueJson, NOW(6), 'system');
            SELECT LAST_INSERT_ID();";

        public const string UpdateEventTypeParameter = @"
            UPDATE re_event_type_parameter
            SET re_event_type_id = @EventTypeId,
                parameter_id = @ParameterId,
                is_required = @IsRequired,
                default_value_json = @DefaultValueJson,
                modified_by = 'system'
            WHERE id = @Id;";
    }
}
