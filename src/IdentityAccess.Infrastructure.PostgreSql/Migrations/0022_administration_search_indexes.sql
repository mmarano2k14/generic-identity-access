CREATE INDEX IF NOT EXISTS ix_users_display_name_search
    ON identity_access.users (identity_scope_id, lower(display_name) text_pattern_ops);

CREATE INDEX IF NOT EXISTS ix_tenants_display_name_search
    ON identity_access.tenants (identity_scope_id, lower(display_name) text_pattern_ops);

CREATE INDEX IF NOT EXISTS ix_user_groups_display_name_search
    ON identity_access.user_groups
       (identity_scope_id, tenant_id, application_key, lower(display_name) text_pattern_ops);

CREATE INDEX IF NOT EXISTS ix_permission_policies_display_name_search
    ON identity_access.permission_policies
       (identity_scope_id, tenant_id, application_key, lower(display_name) text_pattern_ops);

CREATE INDEX IF NOT EXISTS ix_resource_scopes_display_name_search
    ON identity_access.resource_scopes
       (identity_scope_id, tenant_id, application_key, lower(display_name) text_pattern_ops);

CREATE INDEX IF NOT EXISTS ix_resource_scopes_external_id_search
    ON identity_access.resource_scopes
       (identity_scope_id, tenant_id, application_key, lower(external_resource_id) text_pattern_ops);

CREATE INDEX IF NOT EXISTS ix_identity_scope_administration_groups_display_name_search
    ON identity_access.identity_scope_administration_groups
       (identity_scope_id, application_key, lower(display_name) text_pattern_ops);

CREATE INDEX IF NOT EXISTS ix_identity_scope_administration_policies_display_name_search
    ON identity_access.identity_scope_administration_policies
       (identity_scope_id, application_key, lower(display_name) text_pattern_ops);
