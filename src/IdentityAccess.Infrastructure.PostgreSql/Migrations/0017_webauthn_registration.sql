CREATE TABLE identity_access.webauthn_registration_challenges
(
    identity_scope_id uuid NOT NULL,
    authenticator_id uuid NOT NULL,
    application_key varchar(64) NOT NULL,
    challenge_hash bytea NOT NULL,
    expires_at timestamptz NOT NULL,
    consumed_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_webauthn_registration_challenges PRIMARY KEY
        (identity_scope_id, authenticator_id),
    CONSTRAINT fk_webauthn_registration_challenges_generic FOREIGN KEY
        (identity_scope_id, authenticator_id)
        REFERENCES identity_access.user_authenticators
        (identity_scope_id, authenticator_id)
        ON DELETE CASCADE,
    CONSTRAINT ck_webauthn_registration_application_key
        CHECK (application_key ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_webauthn_registration_challenge_hash
        CHECK (octet_length(challenge_hash) = 32),
    CONSTRAINT ck_webauthn_registration_expiry
        CHECK (expires_at > created_at),
    CONSTRAINT ck_webauthn_registration_consumed
        CHECK (consumed_at IS NULL OR consumed_at >= created_at)
);

CREATE TABLE identity_access.webauthn_credentials
(
    identity_scope_id uuid NOT NULL,
    authenticator_id uuid NOT NULL,
    credential_id bytea NOT NULL,
    cose_public_key bytea NOT NULL,
    cose_algorithm smallint NOT NULL,
    aaguid bytea NOT NULL,
    sign_count bigint NOT NULL,
    backup_eligible boolean NOT NULL,
    backup_state boolean NOT NULL,
    user_handle bytea NOT NULL,
    created_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    CONSTRAINT pk_webauthn_credentials PRIMARY KEY
        (identity_scope_id, authenticator_id),
    CONSTRAINT uq_webauthn_credentials_id UNIQUE
        (identity_scope_id, credential_id),
    CONSTRAINT fk_webauthn_credentials_generic FOREIGN KEY
        (identity_scope_id, authenticator_id)
        REFERENCES identity_access.user_authenticators
        (identity_scope_id, authenticator_id)
        ON DELETE CASCADE,
    CONSTRAINT ck_webauthn_credentials_credential_id
        CHECK (octet_length(credential_id) BETWEEN 1 AND 1023),
    CONSTRAINT ck_webauthn_credentials_public_key
        CHECK (octet_length(cose_public_key) BETWEEN 1 AND 4096),
    CONSTRAINT ck_webauthn_credentials_algorithm
        CHECK (cose_algorithm = -7),
    CONSTRAINT ck_webauthn_credentials_aaguid
        CHECK (octet_length(aaguid) = 16),
    CONSTRAINT ck_webauthn_credentials_sign_count
        CHECK (sign_count BETWEEN 0 AND 4294967295),
    CONSTRAINT ck_webauthn_credentials_backup_state
        CHECK (NOT backup_state OR backup_eligible),
    CONSTRAINT ck_webauthn_credentials_user_handle
        CHECK (octet_length(user_handle) = 32)
);

CREATE INDEX ix_webauthn_registration_expiry
    ON identity_access.webauthn_registration_challenges
       (identity_scope_id, expires_at)
    WHERE consumed_at IS NULL;

CREATE INDEX ix_webauthn_credentials_user
    ON identity_access.webauthn_credentials
       (identity_scope_id, user_handle);

CREATE TRIGGER trg_security_mutation_webauthn_registration_challenges
AFTER INSERT OR UPDATE OR DELETE
ON identity_access.webauthn_registration_challenges
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'authenticator_id');

CREATE TRIGGER trg_security_mutation_webauthn_credentials
AFTER INSERT OR UPDATE OR DELETE
ON identity_access.webauthn_credentials
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'authenticator_id');
