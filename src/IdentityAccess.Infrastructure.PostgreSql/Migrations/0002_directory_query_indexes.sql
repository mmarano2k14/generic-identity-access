CREATE INDEX IF NOT EXISTS ix_tenant_memberships_tenant
    ON identity_access.tenant_memberships (identity_scope_id, tenant_id, membership_id);

CREATE INDEX IF NOT EXISTS ix_user_groups_tenant_application
    ON identity_access.user_groups (identity_scope_id, tenant_id, application_key, group_id);

CREATE INDEX IF NOT EXISTS ix_group_memberships_group
    ON identity_access.group_memberships
    (identity_scope_id, tenant_id, application_key, group_id, tenant_membership_id);
