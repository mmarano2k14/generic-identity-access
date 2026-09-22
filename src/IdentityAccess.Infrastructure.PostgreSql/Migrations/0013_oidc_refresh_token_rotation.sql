CREATE TABLE identity_access.oidc_refresh_tokens
(
    identity_scope_id uuid NOT NULL,
    family_id uuid NOT NULL,
    token_id uuid NOT NULL,
    parent_token_id uuid NULL,
    sequence_number bigint NOT NULL,
    token_hash bytea NOT NULL,
    user_id uuid NOT NULL,
    session_id uuid NOT NULL,
    client_id varchar(128) NOT NULL,
    application_key varchar(64) NOT NULL,
    authentication_context_key varchar(64) NOT NULL,
    scope varchar(256) NOT NULL,
    authenticated_at timestamptz NOT NULL,
    issued_at timestamptz NOT NULL,
    expires_at timestamptz NOT NULL,
    consumed_at timestamptz NULL,
    revoked_at timestamptz NULL,
    revocation_reason varchar(32) NULL,
    CONSTRAINT pk_oidc_refresh_tokens PRIMARY KEY
        (identity_scope_id, token_id),
    CONSTRAINT uq_oidc_refresh_tokens_hash UNIQUE
        (token_hash),
    CONSTRAINT uq_oidc_refresh_tokens_family_sequence UNIQUE
        (identity_scope_id, family_id, sequence_number),
    CONSTRAINT fk_oidc_refresh_tokens_user FOREIGN KEY
        (identity_scope_id, user_id)
        REFERENCES identity_access.users
        (identity_scope_id, user_id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_oidc_refresh_tokens_session FOREIGN KEY
        (identity_scope_id, session_id)
        REFERENCES identity_access.user_sessions
        (identity_scope_id, session_id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_oidc_refresh_tokens_parent FOREIGN KEY
        (identity_scope_id, parent_token_id)
        REFERENCES identity_access.oidc_refresh_tokens
        (identity_scope_id, token_id)
        ON DELETE RESTRICT,
    CONSTRAINT ck_oidc_refresh_tokens_hash
        CHECK (octet_length(token_hash) = 32),
    CONSTRAINT ck_oidc_refresh_tokens_sequence
        CHECK (sequence_number >= 0),
    CONSTRAINT ck_oidc_refresh_tokens_parent
        CHECK
        (
            (sequence_number = 0 AND parent_token_id IS NULL)
            OR
            (sequence_number > 0 AND parent_token_id IS NOT NULL AND parent_token_id <> token_id)
        ),
    CONSTRAINT ck_oidc_refresh_tokens_client_id
        CHECK (client_id ~ '^[a-z][a-z0-9_-]{0,127}$'),
    CONSTRAINT ck_oidc_refresh_tokens_application_key
        CHECK (application_key ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_oidc_refresh_tokens_context_key
        CHECK (authentication_context_key ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_oidc_refresh_tokens_scope
        CHECK (scope = 'openid'),
    CONSTRAINT ck_oidc_refresh_tokens_times
        CHECK
        (
            authenticated_at <= issued_at
            AND expires_at > issued_at
            AND (consumed_at IS NULL OR consumed_at >= issued_at)
            AND (revoked_at IS NULL OR revoked_at >= issued_at)
        ),
    CONSTRAINT ck_oidc_refresh_tokens_revocation
        CHECK
        (
            (revoked_at IS NULL AND revocation_reason IS NULL)
            OR
            (revoked_at IS NOT NULL AND revocation_reason IS NOT NULL)
        ),
    CONSTRAINT ck_oidc_refresh_tokens_revocation_reason
        CHECK
        (
            revocation_reason IS NULL
            OR revocation_reason = 'reuse_detected'
        )
);

CREATE INDEX ix_oidc_refresh_tokens_family
    ON identity_access.oidc_refresh_tokens
       (identity_scope_id, family_id, sequence_number);

CREATE INDEX ix_oidc_refresh_tokens_session
    ON identity_access.oidc_refresh_tokens
       (identity_scope_id, session_id, expires_at);

CREATE INDEX ix_oidc_refresh_tokens_active_family
    ON identity_access.oidc_refresh_tokens
       (identity_scope_id, family_id, expires_at)
    WHERE consumed_at IS NULL
      AND revoked_at IS NULL;

CREATE TRIGGER trg_security_mutation_oidc_refresh_tokens
AFTER INSERT OR UPDATE OR DELETE
ON identity_access.oidc_refresh_tokens
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'token_id');
