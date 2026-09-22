CREATE TABLE identity_access.oidc_authorization_codes
(
    identity_scope_id uuid NOT NULL,
    code_id uuid NOT NULL,
    code_hash bytea NOT NULL,
    user_id uuid NOT NULL,
    session_id uuid NOT NULL,
    client_id varchar(128) NOT NULL,
    application_key varchar(64) NOT NULL,
    authentication_context_key varchar(64) NOT NULL,
    redirect_uri varchar(2048) NOT NULL,
    scope varchar(256) NOT NULL,
    code_challenge varchar(128) NOT NULL,
    code_challenge_method varchar(8) NOT NULL,
    nonce varchar(256) NOT NULL,
    authenticated_at timestamptz NOT NULL,
    issued_at timestamptz NOT NULL,
    expires_at timestamptz NOT NULL,
    consumed_at timestamptz NULL,
    CONSTRAINT pk_oidc_authorization_codes PRIMARY KEY
        (identity_scope_id, code_id),
    CONSTRAINT uq_oidc_authorization_codes_hash UNIQUE
        (code_hash),
    CONSTRAINT fk_oidc_authorization_codes_user FOREIGN KEY
        (identity_scope_id, user_id)
        REFERENCES identity_access.users
        (identity_scope_id, user_id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_oidc_authorization_codes_session FOREIGN KEY
        (identity_scope_id, session_id)
        REFERENCES identity_access.user_sessions
        (identity_scope_id, session_id)
        ON DELETE RESTRICT,
    CONSTRAINT ck_oidc_authorization_codes_hash
        CHECK (octet_length(code_hash) = 32),
    CONSTRAINT ck_oidc_authorization_codes_client_id
        CHECK (client_id ~ '^[a-z][a-z0-9_-]{0,127}$'),
    CONSTRAINT ck_oidc_authorization_codes_application_key
        CHECK (application_key ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_oidc_authorization_codes_context_key
        CHECK (authentication_context_key ~ '^[a-z][a-z0-9-]{0,63}$'),
    CONSTRAINT ck_oidc_authorization_codes_redirect_uri
        CHECK (length(redirect_uri) BETWEEN 1 AND 2048),
    CONSTRAINT ck_oidc_authorization_codes_scope
        CHECK (scope = 'openid'),
    CONSTRAINT ck_oidc_authorization_codes_challenge
        CHECK (code_challenge ~ '^[A-Za-z0-9_-]{43}$'),
    CONSTRAINT ck_oidc_authorization_codes_challenge_method
        CHECK (code_challenge_method = 'S256'),
    CONSTRAINT ck_oidc_authorization_codes_nonce
        CHECK (length(nonce) BETWEEN 8 AND 256),
    CONSTRAINT ck_oidc_authorization_codes_times
        CHECK
        (
            authenticated_at <= issued_at
            AND expires_at > issued_at
            AND (consumed_at IS NULL OR consumed_at >= issued_at)
        )
);

CREATE INDEX ix_oidc_authorization_codes_client_active
    ON identity_access.oidc_authorization_codes
       (identity_scope_id, client_id, expires_at)
    WHERE consumed_at IS NULL;

CREATE INDEX ix_oidc_authorization_codes_session
    ON identity_access.oidc_authorization_codes
       (identity_scope_id, session_id);

CREATE TRIGGER trg_security_mutation_oidc_authorization_codes
AFTER INSERT OR UPDATE OR DELETE
ON identity_access.oidc_authorization_codes
FOR EACH ROW EXECUTE FUNCTION identity_access.capture_security_mutation(
    'identity_scope_id',
    'code_id');
