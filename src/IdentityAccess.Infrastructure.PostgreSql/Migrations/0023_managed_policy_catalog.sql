CREATE TABLE IF NOT EXISTS identity_access.managed_policies
(
    identity_scope_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    policy_id uuid NOT NULL,
    policy_key varchar(128) NOT NULL,
    display_name varchar(256) NOT NULL,
    status smallint NOT NULL,
    default_version integer NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_managed_policies PRIMARY KEY
        (identity_scope_id, application_key, policy_id),
    CONSTRAINT uq_managed_policies_key UNIQUE
        (identity_scope_id, application_key, policy_key),
    CONSTRAINT ck_managed_policies_application_key
        CHECK (application_key ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_managed_policies_policy_key
        CHECK (policy_key ~ '^[a-z][a-z0-9-]{0,127}$'),
    CONSTRAINT ck_managed_policies_status CHECK (status IN (1, 2)),
    CONSTRAINT ck_managed_policies_default_version
        CHECK (default_version IS NULL OR default_version > 0),
    CONSTRAINT ck_managed_policies_row_version CHECK (row_version > 0)
);

CREATE TABLE IF NOT EXISTS identity_access.managed_policy_versions
(
    identity_scope_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    policy_id uuid NOT NULL,
    policy_version integer NOT NULL,
    model_version integer NOT NULL,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_managed_policy_versions PRIMARY KEY
        (identity_scope_id, application_key, policy_id, policy_version),
    CONSTRAINT uq_managed_policy_versions_model UNIQUE
        (identity_scope_id, application_key, policy_id, policy_version, model_version),
    CONSTRAINT fk_managed_policy_versions_policy FOREIGN KEY
        (identity_scope_id, application_key, policy_id)
        REFERENCES identity_access.managed_policies
        (identity_scope_id, application_key, policy_id) ON DELETE RESTRICT,
    CONSTRAINT fk_managed_policy_versions_model FOREIGN KEY
        (identity_scope_id, application_key, model_version)
        REFERENCES identity_access.application_security_models
        (identity_scope_id, application_key, model_version) ON DELETE RESTRICT,
    CONSTRAINT ck_managed_policy_versions_version CHECK (policy_version > 0),
    CONSTRAINT ck_managed_policy_versions_model_version CHECK (model_version > 0)
);

ALTER TABLE identity_access.managed_policies
    ADD CONSTRAINT fk_managed_policies_default_version FOREIGN KEY
        (identity_scope_id, application_key, policy_id, default_version)
        REFERENCES identity_access.managed_policy_versions
        (identity_scope_id, application_key, policy_id, policy_version)
        ON DELETE RESTRICT;

CREATE TABLE IF NOT EXISTS identity_access.managed_policy_statements
(
    identity_scope_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    policy_id uuid NOT NULL,
    policy_version integer NOT NULL,
    model_version integer NOT NULL,
    statement_id uuid NOT NULL,
    capability_resource varchar(64) NOT NULL,
    capability_feature varchar(64) NOT NULL,
    capability_action varchar(64) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_managed_policy_statements PRIMARY KEY
        (identity_scope_id, application_key, policy_id, policy_version, statement_id),
    CONSTRAINT fk_managed_policy_statements_version FOREIGN KEY
        (identity_scope_id, application_key, policy_id, policy_version, model_version)
        REFERENCES identity_access.managed_policy_versions
        (identity_scope_id, application_key, policy_id, policy_version, model_version)
        ON DELETE RESTRICT,
    CONSTRAINT fk_managed_policy_statements_capability FOREIGN KEY
        (identity_scope_id, application_key, model_version,
         capability_resource, capability_feature, capability_action)
        REFERENCES identity_access.application_capabilities
        (identity_scope_id, application_key, model_version,
         capability_resource, capability_feature, capability_action)
        ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_managed_policies_application
    ON identity_access.managed_policies
       (identity_scope_id, application_key, policy_key, policy_id);

CREATE INDEX IF NOT EXISTS ix_managed_policy_statements_capability
    ON identity_access.managed_policy_statements
       (identity_scope_id, application_key, model_version,
        capability_resource, capability_feature, capability_action);
