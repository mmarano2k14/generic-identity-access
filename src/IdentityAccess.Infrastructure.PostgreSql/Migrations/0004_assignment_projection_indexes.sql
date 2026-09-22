CREATE INDEX IF NOT EXISTS ix_tenant_memberships_active_subject_tenant
    ON identity_access.tenant_memberships
       (identity_scope_id, tenant_id, user_id, membership_id)
    WHERE status = 1;

CREATE INDEX IF NOT EXISTS ix_group_memberships_assignment_projection
    ON identity_access.group_memberships
       (identity_scope_id, tenant_id, application_key, tenant_membership_id, group_id);

CREATE INDEX IF NOT EXISTS ix_group_policy_bindings_assignment_projection
    ON identity_access.group_policy_bindings
       (identity_scope_id, tenant_id, application_key, group_id, policy_id);

CREATE INDEX IF NOT EXISTS ix_policy_statements_assignment_projection
    ON identity_access.policy_statements
       (identity_scope_id, tenant_id, application_key, policy_id, statement_id);
