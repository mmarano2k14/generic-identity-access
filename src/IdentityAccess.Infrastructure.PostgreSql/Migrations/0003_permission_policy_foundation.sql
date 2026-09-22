CREATE TABLE IF NOT EXISTS identity_access.application_security_models
(
    identity_scope_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    model_version integer NOT NULL,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_application_security_models PRIMARY KEY
        (identity_scope_id, application_key, model_version),
    CONSTRAINT ck_application_security_models_application_key
        CHECK (application_key ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_application_security_models_version CHECK (model_version > 0)
);

CREATE TABLE IF NOT EXISTS identity_access.application_capabilities
(
    identity_scope_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    model_version integer NOT NULL,
    capability_namespace varchar(64) NOT NULL,
    capability_resource varchar(64) NOT NULL,
    capability_action varchar(64) NOT NULL,
    display_name varchar(256) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_application_capabilities PRIMARY KEY
        (identity_scope_id, application_key, model_version,
         capability_namespace, capability_resource, capability_action),
    CONSTRAINT fk_application_capabilities_model FOREIGN KEY
        (identity_scope_id, application_key, model_version)
        REFERENCES identity_access.application_security_models
        (identity_scope_id, application_key, model_version) ON DELETE RESTRICT,
    CONSTRAINT ck_application_capabilities_namespace
        CHECK (capability_namespace ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_application_capabilities_resource
        CHECK (capability_resource ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_application_capabilities_action
        CHECK (capability_action ~ '^[a-z][a-z0-9-]{0,63}$')
);

CREATE TABLE IF NOT EXISTS identity_access.permission_policies
(
    identity_scope_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    policy_id uuid NOT NULL,
    display_name varchar(256) NOT NULL,
    status smallint NOT NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_permission_policies PRIMARY KEY
        (identity_scope_id, tenant_id, application_key, policy_id),
    CONSTRAINT fk_permission_policies_tenant FOREIGN KEY (identity_scope_id, tenant_id)
        REFERENCES identity_access.tenants (identity_scope_id, tenant_id) ON DELETE RESTRICT,
    CONSTRAINT ck_permission_policies_application_key
        CHECK (application_key ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_permission_policies_status CHECK (status IN (1, 2)),
    CONSTRAINT ck_permission_policies_row_version CHECK (row_version > 0)
);

CREATE TABLE IF NOT EXISTS identity_access.policy_statements
(
    identity_scope_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    policy_id uuid NOT NULL,
    statement_id uuid NOT NULL,
    model_version integer NOT NULL,
    capability_namespace varchar(64) NOT NULL,
    capability_resource varchar(64) NOT NULL,
    capability_action varchar(64) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_policy_statements PRIMARY KEY
        (identity_scope_id, tenant_id, application_key, policy_id, statement_id),
    CONSTRAINT fk_policy_statements_policy FOREIGN KEY
        (identity_scope_id, tenant_id, application_key, policy_id)
        REFERENCES identity_access.permission_policies
        (identity_scope_id, tenant_id, application_key, policy_id) ON DELETE RESTRICT,
    CONSTRAINT fk_policy_statements_capability FOREIGN KEY
        (identity_scope_id, application_key, model_version,
         capability_namespace, capability_resource, capability_action)
        REFERENCES identity_access.application_capabilities
        (identity_scope_id, application_key, model_version,
         capability_namespace, capability_resource, capability_action) ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS identity_access.group_policy_bindings
(
    identity_scope_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    group_id uuid NOT NULL,
    policy_id uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_group_policy_bindings PRIMARY KEY
        (identity_scope_id, tenant_id, application_key, group_id, policy_id),
    CONSTRAINT fk_group_policy_bindings_group FOREIGN KEY
        (identity_scope_id, tenant_id, application_key, group_id)
        REFERENCES identity_access.user_groups
        (identity_scope_id, tenant_id, application_key, group_id) ON DELETE RESTRICT,
    CONSTRAINT fk_group_policy_bindings_policy FOREIGN KEY
        (identity_scope_id, tenant_id, application_key, policy_id)
        REFERENCES identity_access.permission_policies
        (identity_scope_id, tenant_id, application_key, policy_id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_permission_policies_application
    ON identity_access.permission_policies (identity_scope_id, application_key, tenant_id);

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'policy_statements'
          AND column_name = 'capability_feature'
    ) THEN
        EXECUTE 'CREATE INDEX IF NOT EXISTS ix_policy_statements_capability '
             || 'ON identity_access.policy_statements '
             || '(identity_scope_id, application_key, model_version, '
             || 'capability_resource, capability_feature, capability_action)';
    ELSE
        EXECUTE 'CREATE INDEX IF NOT EXISTS ix_policy_statements_capability '
             || 'ON identity_access.policy_statements '
             || '(identity_scope_id, application_key, model_version, '
             || 'capability_namespace, capability_resource, capability_action)';
    END IF;
END
$$;

CREATE INDEX IF NOT EXISTS ix_group_policy_bindings_policy
    ON identity_access.group_policy_bindings
       (identity_scope_id, tenant_id, application_key, policy_id);
