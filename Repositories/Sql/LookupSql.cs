// <copyright file="LookupSql.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/30/2025</date>
// <summary>SQL fragments for lookup retrieval.</summary>

namespace GMS.TifoXRCoreWebAPI.Repositories.Sql
{
    public static class LookupSql
    {
        public const string RewardCompositionTypes = @"
            SELECT id, type AS Type
            FROM reward_composition_type
            ORDER BY id;";

        public const string RewardStatuses = @"
            SELECT id, status AS Status
            FROM reward_status
            ORDER BY id;";

        public const string StateTypes = @"
            SELECT id, type AS Type
            FROM state_type
            ORDER BY id;";

        public const string RuleActionTypes = @"
            SELECT id, type AS Type
            FROM rule_action_type
            ORDER BY id;";

        public const string EventTypes = @"
            SELECT id, name AS Name
            FROM re_event_type
            ORDER BY id;";

        public const string Comparators = @"
            SELECT id,
                   code AS Code,
                   ui_label AS UiLabel,
                   supported_types AS SupportedTypes,
                   engine_format AS EngineFormat,
                   value_arity AS ValueArity,
                   (CASE WHEN case_sensitive IS NULL THEN NULL ELSE (case_sensitive + 0) END) AS CaseSensitive
            FROM re_comparator
            ORDER BY id;";

        public const string LogicalOperators = @"
            SELECT id, code AS Code, ui_label AS UiLabel, engine_format AS EngineFormat
            FROM re_logical_operator
            ORDER BY id;";

        public const string PathTypes = @"
            SELECT id, type AS Type
            FROM re_path_type
            ORDER BY id;";

        public const string ProviderTypes = @"
            SELECT id, type AS Type
            FROM re_provider_type
            ORDER BY id;";

        public const string ContextParameters = @"
            SELECT cp.id,
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
                   cp.example_value AS ExampleValue
            FROM re_context_parameter cp
            LEFT JOIN re_dropdown_value_provider d ON d.id = cp.re_dropdown_value_provider_id
            LEFT JOIN re_provider_type pt ON pt.id = d.re_provider_type_id
            ORDER BY cp.id;";

        public const string EventTypeParameters = @"
            SELECT etp.id,
                   etp.re_event_type_id AS EventTypeId,
                   etp.parameter_id AS ParameterId,
                   cp.`key` AS ParameterKey,
                   (etp.is_required + 0) AS IsRequired,
                   etp.default_value_json AS DefaultValueJson
            FROM re_event_type_parameter etp
            JOIN re_context_parameter cp ON cp.id = etp.parameter_id
            WHERE etp.re_event_type_id = @EventTypeId
            ORDER BY etp.id;";
    }
}
