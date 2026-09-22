CREATE TABLE identity_access.identity_scope_administration_groups
(
    identity_scope_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    group_id uuid NOT NULL,
    display_name varchar(256) NOT NULL,
    status smallint NOT NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_identity_scope_administration_groups PRIMARY KEY
        (identity_scope_id, application_key, group_id),
    CONSTRAINT ck_identity_scope_administration_groups_application_key
        CHECK (application_key ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_identity_scope_administration_groups_status
        CHECK (status IN (1, 2)),
    CONSTRAINT ck_identity_scope_administration_groups_row_version
        CHECK (row_version > 0)
);

CREATE TABLE identity_access.identity_scope_administration_group_memberships
(
    identity_scope_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    group_id uuid NOT NULL,
    user_id uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_identity_scope_administration_group_memberships PRIMARY KEY
        (identity_scope_id, application_key, group_id, user_id),
    CONSTRAINT fk_identity_scope_administration_group_memberships_group FOREIGN KEY
        (identity_scope_id, application_key, group_id)
        REFERENCES identity_access.identity_scope_administration_groups
        (identity_scope_id, application_key, group_id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_identity_scope_administration_group_memberships_user FOREIGN KEY
        (identity_scope_id, user_id)
        REFERENCES identity_access.users
        (identity_scope_id, user_id)
        ON DELETE RESTRICT
);

CREATE TABLE identity_access.identity_scope_administration_policies
(
    identity_scope_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    policy_id uuid NOT NULL,
    display_name varchar(256) NOT NULL,
    status smallint NOT NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_identity_scope_administration_policies PRIMARY KEY
        (identity_scope_id, application_key, policy_id),
    CONSTRAINT ck_identity_scope_administration_policies_application_key
        CHECK (application_key ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_identity_scope_administration_policies_status
        CHECK (status IN (1, 2)),
    CONSTRAINT ck_identity_scope_administration_policies_row_version
        CHECK (row_version > 0)
);

CREATE TABLE identity_access.identity_scope_administration_policy_statements
(
    identity_scope_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    policy_id uuid NOT NULL,
    statement_id uuid NOT NULL,
    model_version integer NOT NULL,
    capability_resource varchar(64) NOT NULL,
    capability_feature varchar(64) NOT NULL,
    capability_action varchar(64) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_identity_scope_administration_policy_statements PRIMARY KEY
        (identity_scope_id, application_key, policy_id, statement_id),
    CONSTRAINT fk_identity_scope_administration_policy_statements_policy FOREIGN KEY
        (identity_scope_id, application_key, policy_id)
        REFERENCES identity_access.identity_scope_administration_policies
        (identity_scope_id, application_key, policy_id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_identity_scope_administration_policy_statements_model FOREIGN KEY
        (identity_scope_id, application_key, model_version)
        REFERENCES identity_access.application_security_models
        (identity_scope_id, application_key, model_version)
        ON DELETE RESTRICT,
    CONSTRAINT ck_identity_scope_administration_policy_statements_resource
        CHECK (capability_resource = '*' OR capability_resource ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_identity_scope_administration_policy_statements_feature
        CHECK (capability_feature = '*' OR capability_feature ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_identity_scope_administration_policy_statements_action
        CHECK (capability_action = '*' OR capability_action ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_identity_scope_administration_policy_statements_supported_pattern
        CHECK (capability_resource <> '*' OR capability_feature = '*')
);

CREATE TABLE identity_access.identity_scope_administration_group_policy_bindings
(
    identity_scope_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    group_id uuid NOT NULL,
    policy_id uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_identity_scope_administration_group_policy_bindings PRIMARY KEY
        (identity_scope_id, application_key, group_id, policy_id),
    CONSTRAINT fk_identity_scope_administration_group_policy_bindings_group FOREIGN KEY
        (identity_scope_id, application_key, group_id)
        REFERENCES identity_access.identity_scope_administration_groups
        (identity_scope_id, application_key, group_id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_identity_scope_administration_group_policy_bindings_policy FOREIGN KEY
        (identity_scope_id, application_key, policy_id)
        REFERENCES identity_access.identity_scope_administration_policies
        (identity_scope_id, application_key, policy_id)
        ON DELETE RESTRICT
);

CREATE OR REPLACE FUNCTION identity_access.validate_identity_scope_administration_statement_pattern()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.capability_resource <> '*'
       AND NEW.capability_feature <> '*'
       AND NEW.capability_action <> '*' THEN
        IF NOT EXISTS
        (
            SELECT 1
            FROM identity_access.application_capabilities c
            WHERE c.identity_scope_id = NEW.identity_scope_id
              AND c.application_key = NEW.application_key
              AND c.model_version = NEW.model_version
              AND c.capability_resource = NEW.capability_resource
              AND c.capability_feature = NEW.capability_feature
              AND c.capability_action = NEW.capability_action
        ) THEN
            RAISE EXCEPTION USING
                ERRCODE = '23514',
                MESSAGE = 'identity-scope administration exact capability is not declared by the pinned security model';
        END IF;
    ELSE
        IF NOT EXISTS
        (
            SELECT 1
            FROM identity_access.application_capabilities c
            WHERE c.identity_scope_id = NEW.identity_scope_id
              AND c.application_key = NEW.application_key
              AND c.model_version = NEW.model_version
              AND (NEW.capability_resource = '*' OR c.capability_resource = NEW.capability_resource)
              AND (NEW.capability_feature = '*' OR c.capability_feature = NEW.capability_feature)
              AND (NEW.capability_action = '*' OR c.capability_action = NEW.capability_action)
        ) THEN
            RAISE EXCEPTION USING
                ERRCODE = '23514',
                MESSAGE = 'identity-scope administration wildcard pattern matches no capability in the pinned security model';
        END IF;
    END IF;

    RETURN NEW;
END
$$;

CREATE TRIGGER trg_validate_identity_scope_administration_statement_pattern
BEFORE INSERT OR UPDATE OF
    model_version,
    capability_resource,
    capability_feature,
    capability_action
ON identity_access.identity_scope_administration_policy_statements
FOR EACH ROW
EXECUTE FUNCTION identity_access.validate_identity_scope_administration_statement_pattern();

CREATE INDEX ix_identity_scope_administration_group_memberships_subject
    ON identity_access.identity_scope_administration_group_memberships
       (identity_scope_id, application_key, user_id);

CREATE INDEX ix_identity_scope_administration_group_policy_bindings_policy
    ON identity_access.identity_scope_administration_group_policy_bindings
       (identity_scope_id, application_key, policy_id);

CREATE INDEX ix_identity_scope_administration_policy_statements_pattern
    ON identity_access.identity_scope_administration_policy_statements
       (identity_scope_id, application_key, model_version,
        capability_resource, capability_feature, capability_action);
