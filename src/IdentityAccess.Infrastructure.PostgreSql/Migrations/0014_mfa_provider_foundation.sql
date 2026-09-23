CREATE TABLE IF NOT EXISTS identity_access.mfa_policies
(
    identity_scope_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    mode smallint NOT NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_mfa_policies PRIMARY KEY
        (identity_scope_id, application_key),
    CONSTRAINT ck_mfa_policies_application_key
        CHECK (application_key ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_mfa_policies_mode
        CHECK (mode IN (1, 2, 3)),
    CONSTRAINT ck_mfa_policies_row_version
        CHECK (row_version > 0)
);

CREATE TABLE IF NOT EXISTS identity_access.mfa_policy_providers
(
    identity_scope_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    provider_key varchar(64) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_mfa_policy_providers PRIMARY KEY
        (identity_scope_id, application_key, provider_key),
    CONSTRAINT fk_mfa_policy_providers_policy FOREIGN KEY
        (identity_scope_id, application_key)
        REFERENCES identity_access.mfa_policies
        (identity_scope_id, application_key)
        ON DELETE CASCADE,
    CONSTRAINT ck_mfa_policy_providers_key
        CHECK (provider_key ~ '^[a-z][a-z0-9-]{0,63}$')
);

CREATE TABLE IF NOT EXISTS identity_access.user_authenticators
(
    identity_scope_id uuid NOT NULL,
    authenticator_id uuid NOT NULL,
    user_id uuid NOT NULL,
    provider_key varchar(64) NOT NULL,
    display_name varchar(256) NOT NULL,
    status smallint NOT NULL,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    confirmed_at timestamptz NULL,
    last_used_at timestamptz NULL,
    revoked_at timestamptz NULL,
    updated_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_user_authenticators PRIMARY KEY
        (identity_scope_id, authenticator_id),
    CONSTRAINT fk_user_authenticators_user FOREIGN KEY
        (identity_scope_id, user_id)
        REFERENCES identity_access.users
        (identity_scope_id, user_id)
        ON DELETE RESTRICT,
    CONSTRAINT ck_user_authenticators_provider_key
        CHECK (provider_key ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_user_authenticators_display_name
        CHECK (length(display_name) > 0 AND length(display_name) <= 256),
    CONSTRAINT ck_user_authenticators_status
        CHECK (status IN (1, 2, 3)),
    CONSTRAINT ck_user_authenticators_row_version
        CHECK (row_version > 0),
    CONSTRAINT ck_user_authenticators_timestamps
        CHECK
        (
            (confirmed_at IS NULL OR confirmed_at >= created_at)
            AND (last_used_at IS NULL OR last_used_at >= created_at)
            AND (revoked_at IS NULL OR revoked_at >= created_at)
        ),
    CONSTRAINT ck_user_authenticators_lifecycle
        CHECK
        (
            (status = 1 AND confirmed_at IS NULL AND revoked_at IS NULL)
            OR
            (status = 2 AND confirmed_at IS NOT NULL AND revoked_at IS NULL)
            OR
            (status = 3 AND revoked_at IS NOT NULL)
        )
);

CREATE INDEX IF NOT EXISTS ix_user_authenticators_user
    ON identity_access.user_authenticators
       (identity_scope_id, user_id, provider_key, status);

CREATE INDEX IF NOT EXISTS ix_user_authenticators_active
    ON identity_access.user_authenticators
       (identity_scope_id, user_id, provider_key)
    WHERE status = 2;

CREATE TRIGGER trg_security_mutation_mfa_policies
AFTER INSERT OR UPDATE OR DELETE
ON identity_access.mfa_policies
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'application_key');

CREATE TRIGGER trg_security_mutation_mfa_policy_providers
AFTER INSERT OR UPDATE OR DELETE
ON identity_access.mfa_policy_providers
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'application_key',
    'provider_key');

CREATE TRIGGER trg_security_mutation_user_authenticators
AFTER INSERT OR UPDATE OR DELETE
ON identity_access.user_authenticators
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'authenticator_id');
