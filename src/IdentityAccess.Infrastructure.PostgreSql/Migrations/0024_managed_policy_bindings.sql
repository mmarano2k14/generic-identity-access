CREATE TABLE identity_access.managed_group_policy_bindings
(
    identity_scope_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    group_id uuid NOT NULL,
    policy_id uuid NOT NULL,
    policy_version integer NOT NULL,
    resource_scope_id uuid NULL,
    include_descendants boolean NOT NULL DEFAULT FALSE,
    binding_target_key text GENERATED ALWAYS AS
        (COALESCE(resource_scope_id::text, 'tenant')) STORED,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_managed_group_policy_bindings PRIMARY KEY
        (identity_scope_id, tenant_id, application_key, group_id,
         policy_id, policy_version, binding_target_key),
    CONSTRAINT fk_managed_group_policy_bindings_group FOREIGN KEY
        (identity_scope_id, tenant_id, application_key, group_id)
        REFERENCES identity_access.user_groups
        (identity_scope_id, tenant_id, application_key, group_id) ON DELETE RESTRICT,
    CONSTRAINT fk_managed_group_policy_bindings_policy_version FOREIGN KEY
        (identity_scope_id, application_key, policy_id, policy_version)
        REFERENCES identity_access.managed_policy_versions
        (identity_scope_id, application_key, policy_id, policy_version) ON DELETE RESTRICT,
    CONSTRAINT fk_managed_group_policy_bindings_resource_scope FOREIGN KEY
        (identity_scope_id, tenant_id, application_key, resource_scope_id)
        REFERENCES identity_access.resource_scopes
        (identity_scope_id, tenant_id, application_key, resource_scope_id) ON DELETE RESTRICT,
    CONSTRAINT ck_managed_group_policy_bindings_descendants
        CHECK (resource_scope_id IS NOT NULL OR include_descendants = FALSE),
    CONSTRAINT ck_managed_group_policy_bindings_policy_version
        CHECK (policy_version > 0)
);

CREATE INDEX ix_managed_group_policy_bindings_policy
    ON identity_access.managed_group_policy_bindings
       (identity_scope_id, application_key, policy_id, policy_version, tenant_id);

CREATE INDEX ix_managed_group_policy_bindings_resource_scope
    ON identity_access.managed_group_policy_bindings
       (identity_scope_id, tenant_id, application_key, resource_scope_id)
    WHERE resource_scope_id IS NOT NULL;

CREATE TRIGGER trg_security_mutation_managed_group_policy_bindings
AFTER INSERT OR UPDATE OR DELETE
ON identity_access.managed_group_policy_bindings
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'tenant_id',
    'application_key',
    'group_id',
    'policy_id',
    'policy_version',
    'binding_target_key');
